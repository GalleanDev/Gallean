using System.Collections.Immutable;
using Gallean.Common;

namespace Gallean.Engine.Contracts;

public sealed record WorldInventory(ImmutableArray<WorldItem> Items)
{
    public static WorldInventory Empty { get; } = new(ImmutableArray<WorldItem>.Empty);

    public bool Equals(WorldInventory? other)
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
