namespace Packer;

internal sealed class ClearFilter : ISpriteFilter
{
    private static readonly Guid FilterType = new("5dfbc6a6-9c9d-4a13-ac5d-2b07786caca9");

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
            result.Add(frame with { Pixels = new byte[frame.Pixels.Length] });
        }

        return result;
    }
}
