namespace Packer;

internal sealed class AdditiveTrimFilter(int margin) : ISpriteFilter
{
    private const int MaxInvisibleAlpha = 8;

    private static readonly Guid FilterType = new("73de3ef1-c6ca-4304-be09-46bbc27d5446");

    public Guid Type => FilterType;

    public bool Accepts(AssetSpriteMetadata metadata)
    {
        return metadata is { Additive: true, PixelScale: 1 };
    }

    public IReadOnlyList<AssetSprite> Apply(IReadOnlyList<AssetSprite> frames, SpriteFilterContext context)
    {
        List<(int Left, int Top, int Right, int Bottom)> boxes = [];

        (int Left, int Top, int Right, int Bottom) union = (int.MaxValue, int.MaxValue, int.MinValue, int.MinValue);

        foreach (AssetSprite frame in frames)
        {
            (int Left, int Top, int Right, int Bottom) box = GetVisibleBox(frame);

            boxes.Add(box);

            if (box.Right > box.Left)
            {
                union = (
                    Left: Math.Min(union.Left, box.Left),
                    Top: Math.Min(union.Top, box.Top),
                    Right: Math.Max(union.Right, box.Right),
                    Bottom: Math.Max(union.Bottom, box.Bottom));
            }
        }

        List<AssetSprite> result = [];

        for (int index = 0; index < frames.Count; index++)
        {
            AssetSprite frame = frames[index];
            (int Left, int Top, int Right, int Bottom) box = boxes[index];

            if (box.Right > box.Left)
            {
                result.Add(Crop(frame, box));
            }
            else if (union.Right > union.Left)
            {
                result.Add(MakeEmpty(frame, union.Left, union.Top));
            }
            else
            {
                result.Add(MakeEmpty(frame, frame.Metadata.OffsetX, frame.Metadata.OffsetY));
            }
        }

        return result;
    }

    private static (int Left, int Top, int Right, int Bottom) GetVisibleBox(AssetSprite frame)
    {
        ReadOnlySpan<byte> pixels = frame.Pixels.Span;
        int width = frame.Metadata.PixelWidth;
        int height = frame.Metadata.PixelHeight;

        int left = width;
        int top = height;
        int right = 0;
        int bottom = 0;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (pixels[(((y * width) + x) * 4) + 3] > MaxInvisibleAlpha)
                {
                    left = Math.Min(left, x);
                    top = Math.Min(top, y);
                    right = Math.Max(right, x + 1);
                    bottom = Math.Max(bottom, y + 1);
                }
            }
        }

        return (frame.Metadata.OffsetX + left, frame.Metadata.OffsetY + top, frame.Metadata.OffsetX + right, frame.Metadata.OffsetY + bottom);
    }

    private AssetSprite Crop(AssetSprite frame, (int Left, int Top, int Right, int Bottom) box)
    {
        AssetSpriteMetadata metadata = frame.Metadata;
        int left = Math.Max(0, box.Left - metadata.OffsetX - margin);
        int top = Math.Max(0, box.Top - metadata.OffsetY - margin);
        int right = Math.Min(metadata.PixelWidth, box.Right - metadata.OffsetX + margin);
        int bottom = Math.Min(metadata.PixelHeight, box.Bottom - metadata.OffsetY + margin);

        return SpriteOperations.Crop(frame, left, top, right - left, bottom - top);
    }

    private static AssetSprite MakeEmpty(AssetSprite frame, int offsetX, int offsetY)
    {
        AssetSpriteMetadata metadata = new(1, 1, 1, 1, offsetX, offsetY, frame.Metadata.Additive);

        return new AssetSprite(metadata, new byte[4]);
    }
}
