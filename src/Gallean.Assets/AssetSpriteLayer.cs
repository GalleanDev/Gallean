namespace Gallean.Assets;

public sealed record AssetSpriteLayer<TPixels>(int PixelWidth, int PixelHeight, TPixels Pixels);
