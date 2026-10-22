namespace Gallean.Assets;

public readonly record struct AssetSpriteMetadata(int Width, int Height, int PixelWidth, int PixelHeight, int OffsetX, int OffsetY, bool Additive)
{
    public int PixelScale => Math.Max(1, PixelWidth / Math.Max(1, Width));
}
