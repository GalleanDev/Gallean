namespace Gallean.Assets;

public sealed record AssetSprite<TPixels>(AssetSpriteMetadata Metadata, TPixels Pixels);
