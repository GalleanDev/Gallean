using TorchSharp;

namespace Umbra.Ltx;

internal static class LtxPixelNorm
{
    private const double Epsilon = 1e-8;

    public static torch.Tensor Normalize(torch.Tensor input)
    {
        using IDisposable scope = torch.NewDisposeScope();

        using torch.Tensor squares = input.pow(2.0);
        using torch.Tensor mean = squares.mean([1], keepdim: true);
        using torch.Tensor scale = mean.add(Epsilon).sqrt();

        return input.div(scale).MoveToOuterDisposeScope();
    }
}
