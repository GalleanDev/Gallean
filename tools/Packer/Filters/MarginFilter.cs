namespace Packer;

internal sealed class MarginFilter(int margin) : ISpriteFilter
{
    private static readonly Guid FilterType = new("c4e0a6b2-8f31-47da-9b05-1de8f2a7c634");

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
            result.Add(SpriteOperations.AddMargin(frame, margin));
        }

        return result;
    }
}
