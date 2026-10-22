namespace GameContent;

internal static partial class SpriteOperations
{
    public static AssetSprite? Composite(IReadOnlyList<RawBitmap> parts)
    {
        if (parts.Count <= 0)
        {
            return null;
        }

        int width = 0;
        int height = 0;

        foreach (RawBitmap part in parts)
        {
            width = Math.Max(width, part.OriginalWidth);
            height = Math.Max(height, part.OriginalHeight);
        }

        byte[] pixels = new byte[width * height * 4];

        foreach (RawBitmap part in parts)
        {
            BlendSourceOver(
                pixels,
                width,
                height,
                part.Data,
                part.Bounds.Width,
                part.Bounds.Height,
                part.Bounds.X,
                part.Bounds.Y);
        }

        return new AssetSprite(new AssetSpriteMetadata(width, height, width, height, 0, 0, Additive: false), pixels);
    }

    public static AssetSprite SourceOver(AssetSprite source, AssetSprite destination, int offsetX, int offsetY)
    {
        byte[] pixels = destination.Pixels.ToArray();

        BlendSourceOver(
            pixels,
            destination.Metadata.PixelWidth,
            destination.Metadata.PixelHeight,
            source.Pixels.Span,
            source.Metadata.PixelWidth,
            source.Metadata.PixelHeight,
            offsetX,
            offsetY);

        return destination with { Pixels = pixels };
    }

    public static AssetSprite Crop(AssetSprite sprite, int sourceX, int sourceY, int width, int height)
    {
        byte[] pixels = new byte[width * height * 4];

        for (int y = 0; y < height; y++)
        {
            int sourceRow = (((sourceY + y) * sprite.Metadata.PixelWidth) + sourceX) * 4;
            sprite.Pixels.Span.Slice(sourceRow, width * 4).CopyTo(pixels.AsSpan(y * width * 4));
        }

        return new AssetSprite(
            new AssetSpriteMetadata(width, height, width, height, 0, 0, Additive: sprite.Metadata.Additive),
            pixels);
    }

    public static ContentBox GetContentBox(IReadOnlyList<AssetSprite> frames)
    {
        int width = frames[0].Metadata.PixelWidth;
        int height = frames[0].Metadata.PixelHeight;
        ContentBox box = ContentBox.Empty;

        foreach (AssetSprite frame in frames)
        {
            box = box.Union(GetContentBox(frame.Pixels.Span, width, 0, 0, width, height));
        }

        return box;
    }

    public static IReadOnlyList<AssetSprite> CropFramesToContent(
        IReadOnlyList<AssetSprite> frames,
        ContentOrigin origin,
        int margin)
    {
        int canvasWidth = frames[0].Metadata.PixelWidth;
        int canvasHeight = frames[0].Metadata.PixelHeight;
        ContentBox bounds = GetContentBox(frames);

        (int originX, int originY) = origin switch
        {
            ContentOrigin.Canvas => (0, 0),
            ContentOrigin.Center => (canvasWidth / 2, canvasHeight / 2)
        };

        List<AssetSprite> cropped = [];

        foreach (AssetSprite frame in frames)
        {
            ContentBox box = GetContentBox(frame.Pixels.Span, canvasWidth, 0, 0, canvasWidth, canvasHeight);

            if (!box.IsEmpty)
            {
                int minX = Math.Max(0, box.MinX - margin);
                int minY = Math.Max(0, box.MinY - margin);
                int maxX = Math.Min(canvasWidth - 1, box.MinX + box.Width - 1 + margin);
                int maxY = Math.Min(canvasHeight - 1, box.MinY + box.Height - 1 + margin);

                AssetSprite content = Crop(frame, minX, minY, maxX - minX + 1, maxY - minY + 1);
                cropped.Add(content.WithOffset(minX - originX, minY - originY));
            }
            else
            {
                AssetSpriteMetadata metadata = new(1, 1, 1, 1, bounds.MinX - originX, bounds.MinY - originY, frame.Metadata.Additive);
                cropped.Add(new AssetSprite(metadata, new byte[4]));
            }
        }

        return cropped;
    }

