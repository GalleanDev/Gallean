using Gallean.Common;

namespace Gallean.Client.Tests;

internal sealed class NullAssetProvider : IAssetProvider, IStaticInstance<NullAssetProvider>
{
    public AssetCache<Sprite>.Lease? GetOptionalSprite(AssetSpriteKey key) => null;

    public AssetCache<Animation>.Lease? GetOptionalAnimation(AssetAnimationKey key) => null;

    public AssetCache<Sound>.Lease? GetOptionalSound(AssetSoundKey key) => null;

    public AssetSpriteMetadata? GetOptionalSpriteMetadata(AssetSpriteKey key) => null;

    public AssetAnimationMetadata? GetOptionalAnimationMetadata(AssetAnimationKey key) => null;

    public AssetSoundMetadata? GetOptionalSoundMetadata(AssetSoundKey key) => null;

    public AssetStream? GetOptionalStream(AssetKey key) => null;
}
