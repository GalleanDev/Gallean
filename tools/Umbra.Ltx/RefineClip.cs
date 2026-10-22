namespace Umbra.Ltx;

internal sealed record RefineClip(
    UmbraSequenceId Id,
    long CanvasWidth,
    long CanvasHeight,
    IReadOnlyList<RefineClipFrame> Frames,
    IReadOnlyList<int> CycleLengths);
