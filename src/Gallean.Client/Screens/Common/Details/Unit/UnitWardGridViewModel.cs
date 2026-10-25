using Gallean.Common;

namespace Gallean.Client;

internal sealed record UnitWardGridViewModel(IReadOnlyList<UnitWardItemViewModel> Items)
{
    public bool Equals(UnitWardGridViewModel? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Items.SequenceEqual(other.Items);
    }

    public override int GetHashCode()
    {
        HashCode hash = new();

        hash.AddSequence(Items);

        return hash.ToHashCode();
    }
}
