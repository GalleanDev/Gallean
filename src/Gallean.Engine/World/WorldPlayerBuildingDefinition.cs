using System.Collections.Immutable;
using Gallean.Common;

namespace Gallean.Engine;

public sealed record WorldPlayerBuildingDefinition(
    WorldPlayerBuildingDefinitionId Id,
    GlobalTextId NameTextId,
    GlobalTextId DescriptionTextId,
    WorldPlayerBuildingDefinitionId? RequiredId,
    WorldPlayerBuildingCategory Category,
    UnitBranch? UnitBranch,
    int Level,
    WorldResourceSet Cost,
    ImmutableArray<UnitDefinitionId> UnitIds)
{
    public bool Equals(WorldPlayerBuildingDefinition? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Id == other.Id &&
               NameTextId == other.NameTextId &&
               DescriptionTextId == other.DescriptionTextId &&
               RequiredId == other.RequiredId &&
               Category == other.Category &&
               UnitBranch == other.UnitBranch &&
               Level == other.Level &&
               Cost == other.Cost &&
               UnitIds.SequenceEqual(other.UnitIds);
    }

    public override int GetHashCode()
    {
        HashCode hash = new();

        hash.Add(Id);
        hash.Add(NameTextId);
        hash.Add(DescriptionTextId);
        hash.Add(RequiredId);
        hash.Add(Category);
        hash.Add(UnitBranch);
        hash.Add(Level);
        hash.Add(Cost);
        hash.AddSequence(UnitIds);

        return hash.ToHashCode();
    }
}
