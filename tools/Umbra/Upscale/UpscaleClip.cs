namespace Umbra;

internal sealed record UpscaleClip(long CanvasWidth, long CanvasHeight, IReadOnlyList<UpscaleClipFrame> Frames);
