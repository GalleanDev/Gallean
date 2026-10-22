namespace Packer;

internal sealed class MainMenuBackgroundOverlayFilter : ISpriteFilter
{
    private const int SceneWidth = 800;
    private const int SceneHeight = 600;
    private const int FadeWidth = 20;

    private static readonly Guid FilterType = new("5a0e3c71-8b2d-4f96-a1c4-7d93e6b20f58");

    public Guid Type => FilterType;

    public bool Accepts(AssetSpriteMetadata metadata)
    {
        return metadata.PixelScale >= 2;
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
        AssetSpriteMetadata metadata = sprite.Metadata;
        int width = metadata.PixelWidth;
        int height = metadata.PixelHeight;
        float fade = FadeWidth * metadata.PixelScale;
        bool fadeLeft = metadata.OffsetX > 0;
        bool fadeTop = metadata.OffsetY > 0;
        bool fadeRight = metadata.OffsetX + metadata.Width < SceneWidth;
        bool fadeBottom = metadata.OffsetY + metadata.Height < SceneHeight;
        byte[] pixels = sprite.Pixels.ToArray();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int distance = int.MaxValue;

                if (fadeLeft)
                {
                    distance = Math.Min(distance, x);
                }

                if (fadeRight)
                {
                    distance = Math.Min(distance, width - 1 - x);
                }

                if (fadeTop)
                {
                    distance = Math.Min(distance, y);
                }

                if (fadeBottom)
                {
                    distance = Math.Min(distance, height - 1 - y);
                }

                int offset = ((y * width) + x) * 4;
                float weight = Smooth(distance / fade);

                pixels[offset + 3] = (byte)MathF.Round(pixels[offset + 3] * weight);
            }
        }

        return sprite with { Pixels = pixels };
    }

    private static float Smooth(float value)
    {
        float clamped = Math.Clamp(value, 0, 1);

        return clamped * clamped * (3 - (2 * clamped));
    }
}
