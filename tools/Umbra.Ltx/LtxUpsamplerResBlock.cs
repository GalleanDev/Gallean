using TorchSharp;
using TorchSharp.Modules;
using TorchSharp.Utils;
using static TorchSharp.torch.nn;

namespace Umbra.Ltx;

internal sealed class LtxUpsamplerResBlock : Module<torch.Tensor, torch.Tensor>
{
    private const int Groups = 32;

    [ComponentName(Name = "conv1")]
    private readonly Conv3d convolution1;

    [ComponentName(Name = "norm1")]
    private readonly GroupNorm norm1;

    [ComponentName(Name = "conv2")]
    private readonly Conv3d convolution2;

    [ComponentName(Name = "norm2")]
    private readonly GroupNorm norm2;

    public LtxUpsamplerResBlock(int channels)
        : base(nameof(LtxUpsamplerResBlock))
    {
        convolution1 = Conv3d(channels, channels, 3, padding: 1);
        norm1 = GroupNorm(Groups, channels);
        convolution2 = Conv3d(channels, channels, 3, padding: 1);
        norm2 = GroupNorm(Groups, channels);

        RegisterComponents();
    }

    public override torch.Tensor forward(torch.Tensor input)
    {
        using IDisposable scope = torch.NewDisposeScope();

        using torch.Tensor features1 = convolution1.forward(input);
        using torch.Tensor normalizedFeatures1 = norm1.forward(features1);
        using torch.Tensor activatedFeatures1 = functional.silu(normalizedFeatures1);
        using torch.Tensor features2 = convolution2.forward(activatedFeatures1);
        using torch.Tensor normalizedFeatures2 = norm2.forward(features2);
        using torch.Tensor residualSum = normalizedFeatures2.add(input);

        return functional.silu(residualSum).MoveToOuterDisposeScope();
    }
}
