namespace Gallean.Client;

internal interface IAmbientAudioPlayback : IAudioPlayback
{
    void SetLevel(float volume, float pan);
}
