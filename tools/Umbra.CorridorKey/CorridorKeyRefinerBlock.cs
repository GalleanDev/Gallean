using TorchSharp;
using TorchSharp.Modules;
using TorchSharp.Utils;
using static TorchSharp.torch.nn;

namespace Umbra.CorridorKey;

internal sealed class CorridorKeyRefinerBlock : Module<torch.Tensor, torch.Tensor>
{
    [ComponentName(Name = "conv1")]
    private readonly Conv2d firstConvolution;

    [ComponentName(Name = "gn1")]
    private readonly GroupNorm firstNorm;

    [ComponentName(Name = "conv2")]
    private readonly Conv2d secondConvolution;

    [ComponentName(Name = "gn2")]
    private readonly GroupNorm secondNorm;

    public CorridorKeyRefinerBlock(int channels, int dilation)
        : base(nameof(CorridorKeyRefinerBlock))
    {
        firstConvolution = Conv2d(channels, channels, kernel_size: 3, padding: dilation, dilation: dilation, bias: true);
        firstNorm = GroupNorm(8, channels);
        secondConvolution = Conv2d(channels, channels, kernel_size: 3, padding: dilation, dilation: dilation, bias: true);
        secondNorm = GroupNorm(8, channels);

        RegisterComponents();
    }

    public override torch.Tensor forward(torch.Tensor planes)
    {
        using IDisposable scope = torch.NewDisposeScope();

        using torch.Tensor hidden = firstConvolution.call(planes);
        using torch.Tensor normalizedHidden = firstNorm.call(hidden);
        using torch.Tensor activations = functional.relu(normalizedHidden);
        using torch.Tensor output = secondConvolution.call(activations);
        using torch.Tensor normalizedOutput = secondNorm.call(output);
        using torch.Tensor residualSum = normalizedOutput.add(planes);

        return functional.relu(residualSum).MoveToOuterDisposeScope();
    }
}
