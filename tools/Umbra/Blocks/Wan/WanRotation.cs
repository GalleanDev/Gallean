using TorchSharp;

namespace Umbra;

internal static class WanRotation
{
    public static torch.Tensor Apply(torch.Tensor input, torch.Tensor frequencies, int heads)
    {
        using IDisposable scope = torch.NewDisposeScope();

        long batch = input.shape[0];
        long sequence = input.shape[1];

        using torch.Tensor pairs = input
            .reshape(batch, sequence, heads, -1)
            .to(torch.ScalarType.Float64)
            .reshape(batch, sequence, heads, -1, 2);
        using torch.Tensor complex = torch.view_as_complex(pairs);
        using torch.Tensor rotatedComplex = complex.mul(frequencies);
        using torch.Tensor rotatedPairs = torch.view_as_real(rotatedComplex);

        torch.Tensor result = rotatedPairs.flatten(2).to(input.dtype);

        return result.MoveToOuterDisposeScope();
    }
}
