namespace Packer.Tests;

internal sealed class TestSpriteFilterSource(Dictionary<AssetKey, IReadOnlyList<AssetSprite>> frames) : ISpriteFilterSource
{
    public IReadOnlyList<AssetSprite>? ReadFrames(AssetKey key)
    {
        return frames.GetValueOrDefault(key);
    }
}
