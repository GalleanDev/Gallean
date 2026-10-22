namespace Gallean.Client;

internal interface IAssetMetadataProvider
{
    AssetSpriteMetadata GetRequiredSpriteMetadata(AssetSpriteKey key)
    {
        AssetSpriteMetadata? metadata = GetOptionalSpriteMetadata(key);

        if (metadata is null)
        {
            throw new KeyNotFoundException($"No asset data found for key '{key}'.");
        }

        return metadata.Value;
    }

    AssetSpriteMetadata? GetOptionalSpriteMetadata(AssetSpriteKey key);

    AssetAnimationMetadata GetRequiredAnimationMetadata(AssetAnimationKey key)
    {
        AssetAnimationMetadata? metadata = GetOptionalAnimationMetadata(key);

        if (metadata is null)
        {
            throw new KeyNotFoundException($"No asset data found for key '{key}'.");
        }

        return metadata.Value;
    }

    AssetAnimationMetadata? GetOptionalAnimationMetadata(AssetAnimationKey key);

    AssetSoundMetadata GetRequiredSoundMetadata(AssetSoundKey key)
    {
        AssetSoundMetadata? metadata = GetOptionalSoundMetadata(key);

        if (metadata is null)
        {
            throw new KeyNotFoundException($"No asset data found for key '{key}'.");
        }

        return metadata.Value;
    }

    AssetSoundMetadata? GetOptionalSoundMetadata(AssetSoundKey key);
}
