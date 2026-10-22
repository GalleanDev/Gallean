using TorchSharp;
using TorchSharp.Modules;
using static TorchSharp.torch.nn;

namespace Umbra;

internal sealed class WanVaeEncoder : Module<torch.Tensor, torch.Tensor>
{
    private readonly WanCausalConv3d conv1;
    private readonly ModuleList<Module<torch.Tensor, torch.Tensor>> downsamples;
    private readonly ModuleList<Module<torch.Tensor, torch.Tensor>> middle;
    private readonly ModuleList<Module<torch.Tensor, torch.Tensor>> head;

    public WanVaeEncoder(
        int dim,
        int latentChannels,
        IReadOnlyList<int> multipliers,
        int residualBlocks,
        IReadOnlyList<bool> temporalDownsample)
        : base(nameof(WanVaeEncoder))
    {
        List<int> dims = [dim];

        foreach (int multiplier in multipliers)
        {
            dims.Add(dim * multiplier);
        }

        conv1 = new WanCausalConv3d(3, dims[0], (3, 3, 3), (1, 1, 1));

        List<Module<torch.Tensor, torch.Tensor>> stages = [];
        int outChannels = dims[0];

        for (int index = 0; index + 1 < dims.Count; index++)
        {
            int inChannels = dims[index];

            outChannels = dims[index + 1];

            for (int block = 0; block < residualBlocks; block++)
            {
                stages.Add(new WanVaeResidualBlock(inChannels, outChannels));
                inChannels = outChannels;
            }

            if (index != multipliers.Count - 1)
            {
                stages.Add(new WanResample(
                    outChannels,
                    temporalDownsample[index] ? WanResampleType.Downsample3d : WanResampleType.Downsample2d));
            }
        }

        downsamples = new ModuleList<Module<torch.Tensor, torch.Tensor>>([.. stages]);

        middle = new ModuleList<Module<torch.Tensor, torch.Tensor>>(
            new WanVaeResidualBlock(outChannels, outChannels),
            new WanVaeAttentionBlock(outChannels),
            new WanVaeResidualBlock(outChannels, outChannels));

        head = new ModuleList<Module<torch.Tensor, torch.Tensor>>(
            new WanRmsNorm(outChannels, images: false),
            SiLU(),
            new WanCausalConv3d(outChannels, latentChannels, (3, 3, 3), (1, 1, 1)));

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

        foreach (Module<torch.Tensor, torch.Tensor> layer in downsamples)
        {
            result = WanVaeStep.Apply(layer, result, cache);
        }

        foreach (Module<torch.Tensor, torch.Tensor> layer in middle)
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
