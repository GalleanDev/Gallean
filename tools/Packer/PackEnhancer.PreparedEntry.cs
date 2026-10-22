namespace Packer;

internal sealed partial class PackEnhancer
{
    private sealed record PreparedEntry(
        PackImageEntry Entry,
        IReadOnlyList<AssetPack.Writer.EncodedFrame> Frames,
        IReadOnlyList<AssetTransform> Transforms);
}
