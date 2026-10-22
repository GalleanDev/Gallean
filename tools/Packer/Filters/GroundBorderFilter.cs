namespace Packer;

internal sealed class GroundBorderFilter : ISpriteFilter
{
    private static readonly Guid FilterType = new("26b5f3ec-4d76-4b41-ae3f-6d56cc366e1e");

    public Guid Type => FilterType;

    public bool Accepts(AssetSpriteMetadata metadata)
    {
        return metadata.PixelScale == 1;
    }

    public IReadOnlyList<AssetSprite> Apply(IReadOnlyList<AssetSprite> frames, SpriteFilterContext context)
    {
        List<AssetSprite> result = [];

        foreach (AssetSprite frame in frames)
        {
            result.Add(Process(frame));
        }

        return result;
    }

    private static AssetSprite Process(AssetSprite sprite)
    {
        ReadOnlySpan<byte> source = sprite.Pixels.Span;
        int width = sprite.Metadata.PixelWidth;
        int height = sprite.Metadata.PixelHeight;

        byte[] pixels = source.ToArray();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int newX = x + ((2 * x) + 1 < width ? 1 : -1);
                int newY = y + ((2 * y) + 1 < height ? 1 : -1);
                (int X, int Y)[] candidates = [(newX, y), (x, newY)];

                if (!GroundBorderTile.IsInside(x, y, width, height))
                {
                    foreach ((int candidateX, int candidateY) in candidates)
                    {
                        if (GroundBorderTile.IsInside(candidateX, candidateY, width, height))
                        {
                            source.Slice(((candidateY * width) + candidateX) * 4, 4).CopyTo(pixels.AsSpan(((y * width) + x) * 4, 4));

                            break;
                        }
                    }
                }
            }
        }

        return sprite with { Pixels = pixels };
    }
}
