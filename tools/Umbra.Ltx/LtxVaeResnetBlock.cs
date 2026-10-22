using TorchSharp;
using TorchSharp.Utils;
using static TorchSharp.torch.nn;

namespace Umbra.Ltx;

internal sealed class LtxVaeResnetBlock : Module<torch.Tensor, torch.Tensor>
{
    [ComponentName(Name = "conv1")]
    private readonly LtxCausalConv3d convolution1;

    [ComponentName(Name = "conv2")]
    private readonly LtxCausalConv3d convolution2;

    public LtxVaeResnetBlock(int channels, bool causal, long chunkBytes)
        : base(nameof(LtxVaeResnetBlock))
    {
        convolution1 = new LtxCausalConv3d(channels, channels, causal, chunkBytes);
        convolution2 = new LtxCausalConv3d(channels, channels, causal, chunkBytes);

        RegisterComponents();
    }

    private static torch.Tensor Convolve(LtxCausalConv3d convolution, torch.Tensor input)
    {
        using IDisposable scope = torch.NewDisposeScope();

        using torch.Tensor hidden = LtxPixelNorm.Normalize(input);

        hidden.silu_();

        return convolution.forward(hidden).MoveToOuterDisposeScope();
    }

    public override torch.Tensor forward(torch.Tensor input)
    {
        using IDisposable scope = torch.NewDisposeScope();

        torch.Tensor hidden = Convolve(convolution1, input);
        torch.Tensor output = Convolve(convolution2, hidden);

        hidden.Dispose();
        output.add_(input);

        return output.MoveToOuterDisposeScope();
    }
}
