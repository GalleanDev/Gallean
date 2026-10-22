using TorchSharp;

namespace Umbra;

internal static class WanTimestepEmbedding
{
    private const double Theta = 10000;

    public static torch.Tensor Build(int dim, torch.Tensor position)
    {
        using IDisposable scope = torch.NewDisposeScope();

        using torch.Tensor steps = torch.arange(dim / 2, torch.ScalarType.Float64, position.device);
        using torch.Tensor frequencies = steps.div(dim / 2).mul(Math.Log(Theta)).neg().exp();
        using torch.Tensor sinusoid = torch.outer(position.to(torch.ScalarType.Float64), frequencies);
        using torch.Tensor cosine = sinusoid.cos();
        using torch.Tensor sine = sinusoid.sin();

        torch.Tensor result = torch.cat([cosine, sine], dim: 1).to(position.dtype);

        return result.MoveToOuterDisposeScope();
    }
}
