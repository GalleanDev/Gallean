namespace Gallean.Assets;

public sealed record AssetManifestAnimationFrame(
    AssetSpriteMetadata Metadata,
    long Offset,
    int Length,
    IReadOnlyList<AssetManifestSpriteLayer> Layers);
