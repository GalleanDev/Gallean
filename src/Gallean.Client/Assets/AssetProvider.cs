using Gallean.Common;
using Gallean.Configuration;
using SkiaSharp;

namespace Gallean.Client;

internal sealed partial class AssetProvider : IAssetProvider, IDisposable
{
    private readonly IAssetSource<SKImage> assetSource;
    private readonly AssetImageDecoder imageDecoder;
    private readonly GameSettingsSource gameSettingsSource;
    private readonly AssetCache<Sprite> spriteCache;
    private readonly AssetCache<Animation> animationCache;
    private readonly AssetCache<Sound> soundCache;

    private AssetProvider(
        IAssetSource<SKImage> assetSource,
        AssetImageDecoder imageDecoder,
        GameSettingsSource gameSettingsSource,
        GCMemoryInfo memoryInfo)
    {
        this.assetSource = assetSource;
        this.imageDecoder = imageDecoder;
        this.gameSettingsSource = gameSettingsSource;

        spriteCache = new AssetCache<Sprite>(GetSpriteCacheSize(memoryInfo), LoadSprite, MeasureSprite, DisposeSprite);
        animationCache = new AssetCache<Animation>(GetAnimationCacheSize(memoryInfo), LoadAnimation, MeasureAnimation, DisposeAnimation);
        soundCache = new AssetCache<Sound>(GetSoundCacheSize(memoryInfo), LoadSound, MeasureSound, DisposeSound);
    }

    private static long GetSpriteCacheSize(GCMemoryInfo memoryInfo)
    {
        return Math.Min(768L * 1024 * 1024, memoryInfo.TotalAvailableMemoryBytes / 32);
    }

    private static long GetAnimationCacheSize(GCMemoryInfo memoryInfo)
    {
        return Math.Min(3L * 1024 * 1024 * 1024, memoryInfo.TotalAvailableMemoryBytes / 8);
    }

    private static long GetSoundCacheSize(GCMemoryInfo memoryInfo)
    {
        return 128L * 1024 * 1024;
    }

    public AssetSpriteMetadata? GetOptionalSpriteMetadata(AssetSpriteKey key)
    {
        return assetSource.TryGetSpriteMetadata(key);
    }

    public AssetCache<Sprite>.Lease? GetOptionalSprite(AssetSpriteKey key)
    {
        return spriteCache.Acquire(key);
    }

    public AssetAnimationMetadata? GetOptionalAnimationMetadata(AssetAnimationKey key)
    {
        return assetSource.TryGetAnimationMetadata(key);
    }

    public AssetCache<Animation>.Lease? GetOptionalAnimation(AssetAnimationKey key)
    {
        return animationCache.Acquire(key);
    }

    public AssetSoundMetadata? GetOptionalSoundMetadata(AssetSoundKey key)
    {
        AssetSoundKey localizedKey = MakeLocalizedSoundKey(key);

        return assetSource.TryGetSoundMetadata(localizedKey);
    }

    public AssetCache<Sound>.Lease? GetOptionalSound(AssetSoundKey key)
    {
        AssetSoundKey localizedKey = MakeLocalizedSoundKey(key);

        return soundCache.Acquire(localizedKey);
    }

    public AssetStream? GetOptionalStream(AssetKey key)
    {
        if (key is AssetSoundKey soundKey)
        {
            AssetSoundKey localizedKey = MakeLocalizedSoundKey(soundKey);

            return assetSource.TryGetStream(localizedKey);
        }
        else
        {
            return assetSource.TryGetStream(key);
        }
    }

    public void SetMaxPixelScale(int maxPixelScale)
    {
        if (imageDecoder.MaxPixelScale != maxPixelScale)
        {
            imageDecoder.MaxPixelScale = maxPixelScale;
            spriteCache.Clear();
            animationCache.Clear();
        }
    }

    public void Dispose()
    {
        spriteCache.Dispose();
        animationCache.Dispose();
        soundCache.Dispose();
        assetSource.Dispose();
    }

    public static AssetProvider LoadDefault(GameSettingsSource settings, GCMemoryInfo memoryInfo)
    {
        AssetProvider? provider = TryLoadDefault(settings, memoryInfo);

        if (provider is null)
        {
            throw new InstalledAssetPackNotFoundException();
        }

        return provider;
    }

    public static AssetProvider? TryLoadDefault(GameSettingsSource settings, GCMemoryInfo memoryInfo)
    {
        AssetImageDecoder imageDecoder = new();
        AssetPackSource<SKImage>? source = LoadPackSource(imageDecoder);

        if (source is null)
        {
            return null;
        }

        return new AssetProvider(source, imageDecoder, settings, memoryInfo);
    }

    private static AssetPackSource<SKImage>? LoadPackSource(AssetImageDecoder imageDecoder)
    {
        IReadOnlyList<AssetPack>? packs = PackLoader.Load();

        if (packs is not null)
        {
            return new AssetPackSource<SKImage>(packs, imageDecoder);
        }
        else
        {
            return null;
        }
    }

    private AssetSoundKey MakeLocalizedSoundKey(AssetSoundKey key)
    {
        GameSettings settings = gameSettingsSource.Current;

        return key with { Language = key.Language ?? settings.EffectiveLanguage };
    }

