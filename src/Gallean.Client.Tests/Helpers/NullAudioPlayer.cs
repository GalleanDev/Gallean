namespace Gallean.Client.Tests;

internal sealed class NullAudioPlayer : IAudioPlayer
{
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
        return null;
    }
}
