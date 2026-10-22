namespace Gallean.Assets;

public sealed record AssetAnimation<TPixels>(AssetAnimationMetadata Metadata, IReadOnlyList<AssetSprite<TPixels>> Frames);
