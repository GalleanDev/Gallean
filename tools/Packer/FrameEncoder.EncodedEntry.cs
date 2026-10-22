namespace Packer;

internal static partial class FrameEncoder
{
    public sealed record EncodedEntry(
        IReadOnlyList<AssetPack.Writer.EncodedImage> Images,
        IReadOnlyList<AssetSpriteMetadata> Metadata,
        IReadOnlyList<AssetTransform> Transforms);
}
