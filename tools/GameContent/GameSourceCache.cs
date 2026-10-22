namespace GameContent;

internal sealed class GameSourceCache
{
    private const int MaxCachedAnimations = 256;
    private const int MaxCachedArchiveAnchoredSprites = 1024;
    private const int MaxCachedArchiveObjectSeries = 256;
    private const int MaxCachedArchiveSprites = 1024;
    private const int MaxCachedSoundClips = 64;
    private const int MaxCachedSounds = 64;
    private const int MaxCachedSprites = 1024;

    private readonly BoundedCache<AssetAnimationKey, AssetAnimation?> animations = new(MaxCachedAnimations);
    private readonly BoundedCache<(string Archive, string Name), AssetSprite?> archiveAnchoredSprites = new(MaxCachedArchiveAnchoredSprites);
    private readonly BoundedCache<(string Archive, string Name), IReadOnlyList<AssetSprite>> archiveObjectSeries = new(MaxCachedArchiveObjectSeries);
    private readonly BoundedCache<(string Archive, string Name), AssetSprite?> archiveSprites = new(MaxCachedArchiveSprites);
    private readonly BoundedCache<string, AssetSound?> soundClips = new(MaxCachedSoundClips);
    private readonly BoundedCache<AssetSoundKey, AssetSound?> sounds = new(MaxCachedSounds);
    private readonly BoundedCache<AssetSpriteKey, AssetSprite?> sprites = new(MaxCachedSprites);

    public AssetSprite? GetOrAddSprite(AssetSpriteKey key, Func<AssetSpriteKey, AssetSprite?> build)
    {
        return sprites.GetOrAdd(key, build);
    }

    public AssetAnimation? GetOrAddAnimation(AssetAnimationKey key, Func<AssetAnimationKey, AssetAnimation?> build)
    {
        return animations.GetOrAdd(key, build);
    }

    public AssetSound? GetOrAddSound(AssetSoundKey key, Func<AssetSoundKey, AssetSound?> build)
    {
        return sounds.GetOrAdd(key, build);
    }

    public AssetSprite? GetOrAddArchiveSprite(string archive, string name, Func<string, AssetSprite?> load)
    {
        return archiveSprites.GetOrAdd((archive, Normalize(name)), key => load(key.Name));
    }

    public AssetSprite? GetOrAddArchiveAnchoredSprite(string archive, string name, Func<string, AssetSprite?> load)
    {
        return archiveAnchoredSprites.GetOrAdd((archive, Normalize(name)), key => load(key.Name));
    }

    public IReadOnlyList<AssetSprite> GetOrAddArchiveObjectSeries(string archive, string name, Func<string, IReadOnlyList<AssetSprite>> load)
    {
        return archiveObjectSeries.GetOrAdd((archive, Normalize(name)), key => load(key.Name));
    }

    public AssetSound? GetOrAddSoundClip(string name, Func<string, AssetSound?> load)
    {
        return soundClips.GetOrAdd(Normalize(name), load);
    }

    private static string Normalize(string name)
    {
        return name.ToUpperInvariant();
    }
}
