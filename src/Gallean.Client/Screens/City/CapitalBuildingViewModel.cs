using Gallean.Common;
using Gallean.Engine;

namespace Gallean.Client;

internal sealed record CapitalBuildingViewModel(
    AssetSpriteKey.BuildingIcon IconKey,
    string Name,
    string Description,
    IReadOnlyList<CapitalBuildingUnitViewModel> Units)
{
    public bool HasUnits => Units.Count > 0;

    public static CapitalBuildingViewModel Create(
        WorldPlayerBuildingDefinitionId buildingId,
        IGameDefinitionProvider definitions,
        IGameTextProvider texts)
    {
        WorldPlayerBuildingDefinition building = definitions.GetPlayerBuilding(buildingId);
        List<CapitalBuildingUnitViewModel> units = [];

        foreach (UnitDefinitionId unitId in building.UnitIds)
        {
            UnitDefinition? unit = definitions.TryGetUnit(unitId);

            if (unit is not null)
            {
                units.Add(new CapitalBuildingUnitViewModel(
                    new AssetSpriteKey.UnitPortrait(unit.Id),
                    texts.GetGlobalText(unit.NameTextId).Text));
            }
        }

        return new CapitalBuildingViewModel(
            new AssetSpriteKey.BuildingIcon(buildingId),
            texts.GetGlobalText(building.NameTextId).Text,
            texts.GetGlobalText(building.DescriptionTextId).Text,
            units);
    }

    public bool Equals(CapitalBuildingViewModel? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return IconKey == other.IconKey &&
               Name == other.Name &&
               Description == other.Description &&
               Units.SequenceEqual(other.Units);
    }

    public override int GetHashCode()
    {
        HashCode hash = new();

        hash.Add(IconKey);
        hash.Add(Name);
        hash.Add(Description);
        hash.AddSequence(Units);

        return hash.ToHashCode();
    }
}
