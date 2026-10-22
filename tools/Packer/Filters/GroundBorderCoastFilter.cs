namespace Packer;

internal sealed class GroundBorderCoastFilter : ISpriteFilter
{
    private const int DitherNeighbors = 3;
    private const byte DitherAlpha = 128;

    private static readonly Guid FilterType = new("291064ae-e5f4-4ad2-9eb0-f98177561846");

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
        AssetSprite tile = GroundBorderTile.Clip(sprite);
        int width = tile.Metadata.PixelWidth;
        int height = tile.Metadata.PixelHeight;
        ReadOnlySpan<byte> source = tile.Pixels.Span;
        byte[] pixels = source.ToArray();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                List<int> opposites = [];

                int target = ((y * width) + x) * 4;
                bool isOpaque = source[target + 3] > 0;
                (int X, int Y)[] neighbors = [(x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)];

                foreach ((int neighborX, int neighborY) in neighbors)
                {
                    int neighbor = ((neighborY * width) + neighborX) * 4;

                    if (GroundBorderTile.IsInside(neighborX, neighborY, width, height) && source[neighbor + 3] > 0 != isOpaque)
                    {
                        opposites.Add(neighbor);
                    }
                }

                if (GroundBorderTile.IsInside(x, y, width, height) && opposites.Count >= DitherNeighbors)
                {
                    if (!isOpaque)
                    {
                        for (int channel = 0; channel < 3; channel++)
                        {
                            int sum = 0;

                            foreach (int neighbor in opposites)
                            {
                                sum += source[neighbor + channel];
                            }

                            pixels[target + channel] = (byte)(sum / opposites.Count);
                        }
                    }

                    pixels[target + 3] = DitherAlpha;
                }
            }
        }

        return tile with { Pixels = pixels };
    }
}
