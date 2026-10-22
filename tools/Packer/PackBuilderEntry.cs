using GameContent;

namespace Packer;

internal abstract record PackBuilderEntry(AssetKey Key, bool Required)
{
    public sealed record GameAsset(GameAssetDescriptor Descriptor)
        : PackBuilderEntry(Descriptor.Key, Descriptor.Required);

    public sealed record UnitSound(GameAssetDescriptor.Sound Descriptor, GameAssetDescriptor.Sound RecordDescriptor)
        : PackBuilderEntry(Descriptor.Key, Descriptor.Required);

    public sealed record UnitAttack(GameAssetDescriptor.Animation Descriptor, GameAssetDescriptor.Animation RecordDescriptor)
        : PackBuilderEntry(Descriptor.Key, Descriptor.Required);

    public sealed record Background(AssetSpriteKey BackgroundKey, int SceneWidth)
        : PackBuilderEntry(BackgroundKey, Required: true);

    public sealed record BackgroundOverlay(AssetSpriteKey.BattleBackground BackgroundKey, int Layer)
        : PackBuilderEntry(new AssetAnimationKey.BattleBackgroundOverlay(BackgroundKey.Location, BackgroundKey.Variant, Layer), false);
}
