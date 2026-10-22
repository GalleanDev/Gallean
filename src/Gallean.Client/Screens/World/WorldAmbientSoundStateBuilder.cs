using Avalonia;
using static Gallean.Client.WorldRenderState;

namespace Gallean.Client;

internal sealed class WorldAmbientSoundStateBuilder(IReadOnlyList<AmbientSound> sounds, Point listener)
{
    private const int MaxVoices = 8;

    private const double HearingRadius = 640;
    private const double PanWidth = 384;

    public WorldAmbientSoundState BuildState()
    {
        List<WorldAmbientSoundState.Sound> result = [];

        foreach (AmbientSound sound in sounds)
        {
            Point center = WorldRenderLayout.Footprint.GetAnchor(sound.Footprint);
            Vector offset = center - listener;
            double distance = offset.Length;

            if (distance < HearingRadius)
            {
                float volume = (float)(1 - distance / HearingRadius);

                result.Add(new WorldAmbientSoundState.Sound(sound, volume, GetPan(offset.X)));
            }
        }

        result.Sort(CompareSound);

        if (result.Count > MaxVoices)
        {
            result.RemoveRange(MaxVoices, result.Count - MaxVoices);
        }

        return new WorldAmbientSoundState(result);
    }

    private static float GetPan(double offsetX)
    {
        return (float)Math.Clamp(0.5 + offsetX / (2 * PanWidth), 0, 1);
    }

    private static int CompareSound(WorldAmbientSoundState.Sound left, WorldAmbientSoundState.Sound right)
    {
        int rv;

        rv = right.Volume.CompareTo(left.Volume);
        if (rv != 0)
        {
            return rv;
        }

        WorldPosition leftPosition = left.Source.Footprint.TopLeft;
        WorldPosition rightPosition = right.Source.Footprint.TopLeft;

        rv = leftPosition.X.CompareTo(rightPosition.X);
        if (rv != 0)
        {
            return rv;
        }

        rv = leftPosition.Y.CompareTo(rightPosition.Y);
        if (rv != 0)
        {
            return rv;
        }

        return 0;
    }
}
