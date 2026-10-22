namespace Packer;

internal sealed partial class PreviewBuilder
{
    private sealed record FrameCanvas(int PixelWidth, int PixelHeight, IReadOnlyList<FramePlacement> Placements);
}
