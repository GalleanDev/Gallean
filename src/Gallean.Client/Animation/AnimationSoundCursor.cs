namespace Gallean.Client;

internal sealed class AnimationSoundCursor(IReadOnlyList<AnimationSound> sounds)
{
    private int next;

    public void Play(TimeSpan time, IAudioPlayer player)
    {
        while (next < sounds.Count && sounds[next].Start <= time)
        {
            player.PlaySound(sounds[next].Key);
            next++;
        }
    }
}
