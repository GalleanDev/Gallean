namespace GameContent;

public abstract record GameAssetDescriptor(AssetKey Key, bool Required)
{
    public sealed record Sprite : GameAssetDescriptor
    {
        internal Sprite(AssetKey key, bool required, Func<GameAssetSource, AssetSprite?> extract)
            : base(key, required)
        {
            Extract = extract;
        }

        internal Func<GameAssetSource, AssetSprite?> Extract { get; }
    }

    public sealed record Animation : GameAssetDescriptor
    {
        internal Animation(AssetKey key, bool required, Func<GameAssetSource, AssetAnimation?> extract)
            : base(key, required)
        {
            Extract = extract;
        }

        internal Func<GameAssetSource, AssetAnimation?> Extract { get; }
    }

    public sealed record Sound : GameAssetDescriptor
    {
        internal Sound(AssetKey key, bool required, Func<GameAssetSource, AssetSound?> extract)
            : base(key, required)
        {
            Extract = extract;
        }

        internal Func<GameAssetSource, AssetSound?> Extract { get; }
    }

    internal static Sprite RequiredSprite(AssetSpriteKey key, Func<GameAssetSource, AssetSprite?> extract)
    {
        return new Sprite(key, required: true, extract);
    }

    internal static Sprite OptionalSprite(AssetSpriteKey key, Func<GameAssetSource, AssetSprite?> extract)
    {
        return new Sprite(key, required: false, extract);
    }

    internal static Animation RequiredAnimation(AssetAnimationKey key, Func<GameAssetSource, AssetAnimation?> extract)
    {
        return new Animation(key, required: true, extract);
    }

    internal static Animation OptionalAnimation(AssetAnimationKey key, Func<GameAssetSource, AssetAnimation?> extract)
    {
        return new Animation(key, required: false, extract);
    }

    internal static Sound OptionalSound(AssetSoundKey key, Func<GameAssetSource, AssetSound?> extract)
    {
        return new Sound(key, required: false, extract);
    }
}
