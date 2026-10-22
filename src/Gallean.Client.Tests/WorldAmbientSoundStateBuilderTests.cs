using static Gallean.Client.WorldRenderState;

namespace Gallean.Client.Tests;

public sealed class WorldAmbientSoundStateBuilderTests
{
    private static AmbientSound Sound(AssetSoundKey.AmbientSoundType type, int x, int y)
    {
        return new AmbientSound(new AssetSoundKey.Ambient(type), new WorldFootprint(new WorldPosition(x, y), 1, 1));
    }

    private static WorldAmbientSoundState BuildState(IReadOnlyList<AmbientSound> sounds, WorldPosition listener)
    {
        WorldAmbientSoundStateBuilder builder = new(sounds, WorldTransform.ToWorldPoint(listener));

        return builder.BuildState();
    }

    [Test]
    public void AudibleSoundsTest()
    {
        IReadOnlyList<AmbientSound> sounds =
        [
            Sound(AssetSoundKey.AmbientSoundType.City, 10, 10),
            Sound(AssetSoundKey.AmbientSoundType.Waterfall, 14, 10),
            Sound(AssetSoundKey.AmbientSoundType.Lava, 10, 14),
            Sound(AssetSoundKey.AmbientSoundType.GoldMine, 40, 10)
        ];

        WorldAmbientSoundState state = BuildState(sounds, new WorldPosition(10, 10));

        Assert.That(state, Is.EqualTo(new WorldAmbientSoundState([
            new WorldAmbientSoundState.Sound(Sound(AssetSoundKey.AmbientSoundType.City, 10, 10), 1.0f, 0.5f),
            new WorldAmbientSoundState.Sound(Sound(AssetSoundKey.AmbientSoundType.Lava, 10, 14), 0.7763932f, 0.33333334f),
            new WorldAmbientSoundState.Sound(Sound(AssetSoundKey.AmbientSoundType.Waterfall, 14, 10), 0.7763932f, 0.6666667f)
        ])));
    }

    [Test]
    public void VoiceLimitTest()
    {
        IReadOnlyList<AmbientSound> sounds =
        [
            Sound(AssetSoundKey.AmbientSoundType.Torch, 11, 10),
            Sound(AssetSoundKey.AmbientSoundType.Torch, 12, 10),
            Sound(AssetSoundKey.AmbientSoundType.Torch, 13, 10),
            Sound(AssetSoundKey.AmbientSoundType.Torch, 14, 10),
            Sound(AssetSoundKey.AmbientSoundType.Torch, 15, 10),
            Sound(AssetSoundKey.AmbientSoundType.Torch, 16, 10),
            Sound(AssetSoundKey.AmbientSoundType.Torch, 17, 10),
            Sound(AssetSoundKey.AmbientSoundType.Torch, 18, 10),
            Sound(AssetSoundKey.AmbientSoundType.Torch, 19, 10),
            Sound(AssetSoundKey.AmbientSoundType.Torch, 20, 10)
        ];

        WorldAmbientSoundState state = BuildState(sounds, new WorldPosition(10, 10));

        Assert.That(state, Is.EqualTo(new WorldAmbientSoundState([
            new WorldAmbientSoundState.Sound(Sound(AssetSoundKey.AmbientSoundType.Torch, 11, 10), 0.9440983f, 0.5416667f),
            new WorldAmbientSoundState.Sound(Sound(AssetSoundKey.AmbientSoundType.Torch, 12, 10), 0.8881966f, 0.5833333f),
            new WorldAmbientSoundState.Sound(Sound(AssetSoundKey.AmbientSoundType.Torch, 13, 10), 0.8322949f, 0.625f),
            new WorldAmbientSoundState.Sound(Sound(AssetSoundKey.AmbientSoundType.Torch, 14, 10), 0.7763932f, 0.6666667f),
            new WorldAmbientSoundState.Sound(Sound(AssetSoundKey.AmbientSoundType.Torch, 15, 10), 0.7204915f, 0.7083333f),
            new WorldAmbientSoundState.Sound(Sound(AssetSoundKey.AmbientSoundType.Torch, 16, 10), 0.6645898f, 0.75f),
            new WorldAmbientSoundState.Sound(Sound(AssetSoundKey.AmbientSoundType.Torch, 17, 10), 0.6086881f, 0.7916667f),
            new WorldAmbientSoundState.Sound(Sound(AssetSoundKey.AmbientSoundType.Torch, 18, 10), 0.5527864f, 0.8333333f)
        ])));
    }

    [Test]
    public void PanLimitTest()
    {
        IReadOnlyList<AmbientSound> sounds =
        [
            Sound(AssetSoundKey.AmbientSoundType.City, 26, 10),
            Sound(AssetSoundKey.AmbientSoundType.Waterfall, 10, 26)
        ];

        WorldAmbientSoundState state = BuildState(sounds, new WorldPosition(10, 10));

        Assert.That(state, Is.EqualTo(new WorldAmbientSoundState([
            new WorldAmbientSoundState.Sound(Sound(AssetSoundKey.AmbientSoundType.Waterfall, 10, 26), 0.10557281f, 0.0f),
            new WorldAmbientSoundState.Sound(Sound(AssetSoundKey.AmbientSoundType.City, 26, 10), 0.10557281f, 1.0f)
        ])));
    }
}
