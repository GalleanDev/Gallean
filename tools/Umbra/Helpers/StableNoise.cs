using TorchSharp;

namespace Umbra;

internal static class StableNoise
{
    public static torch.Tensor Generate(long seed, long[] shape, torch.ScalarType dtype, torch.Device device)
    {
        using IDisposable scope = torch.NewDisposeScope();

        using torch.Generator generator = new((ulong)seed);
        using torch.Tensor noise = torch.randn(shape, torch.ScalarType.Float32, device: torch.CPU, generator: generator);

        return noise.to(dtype, device).MoveToOuterDisposeScope();
    }
}
