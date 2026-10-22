namespace Gallean.Client;

internal interface IAssetProvider : IAssetMetadataProvider
{
    AssetCache<Sprite>.Lease GetRequiredSprite(AssetSpriteKey key)
    {
        AssetCache<Sprite>.Lease? sprite = GetOptionalSprite(key);

        if (sprite is null)
        {
            throw new KeyNotFoundException($"No asset data found for key '{key}'.");
        }

        return sprite;
    }

    AssetCache<Sprite>.Lease? GetOptionalSprite(AssetSpriteKey key);

    AssetCache<Animation>.Lease GetRequiredAnimation(AssetAnimationKey key)
    {
        AssetCache<Animation>.Lease? animation = GetOptionalAnimation(key);

        if (animation is null)
        {
            throw new KeyNotFoundException($"No asset data found for key '{key}'.");
        }

        return animation;
    }

    AssetCache<Animation>.Lease? GetOptionalAnimation(AssetAnimationKey key);

    AssetCache<Sound>.Lease GetRequiredSound(AssetSoundKey key)
    {
        AssetCache<Sound>.Lease? sound = GetOptionalSound(key);

        if (sound is null)
        {
            throw new KeyNotFoundException($"No asset data found for key '{key}'.");
        }

        return sound;
    }

    AssetCache<Sound>.Lease? GetOptionalSound(AssetSoundKey key);

    AssetStream? GetOptionalStream(AssetKey key);
}
