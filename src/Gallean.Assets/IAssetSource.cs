namespace Gallean.Assets;

public interface IAssetSource<TPixels> : IDisposable
{
    AssetSprite<TPixels>? TryGetSprite(AssetSpriteKey key);
    AssetSpriteMetadata? TryGetSpriteMetadata(AssetSpriteKey key);
    AssetSpriteLayer<TPixels>? TryGetSpriteLayer(AssetSpriteKey key, AssetSpriteLayerType type);

    AssetAnimation<TPixels>? TryGetAnimation(AssetAnimationKey key);
    AssetAnimationMetadata? TryGetAnimationMetadata(AssetAnimationKey key);
    IReadOnlyList<AssetSpriteLayer<TPixels>>? TryGetAnimationLayer(AssetAnimationKey key, AssetSpriteLayerType type);

    AssetSound? TryGetSound(AssetSoundKey key);
    AssetSoundMetadata? TryGetSoundMetadata(AssetSoundKey key);

    AssetStream? TryGetStream(AssetKey key);
}
