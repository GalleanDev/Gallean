namespace Packer;

internal sealed class FlattenFilter : ISpriteFilter
{
    private static readonly Guid FilterType = new("1cbf315d-c76d-4318-b158-a55762d323f1");

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
        byte[] pixels = sprite.Pixels.ToArray();

        for (int offset = 0; offset < pixels.Length; offset += 4)
        {
            byte alpha = pixels[offset + 3];

            pixels[offset] = (byte)(pixels[offset] * alpha / 255);
            pixels[offset + 1] = (byte)(pixels[offset + 1] * alpha / 255);
            pixels[offset + 2] = (byte)(pixels[offset + 2] * alpha / 255);
            pixels[offset + 3] = byte.MaxValue;
        }

        return sprite with { Pixels = pixels };
    }
}
