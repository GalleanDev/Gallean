namespace Packer;

internal sealed class UnitShadowFilter : ISpriteFilter
{
    private const int Radius = 3;

    private static readonly Guid FilterType = new("b3471958-d5db-4d7a-b82f-400eb5bd8e8b");

    public Guid Type => FilterType;

    public bool Accepts(AssetSpriteMetadata metadata)
    {
        return metadata.PixelScale == 1;
    }

    public IReadOnlyList<AssetSprite> Apply(IReadOnlyList<AssetSprite> frames, SpriteFilterContext context)
    {
        AssetKey bodyKey = GetBodyKey(context.Key);

        IReadOnlyList<AssetSprite>? bodies = context.Source.ReadFrames(bodyKey);

        if (bodies is null)
        {
            throw new InvalidOperationException($"Unit shadow '{context.Key.CanonicalId}' has no frames.");
        }

        List<AssetSprite> result = [];

        for (int index = 0; index < frames.Count; index++)
        {
            AssetSprite body = bodies[index * bodies.Count / frames.Count];

            result.Add(Process(frames[index], body));
        }

        return result;
    }

    private static AssetKey GetBodyKey(AssetKey key)
    {
        return key switch
        {
            AssetAnimationKey.BattleUnitShadow shadow => new AssetAnimationKey.BattleUnit(shadow.DefinitionId, shadow.Pose, shadow.Side),
            AssetAnimationKey.StackUnitShadow shadow => new AssetAnimationKey.StackUnit(shadow.DefinitionId, shadow.Pose, shadow.Direction),
            _ => throw new ArgumentOutOfRangeException(nameof(key), key, null)
        };
    }

    private static AssetSprite Process(AssetSprite sprite, AssetSprite body)
    {
        AssetSprite canvas = SpriteOperations.AddMargin(sprite, Radius);

        int width = canvas.Metadata.PixelWidth;
        int height = canvas.Metadata.PixelHeight;
        int bodyLeft = body.Metadata.OffsetX - canvas.Metadata.OffsetX;
        int bodyTop = body.Metadata.OffsetY - canvas.Metadata.OffsetY;

        byte[] pixels = canvas.Pixels.ToArray();

        for (int step = 0; step < Radius; step++)
        {
            byte[] grown = (byte[])pixels.Clone();

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int offset = ((y * width) + x) * 4;

                    if (pixels[offset + 3] == 0 && IsVisible(body, x - bodyLeft, y - bodyTop))
                    {
                        int neighbor = FindVisibleNeighbor(pixels, width, height, x, y);

                        if (neighbor >= 0)
                        {
                            pixels.AsSpan(neighbor, 4).CopyTo(grown.AsSpan(offset));
                        }
                    }
                }
            }

            pixels = grown;
        }

        return canvas with { Pixels = pixels };
    }

    private static bool IsVisible(AssetSprite sprite, int x, int y)
    {
        int width = sprite.Metadata.PixelWidth;
        int height = sprite.Metadata.PixelHeight;

        if (x >= 0 && x < width && y >= 0 && y < height)
        {
            return sprite.Pixels.Span[(((y * width) + x) * 4) + 3] > 0;
        }
        else
        {
            return false;
        }
    }

    private static int FindVisibleNeighbor(byte[] pixels, int width, int height, int x, int y)
    {
        (int X, int Y)[] neighbors = [(x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)];

        foreach ((int neighborX, int neighborY) in neighbors)
        {
            if (neighborX >= 0 && neighborX < width && neighborY >= 0 && neighborY < height)
            {
                int offset = ((neighborY * width) + neighborX) * 4;

                if (pixels[offset + 3] > 0)
                {
                    return offset;
                }
            }
        }

        return -1;
    }
}
