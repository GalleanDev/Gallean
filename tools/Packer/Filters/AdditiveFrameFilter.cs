namespace Packer;

internal sealed class AdditiveFrameFilter : ISpriteFilter
{
    private const int Margin = 8;

    private static readonly Guid FilterType = new("268df81e-7173-4aac-8653-a10cb1e295a5");

    public Guid Type => FilterType;

    public bool Accepts(AssetSpriteMetadata metadata)
    {
        return metadata is { Additive: true, PixelScale: 1 };
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

        for (int offset = 3; offset < pixels.Length; offset += 4)
        {
            pixels[offset] = 0;
        }

        return SpriteOperations.AddMargin(sprite with { Pixels = pixels }, Margin);
    }
}
