using TorchSharp;

namespace Umbra.Ltx;

internal sealed record LtxVaeSlice(torch.Tensor Frames, long Start) : IDisposable
{
    public void Dispose()
    {
        Frames.Dispose();
    }
}
