using TorchSharp;

namespace Umbra.Ltx;

internal sealed record RefineOptions(
    torch.Device Device,
    torch.ScalarType ModelDtype,
    torch.ScalarType VaeDtype,
    LtxSchedule Schedule)
{
    public const int ModelScale = 2;
    public int FrameRepeatsDefault { get; init; } = 2;
    public int FrameRepeatsMotion { get; init; } = 8;
    public double MotionThreshold { get; init; } = 0.015;
    public double FramesPerSecond { get; init; } = 24;
    public long Seed { get; init; } = 1;
    public int MinClipFrames { get; init; } = 16;
    public int CachedBlocks { get; init; } = 1;
    public double ChromaKeyScreenThreshold { get; init; } = 60.0 / 255.0;
    public double ChromaKeyObjectThreshold { get; init; } = 140.0 / 255.0;
    public int AdditiveDetailRadius { get; init; } = 2;
}
