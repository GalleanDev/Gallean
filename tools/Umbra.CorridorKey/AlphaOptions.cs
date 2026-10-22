using TorchSharp;

namespace Umbra.CorridorKey;

internal sealed record AlphaOptions(torch.Device Device, torch.ScalarType ModelDtype)
{
    public int ImageSize { get; init; } = 2048;
    public double AlphaFloor { get; init; } = 0.1;
    public double EdgeAlpha { get; init; } = 0.98;
    public int HintErode { get; init; } = 2;
    public int HintBlur { get; init; } = 3;
    public int HintBound { get; init; } = 8;
}
