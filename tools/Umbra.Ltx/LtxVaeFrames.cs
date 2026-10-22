using TorchSharp;

namespace Umbra.Ltx;

internal static class LtxVaeFrames
{
    public const int CausalHalo = 2;
    public const int SymmetricHalo = 1;

    public static LtxVaeSlice Slice(torch.Tensor input, long start, long count, torch.Device device)
    {
        using IDisposable scope = torch.NewDisposeScope();

        long frames = input.shape[2];
        long first = Math.Max(0, start);
        long end = Math.Min(frames, start + count);

        using torch.Tensor chunk = input.narrow(2, first, end - first);

        return new LtxVaeSlice(chunk.to(device).MoveToOuterDisposeScope(), first);
    }
}
