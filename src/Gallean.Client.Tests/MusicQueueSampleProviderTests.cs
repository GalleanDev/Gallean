namespace Gallean.Client.Tests;

public sealed class MusicQueueSampleProviderTests
{
    [Test]
    public void LoopingTest()
    {
        AssetSoundKey first = new AssetSoundKey.MenuMusic();
        AssetSoundKey second = new AssetSoundKey.BattleMusic(0);
        TestAssetProviderBuilder builder = new();
        AssetStream firstSound = builder.CreateSound(TestSounds.Create(SoundDecoder.MixerSampleRate, SoundDecoder.MixerChannels, 64));
        AssetStream secondSound = builder.CreateSound(TestSounds.Create(SoundDecoder.MixerSampleRate, SoundDecoder.MixerChannels, 64));

        using TestAssetProvider assets = builder
            .ResolveSprite(_ => throw new InvalidOperationException("No sprite expected."))
            .ResolveAnimation(_ => throw new InvalidOperationException("No animation expected."))
            .ResolveSound(key => key == first ? firstSound : secondSound)
            .Build();
        using MusicQueueSampleProvider provider = new(assets, MusicQueue.Create([first, second], isLooping: true), MusicPosition.Start);
        float[] samples = new float[384];

        int read = provider.Read(samples);

        Assert.That(read, Is.EqualTo(384));
        Assert.That(assets.OptionalAssets, Is.EqualTo([first, second, first]));
    }

    [Test]
    public void NonLoopingTest()
    {
        AssetSoundKey first = new AssetSoundKey.MenuMusic();
        AssetSoundKey second = new AssetSoundKey.BattleMusic(0);
        TestAssetProviderBuilder builder = new();
        AssetStream firstSound = builder.CreateSound(TestSounds.Create(SoundDecoder.MixerSampleRate, SoundDecoder.MixerChannels, 64));
        AssetStream secondSound = builder.CreateSound(TestSounds.Create(SoundDecoder.MixerSampleRate, SoundDecoder.MixerChannels, 64));

        using TestAssetProvider assets = builder
            .ResolveSprite(_ => throw new InvalidOperationException("No sprite expected."))
            .ResolveAnimation(_ => throw new InvalidOperationException("No animation expected."))
            .ResolveSound(key => key == first ? firstSound : secondSound)
            .Build();
        using MusicQueueSampleProvider provider = new(assets, MusicQueue.Create([first, second], isLooping: false), MusicPosition.Start);
        float[] samples = new float[384];

        int read = provider.Read(samples);
        int readAfterEnd = provider.Read(samples.AsSpan(read));

        Assert.That(read, Is.EqualTo(256));
        Assert.That(readAfterEnd, Is.Zero);
        Assert.That(assets.OptionalAssets, Is.EqualTo([first, second]));
    }

    [Test]
    public void MissingTrackTest()
    {
        AssetSoundKey missing = new AssetSoundKey.MenuMusic();
        AssetSoundKey available = new AssetSoundKey.BattleMusic(0);
        TestAssetProviderBuilder builder = new();
        AssetStream sound = builder.CreateSound(TestSounds.Create(SoundDecoder.MixerSampleRate, SoundDecoder.MixerChannels, 64));

        using TestAssetProvider assets = builder
            .ResolveSprite(_ => throw new InvalidOperationException("No sprite expected."))
            .ResolveAnimation(_ => throw new InvalidOperationException("No animation expected."))
            .ResolveSound(key => key == available ? sound : null)
            .Build();
        using MusicQueueSampleProvider provider = new(assets, MusicQueue.Create([missing, available], isLooping: true), MusicPosition.Start);
        float[] samples = new float[256];

        int read = provider.Read(samples);

        Assert.That(read, Is.EqualTo(256));
        Assert.That(assets.OptionalAssets, Is.EqualTo([missing, available, missing, available]));
    }

    [Test]
    public void DecodeFailureTest()
    {
        AssetSoundKey key = new AssetSoundKey.MenuMusic();
        TestAssetProviderBuilder builder = new();
        AssetStream sound = builder.CreateSound(1, 2, 3);

        using TestAssetProvider assets = builder
            .ResolveSprite(_ => throw new InvalidOperationException("No sprite expected."))
            .ResolveAnimation(_ => throw new InvalidOperationException("No animation expected."))
            .ResolveSound(_ => sound)
            .Build();
        using MusicQueueSampleProvider provider = new(assets, MusicQueue.Create([key], isLooping: true), MusicPosition.Start);
        float[] samples = new float[2];

        Assert.That(
            () => provider.Read(samples),
            Throws.TypeOf<SoundDecodeException>()
                .With.InnerException.TypeOf<ArgumentException>());
    }

