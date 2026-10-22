using Gallean.Engine.Contracts;

namespace GameContent.Tests;

public sealed class GameSourceCacheTests
{
    private static AssetSprite CreateSprite()
    {
        return new AssetSprite(new AssetSpriteMetadata(1, 1, 1, 1, 0, 0, Additive: false), new byte[4]);
    }

    private static AssetAnimation CreateAnimation()
    {
        return new AssetAnimation(new AssetAnimationMetadata(1, 1, 1, 0, 0, TriggerFrame: null, SecondTriggerFrame: null), [CreateSprite()]);
    }

    [Test]
    public void SpriteBuiltOnceTest()
    {
        GameSourceCache cache = new();
        AssetSprite sprite = CreateSprite();
        int builds = 0;

        AssetSprite? first = cache.GetOrAddSprite(new AssetSpriteKey.Tree(WorldMapTerrain.Neutral, 1), _ =>
        {
            builds++;

            return sprite;
        });

        AssetSprite? second = cache.GetOrAddSprite(new AssetSpriteKey.Tree(WorldMapTerrain.Neutral, 1), _ =>
        {
            builds++;

            return sprite;
        });

        Assert.That(first, Is.SameAs(sprite));
        Assert.That(second, Is.SameAs(sprite));
        Assert.That(builds, Is.EqualTo(1));
    }

    [Test]
    public void NullSpriteCachedTest()
    {
        GameSourceCache cache = new();
        int builds = 0;

        AssetSprite? first = cache.GetOrAddSprite(new AssetSpriteKey.Tree(WorldMapTerrain.Neutral, 1), _ =>
        {
            builds++;

            return null;
        });

        AssetSprite? second = cache.GetOrAddSprite(new AssetSpriteKey.Tree(WorldMapTerrain.Neutral, 1), _ =>
        {
            builds++;

            return null;
        });

        Assert.That(first, Is.Null);
        Assert.That(second, Is.Null);
        Assert.That(builds, Is.EqualTo(1));
    }

    [Test]
    public void DistinctKeysTest()
    {
        GameSourceCache cache = new();
        int builds = 0;

        cache.GetOrAddSprite(new AssetSpriteKey.Tree(WorldMapTerrain.Neutral, 1), _ =>
        {
            builds++;

            return CreateSprite();
        });

        cache.GetOrAddSprite(new AssetSpriteKey.Tree(WorldMapTerrain.Neutral, 2), _ =>
        {
            builds++;

            return CreateSprite();
        });

        Assert.That(builds, Is.EqualTo(2));
    }

    [Test]
    public void SoundBuiltOnceTest()
    {
        GameSourceCache cache = new();
        AssetSound sound = new(new AssetSoundMetadata(), AssetStream.FromBytes(new byte[4]));
        int builds = 0;

        AssetSound? first = cache.GetOrAddSound(new AssetSoundKey.MenuMusic(), _ =>
        {
            builds++;

            return sound;
        });

        AssetSound? second = cache.GetOrAddSound(new AssetSoundKey.MenuMusic(), _ =>
        {
            builds++;

            return sound;
        });

        Assert.That(first, Is.SameAs(sound));
        Assert.That(second, Is.SameAs(sound));
        Assert.That(builds, Is.EqualTo(1));
    }

    [Test]
    public void SoundClipLoadedOnceTest()
    {
        GameSourceCache cache = new();
        AssetSound clip = new(new AssetSoundMetadata(), AssetStream.FromBytes(new byte[4]));
        int loads = 0;

        AssetSound? first = cache.GetOrAddSoundClip("clip.wav", _ =>
        {
            loads++;

            return clip;
        });

        AssetSound? second = cache.GetOrAddSoundClip("clip.wav", _ =>
        {
            loads++;

            return clip;
        });

        Assert.That(first, Is.SameAs(clip));
        Assert.That(second, Is.SameAs(clip));
        Assert.That(loads, Is.EqualTo(1));
    }

    [Test]
    public void ArchiveSpriteLoadedOnceTest()
    {
        GameSourceCache cache = new();
        AssetSprite sprite = CreateSprite();
        int loads = 0;

        AssetSprite? first = cache.GetOrAddArchiveSprite("IsoStill.ff", "G000RU0000010", _ =>
        {
            loads++;

            return sprite;
        });

        AssetSprite? second = cache.GetOrAddArchiveSprite("IsoStill.ff", "G000RU0000010", _ =>
        {
            loads++;

            return sprite;
        });

        AssetSprite? other = cache.GetOrAddArchiveSprite("IsoCmon.ff", "G000RU0000010", _ =>
        {
            loads++;

            return CreateSprite();
        });

        Assert.That(first, Is.SameAs(sprite));
        Assert.That(second, Is.SameAs(sprite));
        Assert.That(other, Is.Not.SameAs(sprite));
        Assert.That(loads, Is.EqualTo(2));
    }

    [Test]
    public void AnimationBuiltOnceTest()
    {
        GameSourceCache cache = new();
        AssetAnimation animation = CreateAnimation();
        int builds = 0;

        AssetAnimation? first = cache.GetOrAddAnimation(new AssetAnimationKey.City(1), _ =>
        {
            builds++;

            return animation;
        });

        AssetAnimation? second = cache.GetOrAddAnimation(new AssetAnimationKey.City(1), _ =>
        {
            builds++;

            return animation;
        });

        Assert.That(first, Is.SameAs(animation));
        Assert.That(second, Is.SameAs(animation));
        Assert.That(builds, Is.EqualTo(1));
    }

    [Test]
    public void NullAnimationCachedTest()
    {
        GameSourceCache cache = new();
        int builds = 0;

        AssetAnimation? first = cache.GetOrAddAnimation(new AssetAnimationKey.City(1), _ =>
        {
            builds++;

            return null;
        });

        AssetAnimation? second = cache.GetOrAddAnimation(new AssetAnimationKey.City(1), _ =>
        {
            builds++;

            return null;
        });

        Assert.That(first, Is.Null);
        Assert.That(second, Is.Null);
        Assert.That(builds, Is.EqualTo(1));
    }

    [Test]
    public void ArchiveSpriteCaseTest()
    {
        GameSourceCache cache = new();
        AssetSprite sprite = CreateSprite();
        List<string> loaded = [];

        AssetSprite? first = cache.GetOrAddArchiveSprite("IsoStill.ff", "G000RU0000010", name =>
        {
            loaded.Add(name);

            return sprite;
        });

        AssetSprite? second = cache.GetOrAddArchiveSprite("IsoStill.ff", "g000ru0000010", name =>
        {
            loaded.Add(name);

            return CreateSprite();
        });

        Assert.That(first, Is.SameAs(sprite));
        Assert.That(second, Is.SameAs(sprite));
        Assert.That(loaded, Is.EqualTo(["G000RU0000010"]));
    }

    [Test]
    public void SoundClipCaseTest()
    {
        GameSourceCache cache = new();
        AssetSound clip = new(new AssetSoundMetadata(), AssetStream.FromBytes(new byte[4]));
        List<string> loaded = [];

        AssetSound? first = cache.GetOrAddSoundClip("CREATSTK", name =>
        {
            loaded.Add(name);

            return clip;
        });

        AssetSound? second = cache.GetOrAddSoundClip("creatstk", name =>
        {
            loaded.Add(name);

            return new AssetSound(new AssetSoundMetadata(), AssetStream.FromBytes(new byte[8]));
        });

        Assert.That(first, Is.SameAs(clip));
        Assert.That(second, Is.SameAs(clip));
        Assert.That(loaded, Is.EqualTo(["CREATSTK"]));
    }
}
