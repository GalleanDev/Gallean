namespace Gallean.Client.Tests;

public sealed class AnimationSoundCursorTests
{
    private sealed class TestSoundPlayer : IAudioPlayer
    {
        public List<AssetSoundKey> Played { get; } = [];
        public float SoundVolume { get; set; }
        public float MusicVolume { get; set; }

        public void PlaySound(AssetSoundKey key)
        {
            Played.Add(key);
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
            return null;
        }
    }

    [Test]
    public void PlayTest()
    {
        AssetSoundKey first = new AssetSoundKey.BattleUnitAttack(new UnitDefinitionId("ATK"), Variant: 3);
        AssetSoundKey second = new AssetSoundKey.BattleUnitHit(new UnitDefinitionId("ATK"), Variant: 2);

        List<AnimationSound> sounds =
        [
            new(TimeSpan.FromSeconds(0.1), first),
            new(TimeSpan.FromSeconds(0.3), second)
        ];

        TestSoundPlayer player = new();
        AnimationSoundCursor cursor = new(sounds);

        cursor.Play(TimeSpan.FromSeconds(0.05), player);
        Assert.That(player.Played, Is.Empty);

        cursor.Play(TimeSpan.FromSeconds(0.2), player);
        Assert.That(player.Played, Is.EqualTo([first]));

        cursor.Play(TimeSpan.FromSeconds(0.2), player);
        Assert.That(player.Played, Is.EqualTo([first]));

        cursor.Play(TimeSpan.FromSeconds(0.5), player);
        Assert.That(player.Played, Is.EqualTo([first, second]));
    }

    [Test]
    public void PlayRestartTest()
    {
        AssetSoundKey sound = new AssetSoundKey.BattleUnitAttack(new UnitDefinitionId("ATK"), Variant: 3);
        List<AnimationSound> sounds = [new(TimeSpan.Zero, sound)];

        TestSoundPlayer player = new();
        AnimationSoundCursor cursor = new(sounds);
        AnimationSoundCursor restarted = new(sounds);

        cursor.Play(TimeSpan.FromSeconds(1), player);
        restarted.Play(TimeSpan.FromSeconds(1), player);

        Assert.That(player.Played, Is.EqualTo([sound, sound]));
    }
}
