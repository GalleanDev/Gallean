using System.Collections.Immutable;
using Gallean.Common;

namespace Gallean.Engine.Contracts;

public sealed record LeaderEquipment(ImmutableSortedDictionary<LeaderEquipmentSlot, WorldItem> Items)
{
    public static LeaderEquipment Empty { get; } = new(ImmutableSortedDictionary<LeaderEquipmentSlot, WorldItem>.Empty);

    public WorldItem? this[LeaderEquipmentSlot slot] => CollectionExtensions.GetValueOrDefault(Items, slot);

    public bool Equals(LeaderEquipment? other)
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

    public LeaderEquipment With(LeaderEquipmentSlot slot, WorldItem item)
    {
        return new LeaderEquipment(Items.SetItem(slot, item));
    }

    public LeaderEquipment Without(LeaderEquipmentSlot slot)
    {
        return new LeaderEquipment(Items.Remove(slot));
    }

    public override int GetHashCode()
    {
        HashCode hash = new();

        hash.AddSequence(Items);

        return hash.ToHashCode();
    }
}
