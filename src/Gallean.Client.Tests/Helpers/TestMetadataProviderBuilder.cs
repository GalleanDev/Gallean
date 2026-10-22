namespace Gallean.Client.Tests;

internal sealed class TestMetadataProviderBuilder
{
    private sealed class Provider(
        IReadOnlyDictionary<AssetSpriteKey, AssetSpriteMetadata> sprites,
        IReadOnlyDictionary<AssetAnimationKey, AssetAnimationMetadata> animations,
        IReadOnlyDictionary<AssetSoundKey, AssetSoundMetadata> sounds) : IAssetMetadataProvider
    {
        public AssetSpriteMetadata? GetOptionalSpriteMetadata(AssetSpriteKey key)
        {
            if (sprites.TryGetValue(key, out AssetSpriteMetadata metadata))
            {
                return metadata;
            }
            else
            {
                return null;
            }
        }

        public AssetAnimationMetadata? GetOptionalAnimationMetadata(AssetAnimationKey key)
        {
            if (animations.TryGetValue(key, out AssetAnimationMetadata metadata))
            {
                return metadata;
            }
            else
            {
                return null;
            }
        }

        public AssetSoundMetadata? GetOptionalSoundMetadata(AssetSoundKey key)
        {
            if (sounds.TryGetValue(key, out AssetSoundMetadata metadata))
            {
                return metadata;
            }
            else
            {
                return null;
            }
        }
    }

    private readonly Dictionary<AssetAnimationKey, AssetAnimationMetadata> animations = [];
    private readonly Dictionary<AssetSoundKey, AssetSoundMetadata> sounds = [];
    private readonly Dictionary<AssetSpriteKey, AssetSpriteMetadata> sprites = [];

    public TestMetadataProviderBuilder WithSprite(AssetSpriteKey key, AssetSpriteMetadata metadata)
    {
        sprites[key] = metadata;
        return this;
    }

    public TestMetadataProviderBuilder WithAnimation(AssetAnimationKey key, AssetAnimationMetadata metadata)
    {
        animations[key] = metadata;
        return this;
    }

    public TestMetadataProviderBuilder WithSound(AssetSoundKey key, AssetSoundMetadata metadata)
    {
        sounds[key] = metadata;
        return this;
    }

    public IAssetMetadataProvider Build()
    {
        return new Provider(sprites, animations, sounds);
    }
}
