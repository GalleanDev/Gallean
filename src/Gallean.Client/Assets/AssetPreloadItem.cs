namespace Gallean.Client;

internal abstract record AssetPreloadItem
{
    public sealed record Sprite(AssetSpriteKey Key) : AssetPreloadItem
    {
        public override IDisposable? Preload(IAssetProvider assets)
        {
            return assets.GetOptionalSprite(Key);
        }
    }

    public sealed record Animation(AssetAnimationKey Key) : AssetPreloadItem
    {
        public override IDisposable? Preload(IAssetProvider assets)
        {
            return assets.GetOptionalAnimation(Key);
        }
    }

    public sealed record Sound(AssetSoundKey Key) : AssetPreloadItem
    {
        public override IDisposable? Preload(IAssetProvider assets)
        {
            return assets.GetOptionalSound(Key);
        }
    }

    public abstract IDisposable? Preload(IAssetProvider assets);
}
