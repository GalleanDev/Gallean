using TorchSharp;

namespace Umbra.CorridorKey;

internal sealed record CorridorKeyPrediction(torch.Tensor Alpha, torch.Tensor Foreground) : IDisposable
{
    public void Dispose()
    {
        Alpha.Dispose();
        Foreground.Dispose();
    }
}
