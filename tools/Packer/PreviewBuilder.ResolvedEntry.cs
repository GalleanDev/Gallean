namespace Packer;

internal sealed partial class PreviewBuilder
{
    private sealed record ResolvedEntry(
        string Id,
        IReadOnlyList<AssetSpriteMetadata> FrameMetadata,
        IReadOnlyList<EnhancementCache.Key> FrameKeys,
        IReadOnlyList<string> MissingFrames);
}
