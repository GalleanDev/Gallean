using Gallean.Common;

namespace Gallean.Client.Tests;

public sealed class MusicQueueTests
{
    private static IReadOnlyList<AssetSoundKey> WorldTracks(int count)
    {
        List<AssetSoundKey> tracks = [];

        for (int variant = 0; variant < count; variant++)
        {
            tracks.Add(new AssetSoundKey.WorldMusic(variant));
        }

        return tracks;
    }

    private static IReadOnlyList<AssetSoundKey> AmbientTracks(int count)
    {
        List<AssetSoundKey> tracks = [];

        for (int variant = 0; variant < count; variant++)
        {
            tracks.Add(new AssetSoundKey.AmbientMusic(variant));
        }

        return tracks;
    }

    [Test]
    public void OrderedPlaylistTest()
    {
        MusicQueue queue = MusicQueue.Create(WorldTracks(4), isLooping: true);

        IReadOnlyList<AssetSoundKey> expected =
        [
            new AssetSoundKey.WorldMusic(0),
            new AssetSoundKey.WorldMusic(1),
            new AssetSoundKey.WorldMusic(2),
            new AssetSoundKey.WorldMusic(3)
        ];

        Assert.That(queue.Tracks, Is.EqualTo(expected));
    }

    [Test]
    public void SinglePlaylistTest()
    {
        MusicQueue queue = new MusicQueue.Builder(new RandomProvider(0))
            .AddShuffled(WorldTracks(4))
            .Build();

        IReadOnlyList<AssetSoundKey> expected =
        [
            new AssetSoundKey.WorldMusic(2),
            new AssetSoundKey.WorldMusic(3),
            new AssetSoundKey.WorldMusic(1),
            new AssetSoundKey.WorldMusic(0)
        ];

        Assert.That(queue.Tracks, Is.EqualTo(expected));
    }

    [Test]
    public void RoundRobinTest()
    {
        MusicQueue queue = new MusicQueue.Builder(new RandomProvider(0))
            .AddShuffled(WorldTracks(4))
            .AddShuffled(AmbientTracks(4))
            .Build();

        IReadOnlyList<AssetSoundKey> expected =
        [
            new AssetSoundKey.AmbientMusic(2),
            new AssetSoundKey.WorldMusic(2),
            new AssetSoundKey.AmbientMusic(1),
            new AssetSoundKey.WorldMusic(3),
            new AssetSoundKey.AmbientMusic(3),
            new AssetSoundKey.WorldMusic(1),
            new AssetSoundKey.AmbientMusic(0),
            new AssetSoundKey.WorldMusic(0)
        ];

        Assert.That(queue.Tracks, Is.EqualTo(expected));
    }

    [Test]
    public void UnevenPlaylistsTest()
    {
        MusicQueue queue = new MusicQueue.Builder(new RandomProvider(0))
            .AddShuffled(WorldTracks(4))
            .AddShuffled(AmbientTracks(2))
            .Build();

        IReadOnlyList<AssetSoundKey> expected =
        [
            new AssetSoundKey.WorldMusic(2),
            new AssetSoundKey.AmbientMusic(1),
            new AssetSoundKey.WorldMusic(3),
            new AssetSoundKey.AmbientMusic(0),
            new AssetSoundKey.WorldMusic(1),
            new AssetSoundKey.WorldMusic(0)
        ];

        Assert.That(queue.Tracks, Is.EqualTo(expected));
    }
}
