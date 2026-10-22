namespace Packer;

internal sealed class GroundBorderMaskFilter : ISpriteFilter
{
    private static readonly Guid FilterType = new("bada57c5-1733-4680-9535-380c4ef4a4ac");

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

        byte[] pixels = tile.Pixels.ToArray();

        for (int offset = 0; offset < pixels.Length; offset += 4)
        {
            byte luminance = pixels[offset];

            if (luminance > 0)
            {
                pixels[offset] = byte.MaxValue;
                pixels[offset + 1] = byte.MaxValue;
                pixels[offset + 2] = byte.MaxValue;
            }
            else
            {
                pixels[offset + 1] = 0;
                pixels[offset + 2] = 0;
            }

            pixels[offset + 3] = luminance;
        }

        return tile with { Pixels = pixels };
    }
}
