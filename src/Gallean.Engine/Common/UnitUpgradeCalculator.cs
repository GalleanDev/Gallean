using System.Collections.Immutable;

namespace Gallean.Engine;

internal static class UnitUpgradeCalculator
{
    public static UnitUpgrade GetUpgrade(
        UnitDefinitionId unitId,
        bool isTypeLocked,
        WorldPlayerBuildings buildings,
        IGameDefinitionProvider definitions)
    {
        ImmutableArray<UnitUpgradeDefinition> upgrades = isTypeLocked ? [] : definitions.GetUnitUpgrades(unitId);
        bool isBlocked = false;

        foreach (UnitUpgradeDefinition upgrade in upgrades)
        {
            if (upgrade.RequiredBuildingId is null)
            {
                return new UnitUpgrade.TypeUpgrade(upgrade.ToUnitId);
            }

            PlayerBuildingsCalculator.BuildingInfo info = PlayerBuildingsCalculator.GetInfo(buildings, upgrade.RequiredBuildingId.Value, definitions);

            if (info.Status is WorldPlayerBuildingStatus.Acquired)
            {
                return new UnitUpgrade.TypeUpgrade(upgrade.ToUnitId);
            }
            else if (info.Status == WorldPlayerBuildingStatus.Constructible)
            {
                isBlocked = true;
            }
            else if (info.Status == WorldPlayerBuildingStatus.MissingPrerequisite)
            {
                isBlocked = true;
            }
        }

        if (isBlocked)
        {
            return new UnitUpgrade.Blocked();
        }
        else
        {
            return new UnitUpgrade.DynamicUpgrade();
        }
    }
}
