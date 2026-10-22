namespace Gallean.Assets;

public sealed record AssetManifestAnimation(
    string Id,
    AssetAnimationMetadata Metadata,
    IReadOnlyList<AssetManifestAnimationFrame> Frames,
    IReadOnlyList<AssetTransform> Transforms);
