using TorchSharp;
using TorchSharp.Modules;
using static TorchSharp.torch.nn;

namespace Umbra;

internal sealed class WanVaeDecoder : Module<torch.Tensor, torch.Tensor>
{
    private readonly WanCausalConv3d conv1;
    private readonly ModuleList<Module<torch.Tensor, torch.Tensor>> middle;
    private readonly ModuleList<Module<torch.Tensor, torch.Tensor>> upsamples;
    private readonly ModuleList<Module<torch.Tensor, torch.Tensor>> head;

    public WanVaeDecoder(
        int dim,
        int latentChannels,
        IReadOnlyList<int> multipliers,
        int residualBlocks,
        IReadOnlyList<bool> temporalUpsample)
        : base(nameof(WanVaeDecoder))
    {
        List<int> dims = [dim * multipliers[^1]];

        for (int index = multipliers.Count - 1; index >= 0; index--)
        {
            dims.Add(dim * multipliers[index]);
        }

        conv1 = new WanCausalConv3d(latentChannels, dims[0], (3, 3, 3), (1, 1, 1));

        middle = new ModuleList<Module<torch.Tensor, torch.Tensor>>(
            new WanVaeResidualBlock(dims[0], dims[0]),
            new WanVaeAttentionBlock(dims[0]),
            new WanVaeResidualBlock(dims[0], dims[0]));

        List<Module<torch.Tensor, torch.Tensor>> stages = [];
        int outChannels = dims[0];

        for (int index = 0; index + 1 < dims.Count; index++)
        {
            int inChannels = index is 1 or 2 or 3 ? dims[index] / 2 : dims[index];

            outChannels = dims[index + 1];

            for (int block = 0; block < residualBlocks + 1; block++)
            {
                stages.Add(new WanVaeResidualBlock(inChannels, outChannels));
                inChannels = outChannels;
            }

            if (index != multipliers.Count - 1)
            {
                stages.Add(new WanResample(
                    outChannels,
                    temporalUpsample[index] ? WanResampleType.Upsample3d : WanResampleType.Upsample2d));
            }
        }

        upsamples = new ModuleList<Module<torch.Tensor, torch.Tensor>>([.. stages]);

        head = new ModuleList<Module<torch.Tensor, torch.Tensor>>(
            new WanRmsNorm(outChannels, images: false),
            SiLU(),
            new WanCausalConv3d(outChannels, 3, (3, 3, 3), (1, 1, 1)));

        RegisterComponents();
    }

    public override torch.Tensor forward(torch.Tensor input)
    {
        return Forward(input, null);
    }

    public torch.Tensor Forward(torch.Tensor input, WanVaeCache? cache)
    {
        using IDisposable scope = torch.NewDisposeScope();

        torch.Tensor result = WanVaeCacheTail.Convolve(conv1, input, cache);

        foreach (Module<torch.Tensor, torch.Tensor> layer in middle)
        {
            result = WanVaeStep.Apply(layer, result, cache);
        }

        foreach (Module<torch.Tensor, torch.Tensor> layer in upsamples)
        {
            result = WanVaeStep.Apply(layer, result, cache);
        }

        foreach (Module<torch.Tensor, torch.Tensor> layer in head)
        {
            result = WanVaeStep.Apply(layer, result, cache);
        }

        return result.MoveToOuterDisposeScope();
    }
}
