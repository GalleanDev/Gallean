using Gallean.Common;

namespace Gallean.Client;

internal sealed record AnimationSequence(
    TimeSpan Duration,
    IReadOnlyList<AnimationClip> Clips,
    IReadOnlyList<AnimationSound> Sounds,
    IReadOnlyList<AnimationProperty> Properties,
    IReadOnlyList<AnimationPause> Pauses) : IAnimationObject
{
    public TimeSpan GetTime(TimeSpan elapsed)
    {
        TimeSpan time = elapsed;

        foreach (AnimationPause pause in Pauses)
        {
            if (time <= pause.Start)
            {
                break;
            }

            if (time < pause.Start + pause.Duration)
            {
                return pause.Start;
            }

            time -= pause.Duration;
        }

        return time;
    }

    public bool Equals(AnimationSequence? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Duration == other.Duration &&
               Clips.SequenceEqual(other.Clips) &&
               Sounds.SequenceEqual(other.Sounds) &&
               Properties.SequenceEqual(other.Properties) &&
               Pauses.SequenceEqual(other.Pauses);
    }

    public override int GetHashCode()
    {
        HashCode hash = new();

        hash.Add(Duration);
        hash.AddSequence(Clips);
        hash.AddSequence(Sounds);
        hash.AddSequence(Properties);
        hash.AddSequence(Pauses);

        return hash.ToHashCode();
    }
}
