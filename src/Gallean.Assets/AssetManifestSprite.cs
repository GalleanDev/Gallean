namespace Gallean.Assets;

public sealed record AssetManifestSprite(
    string Id,
    AssetSpriteMetadata Metadata,
    long Offset,
    int Length,
    IReadOnlyList<AssetTransform> Transforms,
    IReadOnlyList<AssetManifestSpriteLayer> Layers);