    [Test]
    public void ZeroSamplesTest()
    {
        using MemoryStream stream = new(TestSounds.Create(SoundDecoder.MixerSampleRate, SoundDecoder.MixerChannels, 0));

        Assert.That(
            () => SoundDecoder.CreateSamples(stream, TimeSpan.Zero),
            Throws.TypeOf<InvalidDataException>()
                .With.Message.EqualTo("Sound contains no samples."));
    }

    [Test]
    public void PositionTest()
    {
        AssetSoundKey first = new AssetSoundKey.MenuMusic();
        AssetSoundKey second = new AssetSoundKey.BattleMusic(0);
        TestAssetProviderBuilder builder = new();
        AssetStream firstSound = builder.CreateSound(TestSounds.Create(SoundDecoder.MixerSampleRate, SoundDecoder.MixerChannels, 4410));
        AssetStream secondSound = builder.CreateSound(TestSounds.Create(SoundDecoder.MixerSampleRate, SoundDecoder.MixerChannels, 4410));

        using TestAssetProvider assets = builder
            .ResolveSprite(_ => throw new InvalidOperationException("No sprite expected."))
            .ResolveAnimation(_ => throw new InvalidOperationException("No animation expected."))
            .ResolveSound(key => key == first ? firstSound : secondSound)
            .Build();
        using MusicQueueSampleProvider provider = new(assets, MusicQueue.Create([first, second], isLooping: false), MusicPosition.Start);
        float[] samples = new float[1000];

        MusicPosition initialPosition = provider.Position;
        provider.Read(samples);
        MusicPosition playingPosition = provider.Position;

        Assert.That(initialPosition, Is.EqualTo(new MusicPosition(0, null)));
        Assert.That(playingPosition, Is.EqualTo(new MusicPosition(0, TimeSpan.FromTicks(113378))));
    }

    [Test]
    public void FinishedTrackPositionTest()
    {
        AssetSoundKey first = new AssetSoundKey.MenuMusic();
        AssetSoundKey second = new AssetSoundKey.BattleMusic(0);
        TestAssetProviderBuilder builder = new();
        AssetStream firstSound = builder.CreateSound(TestSounds.Create(SoundDecoder.MixerSampleRate, SoundDecoder.MixerChannels, 64));
        AssetStream secondSound = builder.CreateSound(TestSounds.Create(SoundDecoder.MixerSampleRate, SoundDecoder.MixerChannels, 64));

        using TestAssetProvider assets = builder
            .ResolveSprite(_ => throw new InvalidOperationException("No sprite expected."))
            .ResolveAnimation(_ => throw new InvalidOperationException("No animation expected."))
            .ResolveSound(key => key == first ? firstSound : secondSound)
            .Build();
        using MusicQueueSampleProvider provider = new(assets, MusicQueue.Create([first, second], isLooping: false), MusicPosition.Start);
        float[] samples = new float[128];

        provider.Read(samples);

        Assert.That(provider.Position, Is.EqualTo(new MusicPosition(1, null)));
    }

    [Test]
    public void ResumeTest()
    {
        AssetSoundKey first = new AssetSoundKey.MenuMusic();
        AssetSoundKey second = new AssetSoundKey.BattleMusic(0);
        TestAssetProviderBuilder builder = new();
        AssetStream firstSound = builder.CreateSound(TestSounds.Create(SoundDecoder.MixerSampleRate, SoundDecoder.MixerChannels, 4410));
        AssetStream secondSound = builder.CreateSound(TestSounds.Create(SoundDecoder.MixerSampleRate, SoundDecoder.MixerChannels, 4410));

        using TestAssetProvider assets = builder
            .ResolveSprite(_ => throw new InvalidOperationException("No sprite expected."))
            .ResolveAnimation(_ => throw new InvalidOperationException("No animation expected."))
            .ResolveSound(key => key == first ? firstSound : secondSound)
            .Build();
        MusicPosition position = new(1, TimeSpan.FromMilliseconds(50.0));
        using MusicQueueSampleProvider provider = new(assets, MusicQueue.Create([first, second], isLooping: false), position);
        float[] samples = new float[16384];

        int read = provider.Read(samples);

        Assert.That(read, Is.EqualTo(4410));
        Assert.That(assets.OptionalAssets, Is.EqualTo([second]));
    }
}
