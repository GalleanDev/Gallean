namespace Packer;

internal static class GroundBorderTile
{
    private const int DiamondOffsetX = -1;

    public static bool IsInside(int x, int y, int width, int height)
    {
        return (Math.Abs((2 * x) + 1 - width) * height) + (Math.Abs((2 * y) + 1 - height) * width) < width * height;
    }

    public static AssetSprite Clip(AssetSprite sprite)
    {
        ReadOnlySpan<byte> source = sprite.Pixels.Span;
        int width = sprite.Metadata.PixelWidth;
        int height = sprite.Metadata.PixelHeight;

        byte[] pixels = new byte[source.Length];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (IsInside(x, y, width, height))
                {
                    int target = ((y * width) + x) * 4;

                    source.Slice(target + (DiamondOffsetX * 4), 4).CopyTo(pixels.AsSpan(target, 4));
                }
            }
        }

        return sprite with { Pixels = pixels };
    }
}
