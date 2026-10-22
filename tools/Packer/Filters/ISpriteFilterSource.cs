namespace Packer;

internal interface ISpriteFilterSource
{
    IReadOnlyList<AssetSprite>? ReadFrames(AssetKey key);
}
