namespace Packer;

internal sealed class CursorFilter : ISpriteFilter
{
    private static readonly Guid FilterType = new("e3eb0872-6357-4c51-ab30-93a2db931dc5");

    public Guid Type => FilterType;

    public bool Accepts(AssetSpriteMetadata metadata)
    {
        return metadata.PixelScale == 1;
    }

    public IReadOnlyList<AssetSprite> Apply(IReadOnlyList<AssetSprite> frames, SpriteFilterContext context)
    {
        AssetAnimationKey.Cursor cursor = context.Key switch
        {
            AssetAnimationKey.Cursor key => key,
            _ => throw new ArgumentOutOfRangeException(nameof(context), context.Key, null)
        };

        if (cursor.Overlay is null)
        {
            return frames;
        }

        IReadOnlyList<AssetSprite>? arrowFrames = context.Source.ReadFrames(cursor.Arrow);

        if (arrowFrames is null || arrowFrames.Count != 1)
        {
            throw new InvalidOperationException($"Unexpected frame count for '{cursor.Arrow.CanonicalId}'.");
        }

        List<AssetSprite> result = [];

        foreach (AssetSprite frame in frames)
        {
            result.Add(Erase(frame, arrowFrames[0]));
        }

        return result;
    }

    private static AssetSprite Erase(AssetSprite sprite, AssetSprite arrow)
    {
        int width = sprite.Metadata.PixelWidth;
        int height = sprite.Metadata.PixelHeight;
        int arrowWidth = arrow.Metadata.PixelWidth;
        int arrowHeight = arrow.Metadata.PixelHeight;
        int shiftX = sprite.Metadata.OffsetX - arrow.Metadata.OffsetX;
        int shiftY = sprite.Metadata.OffsetY - arrow.Metadata.OffsetY;
        ReadOnlySpan<byte> arrowPixels = arrow.Pixels.Span;
        byte[] pixels = [.. sprite.Pixels.Span];

        for (int y = Math.Max(0, -shiftY); y < Math.Min(height, arrowHeight - shiftY); y++)
        {
            for (int x = Math.Max(0, -shiftX); x < Math.Min(width, arrowWidth - shiftX); x++)
            {
                int arrowOffset = (((y + shiftY) * arrowWidth) + x + shiftX) * 4;

                if (arrowPixels[arrowOffset + 3] > 0)
                {
                    pixels.AsSpan(((y * width) + x) * 4, 4).Clear();
                }
            }
        }

        return sprite with { Pixels = pixels };
    }
}
