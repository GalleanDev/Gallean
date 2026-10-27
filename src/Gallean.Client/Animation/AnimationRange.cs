using Gallean.Common;

namespace Gallean.Client;

internal sealed record AnimationRange(IReadOnlyList<AnimationProperty> Current, IReadOnlyList<AnimationProperty> Maximum)
{
    public static readonly AnimationRange None = new([], []);

    public bool Equals(AnimationRange? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Current.SequenceEqual(other.Current) &&
               Maximum.SequenceEqual(other.Maximum);
    }

    public override int GetHashCode()
    {
        HashCode hash = new();

        hash.AddSequence(Current);
        hash.AddSequence(Maximum);

        return hash.ToHashCode();
    }
}
