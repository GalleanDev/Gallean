using System.Collections.Immutable;
using Gallean.Common;

namespace Gallean.Engine;

public sealed record LordDefinition(
    LordDefinitionId Id,
    WorldRace Race,
    LordCategory Category,
    GlobalTextId NameTextId,
    GlobalTextId DescriptionTextId,
    ImmutableArray<WorldPlayerBuildingDefinitionId> BuildingIds)
{
    public bool Equals(LordDefinition? other)
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
               Race == other.Race &&
               Category == other.Category &&
               NameTextId == other.NameTextId &&
               DescriptionTextId == other.DescriptionTextId &&
               BuildingIds.SequenceEqual(other.BuildingIds);
    }

    public override int GetHashCode()
    {
        HashCode hash = new();

        hash.Add(Id);
        hash.Add(Race);
        hash.Add(Category);
        hash.Add(NameTextId);
        hash.Add(DescriptionTextId);
        hash.AddSequence(BuildingIds);

        return hash.ToHashCode();
    }
}
