namespace Packer;

internal sealed class GroundBorderMaskEdgeFilter : ISpriteFilter
{
    private const int ConnectedEdgeBand = 4;

    private static readonly Guid FilterType = new("a6c4beb5-ecfb-4b7e-8773-fa7f134317c3");

    public Guid Type => FilterType;

    public bool Accepts(AssetSpriteMetadata metadata)
    {
        return metadata.PixelScale == 1;
    }

    public IReadOnlyList<AssetSprite> Apply(IReadOnlyList<AssetSprite> frames, SpriteFilterContext context)
    {
        AssetSpriteKey.GroundBorderEdges edges = GetEdges(context.Key);

        List<AssetSprite> result = [];

        foreach (AssetSprite frame in frames)
        {
            result.Add(Process(frame, edges));
        }

        return result;
    }

    private static AssetSpriteKey.GroundBorderEdges GetEdges(AssetKey key)
    {
        if (key is AssetSpriteKey.GroundBorder { Shape: AssetSpriteKey.GroundBorderShape.Edges edges })
        {
            return edges.Value;
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(key), key, null);
        }
    }

    private static AssetSprite Process(AssetSprite sprite, AssetSpriteKey.GroundBorderEdges edges)
    {
        int width = sprite.Metadata.PixelWidth;
        int height = sprite.Metadata.PixelHeight;
        byte[] pixels = sprite.Pixels.ToArray();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (GroundBorderTile.IsInside(x, y, width, height) && IsConnectedEdge(x, y, width, height, edges))
                {
                    pixels.AsSpan(((y * width) + x) * 4, 4).Fill(byte.MaxValue);
                }
            }
        }

        return sprite with { Pixels = pixels };
    }

    private static bool IsConnectedEdge(int x, int y, int width, int height, AssetSpriteKey.GroundBorderEdges edges)
    {
        int horizontal = (2 * x) + 1 - width;
        int vertical = (2 * y) + 1 - height;

        AssetSpriteKey.GroundBorderEdges edge = (horizontal > 0, vertical > 0) switch
        {
            (true, false) => AssetSpriteKey.GroundBorderEdges.North,
            (true, true) => AssetSpriteKey.GroundBorderEdges.East,
            (false, true) => AssetSpriteKey.GroundBorderEdges.South,
            (false, false) => AssetSpriteKey.GroundBorderEdges.West
        };

        int distance = ((width * height) - (Math.Abs(horizontal) * height) - (Math.Abs(vertical) * width)) / height;

        return edges.HasFlag(edge) && distance < 2 * ConnectedEdgeBand;
    }
}
