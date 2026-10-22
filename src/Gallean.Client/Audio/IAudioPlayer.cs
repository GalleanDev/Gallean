namespace Gallean.Client;

internal interface IAudioPlayer
{
    float SoundVolume { get; set; }
    float MusicVolume { get; set; }

    void PlaySound(AssetSoundKey key);
    IMusicPlayback? PlayMusic(MusicQueue queue, MusicPosition position);
    IAudioPlayback? PlayVoice(AssetSoundKey key);
    IAmbientAudioPlayback? PlayAmbientSound(AssetSoundKey key, float volume, float pan);
}
