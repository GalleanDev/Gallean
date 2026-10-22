namespace Packer;

internal interface ISpriteFilter
{
    Guid Type { get; }

    bool Accepts(AssetSpriteMetadata metadata);

    IReadOnlyList<AssetSprite> Apply(IReadOnlyList<AssetSprite> frames, SpriteFilterContext context);
}
