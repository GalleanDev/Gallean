using Gallean.Common;

namespace Gallean.Client.Tests;

internal sealed class TestAssetProvider : IAssetProvider, IDisposable
{
    private readonly Func<AssetKey, Sprite?> resolveSprite;
    private readonly Func<AssetKey, Animation?> resolveAnimation;
    private readonly Func<AssetKey, AssetStream?> resolveSound;
    private readonly Action<AssetKey>? onRequest;
    private readonly CleanupList cleanup;
    private readonly AssetCache<Sprite> spriteCache;
    private readonly AssetCache<Animation> animationCache;
    private readonly AssetCache<Sound> soundCache;
    private readonly Lock requestLocker;

    internal TestAssetProvider(
        Func<AssetKey, Sprite?> resolveSprite,
        Func<AssetKey, Animation?> resolveAnimation,
        Func<AssetKey, AssetStream?> resolveSound,
        Action<AssetKey>? onRequest,
        CleanupList cleanup)
    {
        this.resolveSprite = resolveSprite;
        this.resolveAnimation = resolveAnimation;
        this.resolveSound = resolveSound;
        this.onRequest = onRequest;
        this.cleanup = cleanup;

        spriteCache = new AssetCache<Sprite>(long.MaxValue, LoadSprite, MeasureSprite, _ => { });
        animationCache = new AssetCache<Animation>(long.MaxValue, LoadAnimation, MeasureAnimation, _ => { });
        soundCache = new AssetCache<Sound>(long.MaxValue, LoadSound, MeasureSound, _ => { });

        requestLocker = new Lock();
    }

    public List<AssetKey> RequiredAssets { get; } = [];
    public List<AssetKey> OptionalAssets { get; } = [];

    public AssetCache<Sprite>.Lease GetRequiredSprite(AssetSpriteKey key)
    {
        Request(key, RequiredAssets);

        AssetCache<Sprite>.Lease? sprite = spriteCache.Acquire(key);

        if (sprite is null)
        {
            throw new KeyNotFoundException($"No asset data found for key '{key}'.");
        }

        return sprite;
    }

    public AssetCache<Sprite>.Lease? GetOptionalSprite(AssetSpriteKey key)
    {
        Request(key, OptionalAssets);

        return spriteCache.Acquire(key);
    }

    public AssetCache<Animation>.Lease GetRequiredAnimation(AssetAnimationKey key)
    {
        Request(key, RequiredAssets);

        AssetCache<Animation>.Lease? animation = animationCache.Acquire(key);

        if (animation is null)
        {
            throw new KeyNotFoundException($"No asset data found for key '{key}'.");
        }

        return animation;
    }

    public AssetCache<Animation>.Lease? GetOptionalAnimation(AssetAnimationKey key)
    {
        Request(key, OptionalAssets);

        return animationCache.Acquire(key);
    }

    public AssetCache<Sound>.Lease GetRequiredSound(AssetSoundKey key)
    {
        Request(key, RequiredAssets);

        AssetCache<Sound>.Lease? sound = soundCache.Acquire(key);

        if (sound is null)
        {
            throw new KeyNotFoundException($"No asset data found for key '{key}'.");
        }

        return sound;
    }

    public AssetSpriteMetadata? GetOptionalSpriteMetadata(AssetSpriteKey key)
    {
        Sprite? sprite = resolveSprite(key);

        if (sprite is null)
        {
            return null;
        }

        return sprite.Metadata;
    }

    public AssetAnimationMetadata? GetOptionalAnimationMetadata(AssetAnimationKey key)
    {
        Animation? animation = resolveAnimation(key);

        if (animation is null)
        {
            return null;
        }

        return animation.Metadata;
    }

    public AssetSoundMetadata? GetOptionalSoundMetadata(AssetSoundKey key)
    {
        AssetStream? sound = resolveSound(key);

        if (sound is null)
        {
            return null;
        }

        return new AssetSoundMetadata();
    }

    public AssetCache<Sound>.Lease? GetOptionalSound(AssetSoundKey key)
    {
        Request(key, OptionalAssets);

        return soundCache.Acquire(key);
    }

    public AssetStream? GetOptionalStream(AssetKey key)
    {
        Request(key, OptionalAssets);

        return resolveSound(key);
    }

    public void Dispose()
    {
        spriteCache.Dispose();
        animationCache.Dispose();
        soundCache.Dispose();
        cleanup.Dispose();
    }

    private Sprite? LoadSprite(AssetKey key)
    {
        return resolveSprite(key);
    }

    private Animation? LoadAnimation(AssetKey key)
    {
        return resolveAnimation(key);
    }

    private Sound? LoadSound(AssetKey key)
    {
        AssetStream? data = resolveSound(key);

        if (data is null)
        {
            return null;
        }

        using Stream stream = data.Open();

        return SoundDecoder.DecodeToMemory(stream);
    }

    private static long MeasureSprite(Sprite sprite)
    {
        return (long)sprite.Metadata.PixelWidth * sprite.Metadata.PixelHeight * 4;
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

    private void Request(AssetKey key, List<AssetKey> assets)
    {
        lock (requestLocker)
        {
            assets.Add(key);
        }

        onRequest?.Invoke(key);
    }
}
