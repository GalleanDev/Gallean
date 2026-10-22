namespace Packer;

internal static class SpriteOperations
{
    public static AssetSprite AddMargin(AssetSprite sprite, int margin)
    {
        AssetSpriteMetadata metadata = sprite.Metadata;

        int width = metadata.PixelWidth + (2 * margin);
        int height = metadata.PixelHeight + (2 * margin);

        byte[] pixels = new byte[width * height * 4];

        for (int y = 0; y < metadata.PixelHeight; y++)
        {
            int sourceRow = y * metadata.PixelWidth * 4;
            int targetRow = (((y + margin) * width) + margin) * 4;

            sprite.Pixels.Span.Slice(sourceRow, metadata.PixelWidth * 4).CopyTo(pixels.AsSpan(targetRow));
        }

        AssetSpriteMetadata newMetadata = new(
            width,
            height,
            width,
            height,
            metadata.OffsetX - margin,
            metadata.OffsetY - margin,
            metadata.Additive);

        return new AssetSprite(newMetadata, pixels);
    }

    public static AssetSprite Crop(AssetSprite sprite, int left, int top, int width, int height)
    {
        AssetSpriteMetadata metadata = sprite.Metadata;
        int scale = metadata.PixelScale;
        byte[] pixels = new byte[width * height * 4];

        for (int y = 0; y < height; y++)
        {
            int sourceRow = (((top + y) * metadata.PixelWidth) + left) * 4;
            sprite.Pixels.Span.Slice(sourceRow, width * 4).CopyTo(pixels.AsSpan(y * width * 4));
        }

        AssetSpriteMetadata cropped = new(
            width / scale,
            height / scale,
            width,
            height,
            metadata.OffsetX + (left / scale),
            metadata.OffsetY + (top / scale),
            metadata.Additive);

        return new AssetSprite(cropped, pixels);
    }
}