    private Sprite? LoadSprite(AssetKey key)
    {
        AssetSpriteKey spriteKey = (AssetSpriteKey)key;

        AssetSprite<SKImage>? sprite = assetSource.TryGetSprite(spriteKey);
        if (sprite is null)
        {
            return null;
        }

        AssetSpriteLayer<SKImage>? normalLayer = assetSource.TryGetSpriteLayer(spriteKey, AssetSpriteLayerType.Normal);
        AssetSpriteLayer<SKImage>? depthLayer = assetSource.TryGetSpriteLayer(spriteKey, AssetSpriteLayerType.Depth);
        AssetSpriteLayer<SKImage>? albedoLayer = assetSource.TryGetSpriteLayer(spriteKey, AssetSpriteLayerType.Albedo);

        IReadOnlyList<IReadOnlyList<SpriteLight>> lights = SpriteLightBuilder.Build([sprite]);

        return CreateSprite(sprite, normalLayer, depthLayer, albedoLayer, lights[0]);
    }

    private Animation? LoadAnimation(AssetKey key)
    {
        AssetAnimationKey animationKey = (AssetAnimationKey)key;

        AssetAnimationMetadata? metadata = assetSource.TryGetAnimationMetadata(animationKey);
        if (metadata is null)
        {
            return null;
        }

        AssetAnimation<SKImage>? animation = assetSource.TryGetAnimation(animationKey);
        if (animation is null)
        {
            return null;
        }

        IReadOnlyList<AssetSpriteLayer<SKImage>>? normalLayers = assetSource.TryGetAnimationLayer(animationKey, AssetSpriteLayerType.Normal);
        IReadOnlyList<AssetSpriteLayer<SKImage>>? depthLayers = assetSource.TryGetAnimationLayer(animationKey, AssetSpriteLayerType.Depth);
        IReadOnlyList<AssetSpriteLayer<SKImage>>? albedoLayers = assetSource.TryGetAnimationLayer(animationKey, AssetSpriteLayerType.Albedo);
        IReadOnlyList<IReadOnlyList<SpriteLight>> lights = SpriteLightBuilder.Build(animation.Frames);

        List<Sprite> sprites = new(animation.Frames.Count);

        for (int index = 0; index < animation.Frames.Count; index++)
        {
            Sprite sprite = CreateSprite(
                animation.Frames[index],
                normalLayers?[index],
                depthLayers?[index],
                albedoLayers?[index],
                lights[index]);

            sprites.Add(sprite);
        }

        return new Animation(metadata.Value, sprites, BuildHitMask(animationKey, metadata.Value, sprites));
    }

    private static ImmutableBitmap? BuildHitMask(AssetAnimationKey key, AssetAnimationMetadata metadata, IReadOnlyList<Sprite> frames)
    {
        if (key is AssetAnimationKey.StackUnit or AssetAnimationKey.StackBoat)
        {
            return AnimationHitMaskBuilder.Build(metadata, frames);
        }
        else
        {
            return null;
        }
    }

    private Sound? LoadSound(AssetKey key)
    {
        AssetStream? data = assetSource.TryGetStream(key);
        if (data is null)
        {
            return null;
        }

        using Stream stream = data.Open();

        return SoundDecoder.DecodeToMemory(stream);
    }

    private static long MeasureSprite(Sprite sprite)
    {
        long total = (long)sprite.Metadata.PixelWidth * sprite.Metadata.PixelHeight * 4;

        if (sprite.Normal is not null)
        {
            total += (long)sprite.Normal.Width * sprite.Normal.Height * 4;
        }

        if (sprite.Depth is not null)
        {
            total += (long)sprite.Depth.Width * sprite.Depth.Height * 4;
        }

        if (sprite.Albedo is not null)
        {
            total += (long)sprite.Albedo.Width * sprite.Albedo.Height * 4;
        }

        return total;
    }

    private static long MeasureAnimation(Animation animation)
    {
        long total = 0;

        for (int index = 0; index < animation.Metadata.FrameCount; index++)
        {
            total += MeasureSprite(animation[index]);
        }

        return total;
    }

    private static long MeasureSound(Sound sound)
    {
        return (long)sound.Samples.Length * sizeof(float);
    }

    private static void DisposeSprite(Sprite sprite)
    {
        sprite.Image.Dispose();
        sprite.Normal?.Dispose();
        sprite.Depth?.Dispose();
        sprite.Albedo?.Dispose();
    }

    private static void DisposeAnimation(Animation animation)
    {
        for (int index = 0; index < animation.Metadata.FrameCount; index++)
        {
            DisposeSprite(animation[index]);
        }
    }

    private static void DisposeSound(Sound sound)
    {
    }

    private static Sprite CreateSprite(
        AssetSprite<SKImage> asset,
        AssetSpriteLayer<SKImage>? normalLayer,
        AssetSpriteLayer<SKImage>? depthLayer,
        AssetSpriteLayer<SKImage>? albedoLayer,
        IReadOnlyList<SpriteLight> lights)
    {
        return new Sprite(
            asset.Metadata,
            asset.Pixels,
            normalLayer?.Pixels,
            depthLayer?.Pixels,
            albedoLayer?.Pixels,
            lights);
    }
}
