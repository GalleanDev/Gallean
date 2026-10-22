using System.Globalization;
using static Gallean.Client.WorldRenderState;

namespace Gallean.Client.Tests;

public sealed class WorldAmbientSoundControllerTests
{
    private sealed class RecordingAudioPlayer : IAudioPlayer
    {
        public List<string> Actions { get; } = [];
        public bool HasClips { get; set; } = true;
        public float SoundVolume { get; set; }
        public float MusicVolume { get; set; }

        public void PlaySound(AssetSoundKey key)
        {
        }

        public IMusicPlayback? PlayMusic(MusicQueue queue, MusicPosition position)
        {
            return null;
        }

        public IAudioPlayback? PlayVoice(AssetSoundKey key)
        {
            return null;
        }

        public IAmbientAudioPlayback? PlayAmbientSound(AssetSoundKey key, float volume, float pan)
        {
            Actions.Add(Format("play", key, volume, pan));

            if (HasClips)
            {
                return new RecordingPlayback(key, this);
            }
            else
            {
                return null;
            }
        }

        public static string Format(string action, AssetSoundKey key, float volume, float pan)
        {
            return string.Create(CultureInfo.InvariantCulture, $"{action} {key.CanonicalId} volume={volume:F3} pan={pan:F3}");
        }
    }

    private sealed class RecordingPlayback(AssetSoundKey key, RecordingAudioPlayer player) : IAmbientAudioPlayback
    {
        public void SetLevel(float volume, float pan)
        {
            player.Actions.Add(RecordingAudioPlayer.Format("level", key, volume, pan));
        }

        public void Stop()
        {
            player.Actions.Add($"stop {key.CanonicalId}");
        }
    }

    private static AmbientSound Sound(AssetSoundKey.AmbientSoundType type, int x, int y)
    {
        return new AmbientSound(new AssetSoundKey.Ambient(type), new WorldFootprint(new WorldPosition(x, y), 1, 1));
    }

    [Test]
    public void UnchangedInputTest()
    {
        WorldAmbientSoundController controller = new();
        RecordingAudioPlayer player = new();
        IReadOnlyList<AmbientSound> sounds = [Sound(AssetSoundKey.AmbientSoundType.City, 10, 10)];

        controller.Update(sounds, WorldTransform.ToWorldPoint(new WorldPosition(10, 10)), player);
        controller.Update(sounds, WorldTransform.ToWorldPoint(new WorldPosition(10, 10)), player);

        Assert.That(player.Actions, Is.EqualTo([
            "play ambient/city volume=1.000 pan=0.500"
        ]));
    }

    [Test]
    public void ListenerMovedTest()
    {
        WorldAmbientSoundController controller = new();
        RecordingAudioPlayer player = new();
        IReadOnlyList<AmbientSound> sounds =
        [
            Sound(AssetSoundKey.AmbientSoundType.City, 10, 10),
            Sound(AssetSoundKey.AmbientSoundType.Waterfall, 24, 10)
        ];

        controller.Update(sounds, WorldTransform.ToWorldPoint(new WorldPosition(10, 10)), player);
        controller.Update(sounds, WorldTransform.ToWorldPoint(new WorldPosition(14, 10)), player);
        controller.Update(sounds, WorldTransform.ToWorldPoint(new WorldPosition(40, 10)), player);

        Assert.That(player.Actions, Is.EqualTo([
            "play ambient/city volume=1.000 pan=0.500",
            "play ambient/waterfall volume=0.217 pan=1.000",
            "level ambient/city volume=0.776 pan=0.333",
            "level ambient/waterfall volume=0.441 pan=0.917",
            "level ambient/waterfall volume=0.106 pan=0.000",
            "stop ambient/city"
        ]));
    }

    [Test]
    public void StopTest()
    {
        WorldAmbientSoundController controller = new();
        RecordingAudioPlayer player = new();
        IReadOnlyList<AmbientSound> sounds = [Sound(AssetSoundKey.AmbientSoundType.City, 10, 10)];

        controller.Update(sounds, WorldTransform.ToWorldPoint(new WorldPosition(10, 10)), player);
        controller.Stop();
        controller.Update(sounds, WorldTransform.ToWorldPoint(new WorldPosition(10, 10)), player);

        Assert.That(player.Actions, Is.EqualTo([
            "play ambient/city volume=1.000 pan=0.500",
            "stop ambient/city",
            "play ambient/city volume=1.000 pan=0.500"
        ]));
    }

    [Test]
    public void MissingClipTest()
    {
        WorldAmbientSoundController controller = new();
        RecordingAudioPlayer player = new()
        {
            HasClips = false
        };
        IReadOnlyList<AmbientSound> sounds = [Sound(AssetSoundKey.AmbientSoundType.City, 10, 10)];

        controller.Update(sounds, WorldTransform.ToWorldPoint(new WorldPosition(10, 10)), player);
        controller.Update(sounds, WorldTransform.ToWorldPoint(new WorldPosition(11, 10)), player);

        Assert.That(player.Actions, Is.EqualTo([
            "play ambient/city volume=1.000 pan=0.500"
        ]));
    }
}
