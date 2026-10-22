using Avalonia;
using static Gallean.Client.WorldRenderState;

namespace Gallean.Client;

internal sealed class WorldAmbientSoundController
{
    private Dictionary<AmbientSound, IAmbientAudioPlayback?> playbacks = [];
    private Dictionary<AmbientSound, IAmbientAudioPlayback?> previousPlaybacks = [];
    private IReadOnlyList<AmbientSound>? currentSounds;
    private Point currentPosition;

    public void Update(IReadOnlyList<AmbientSound> sounds, Point position, IAudioPlayer player)
    {
        if (!ReferenceEquals(sounds, currentSounds) || position != currentPosition)
        {
            currentSounds = sounds;
            currentPosition = position;

            WorldAmbientSoundStateBuilder builder = new(sounds, position);
            WorldAmbientSoundState state = builder.BuildState();

            Apply(state, player);
        }
    }

    public void Stop()
    {
        StopPlaybacks(playbacks);

        currentSounds = null;
    }

    private void Apply(WorldAmbientSoundState state, IAudioPlayer player)
    {
        (playbacks, previousPlaybacks) = (previousPlaybacks, playbacks);
        playbacks.Clear();

        foreach (WorldAmbientSoundState.Sound sound in state.Sounds)
        {
            if (previousPlaybacks.Remove(sound.Source, out IAmbientAudioPlayback? playback))
            {
                playback?.SetLevel(sound.Volume, sound.Pan);
            }
            else
            {
                playback = player.PlayAmbientSound(sound.Source.AssetKey, sound.Volume, sound.Pan);
            }

            playbacks[sound.Source] = playback;
        }

        StopPlaybacks(previousPlaybacks);
    }

    private static void StopPlaybacks(Dictionary<AmbientSound, IAmbientAudioPlayback?> playing)
    {
        foreach (IAmbientAudioPlayback? playback in playing.Values)
        {
            playback?.Stop();
        }

        playing.Clear();
    }
}