    public static AssetSprite ClipToCanvas(RawBitmap bitmap, int canvasWidth, int canvasHeight, int offsetX, int offsetY)
    {
        int placedLeft = offsetX + bitmap.Bounds.X;
        int placedTop = offsetY + bitmap.Bounds.Y;

        int left = Math.Max(0, -placedLeft);
        int top = Math.Max(0, -placedTop);
        int right = Math.Min(bitmap.Bounds.Width, canvasWidth - placedLeft);
        int bottom = Math.Min(bitmap.Bounds.Height, canvasHeight - placedTop);

        ContentBox box = ContentBox.Empty;

        if (right > left && bottom > top)
        {
            box = GetContentBox(bitmap.Data, bitmap.Bounds.Width, left, top, right, bottom);
        }

        if (box.IsEmpty)
        {
            return new AssetSprite(new AssetSpriteMetadata(1, 1, 1, 1, 0, 0, Additive: false), new byte[4]);
        }

        byte[] pixels = new byte[box.Width * box.Height * 4];

        for (int row = 0; row < box.Height; row++)
        {
            int sourceOffset = (((box.MinY + row) * bitmap.Bounds.Width) + box.MinX) * 4;
            Buffer.BlockCopy(bitmap.Data, sourceOffset, pixels, row * box.Width * 4, box.Width * 4);
        }

        AssetSpriteMetadata metadata = new(
            box.Width,
            box.Height,
            box.Width,
            box.Height,
            placedLeft + box.MinX,
            placedTop + box.MinY,
            Additive: bitmap.Additive);

        return new AssetSprite(metadata, pixels);
    }

    private static ContentBox GetContentBox(ReadOnlySpan<byte> pixels, int stride, int left, int top, int right, int bottom)
    {
        int minX = right;
        int minY = bottom;
        int maxX = left - 1;
        int maxY = top - 1;

        for (int y = top; y < bottom; y++)
        {
            for (int x = left; x < right; x++)
            {
                if (pixels[(((y * stride) + x) * 4) + 3] > 0)
                {
                    minX = Math.Min(minX, x);
                    maxX = Math.Max(maxX, x);
                    minY = Math.Min(minY, y);
                    maxY = Math.Max(maxY, y);
                }
            }
        }

        if (maxX < minX)
        {
            return ContentBox.Empty;
        }

        return new ContentBox(minX, minY, maxX - minX + 1, maxY - minY + 1);
    }

    private static void BlendSourceOver(
        byte[] destination,
        int destinationWidth,
        int destinationHeight,
        ReadOnlySpan<byte> source,
        int sourceWidth,
        int sourceHeight,
        int offsetX,
        int offsetY)
    {
        for (int y = 0; y < sourceHeight; y++)
        {
            int targetY = offsetY + y;

            if (targetY < 0 || targetY >= destinationHeight)
            {
                continue;
            }

            for (int x = 0; x < sourceWidth; x++)
            {
                int targetX = offsetX + x;

                if (targetX < 0 || targetX >= destinationWidth)
                {
                    continue;
                }

                int sourceOffset = ((y * sourceWidth) + x) * 4;
                byte sourceAlpha = source[sourceOffset + 3];

                if (sourceAlpha <= 0)
                {
                    continue;
                }

                int target = ((targetY * destinationWidth) + targetX) * 4;

                if (sourceAlpha == byte.MaxValue)
                {
                    source.Slice(sourceOffset, 4).CopyTo(destination.AsSpan(target));
                }
                else
                {
                    BlendPixel(destination, target, source, sourceOffset, sourceAlpha);
                }
            }
        }
    }

    private static void BlendPixel(byte[] destination, int target, ReadOnlySpan<byte> source, int offset, byte alpha)
    {
        int inverseAlpha = byte.MaxValue - alpha;
        byte destinationAlpha = destination[target + 3];
        int weightedAlpha = destinationAlpha * inverseAlpha / byte.MaxValue;
        int finalAlpha = alpha + weightedAlpha;

        if (finalAlpha != 0)
        {
            destination[target] =
                (byte)((source[offset] * alpha + destination[target] * weightedAlpha) / finalAlpha);
            destination[target + 1] =
                (byte)((source[offset + 1] * alpha + destination[target + 1] * weightedAlpha) / finalAlpha);
            destination[target + 2] =
                (byte)((source[offset + 2] * alpha + destination[target + 2] * weightedAlpha) / finalAlpha);
            destination[target + 3] =
                (byte)finalAlpha;
        }
    }
}
