using System.Collections.Immutable;
using Gallean.Common;

namespace Gallean.Engine.Contracts;

public sealed record WorldPlayerSpellbook(
    ImmutableSortedSet<WorldSpellDefinitionId> Available,
    ImmutableSortedSet<WorldSpellDefinitionId> Acquired)
{
    public bool Equals(WorldPlayerSpellbook? other)
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
