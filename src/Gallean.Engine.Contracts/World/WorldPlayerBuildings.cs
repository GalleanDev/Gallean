using System.Collections.Immutable;
using Gallean.Common;

namespace Gallean.Engine.Contracts;

public sealed record WorldPlayerBuildings(
    ImmutableSortedSet<WorldPlayerBuildingDefinitionId> Available,
    ImmutableSortedSet<WorldPlayerBuildingDefinitionId> Acquired)
{
    public bool Equals(WorldPlayerBuildings? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Available.SequenceEqual(other.Available) &&
               Acquired.SequenceEqual(other.Acquired);
    }

    public override int GetHashCode()
    {
        HashCode hash = new();

        hash.AddSequence(Available);
        hash.AddSequence(Acquired);

        return hash.ToHashCode();
    }
}
