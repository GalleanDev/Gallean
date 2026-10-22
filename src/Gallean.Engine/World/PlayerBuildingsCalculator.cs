using System.Collections.Immutable;

namespace Gallean.Engine;

internal static partial class PlayerBuildingsCalculator
{
    public static bool IsAcquired(
        WorldPlayerBuildings buildings,
        WorldPlayerBuildingCategory category,
        IGameDefinitionProvider definitions)
    {
        foreach (WorldPlayerBuildingDefinitionId buildingId in buildings.Acquired)
        {
            if (definitions.GetPlayerBuilding(buildingId).Category == category)
            {
                return true;
            }
        }

        return false;
    }

    public static WorldPlayerBuildingDefinitionId? FindAvailable(
        WorldPlayerBuildings buildings,
        WorldPlayerBuildingCategory category,
        IGameDefinitionProvider definitions)
    {
        foreach (WorldPlayerBuildingDefinitionId buildingId in buildings.Available)
        {
            if (definitions.GetPlayerBuilding(buildingId).Category == category)
            {
                return buildingId;
            }
        }

        return null;
    }

    public static BuildingInfo GetInfo(
        WorldPlayerBuildings buildings,
        WorldPlayerBuildingDefinitionId buildingId,
        IGameDefinitionProvider definitions)
    {
        WorldPlayerBuildingStatus status = GetStatus(buildings, buildingId, definitions);

        ImmutableArray<WorldPlayerBuildingDefinitionId> lockedIds = status switch
        {
            WorldPlayerBuildingStatus.Constructible or WorldPlayerBuildingStatus.MissingPrerequisite => GetLockedIds(buildings, buildingId, definitions),
            WorldPlayerBuildingStatus.Acquired or WorldPlayerBuildingStatus.BranchLocked or WorldPlayerBuildingStatus.ScenarioLocked => []
        };

        return new BuildingInfo(status, lockedIds);
    }

    private static WorldPlayerBuildingStatus GetStatus(
        WorldPlayerBuildings buildings,
        WorldPlayerBuildingDefinitionId buildingId,
        IGameDefinitionProvider definitions)
    {
        if (buildings.Acquired.Contains(buildingId))
        {
            return WorldPlayerBuildingStatus.Acquired;
        }
        else if (!buildings.Available.Contains(buildingId))
        {
            return WorldPlayerBuildingStatus.ScenarioLocked;
        }
        else if (IsBranchLocked(buildings, buildingId, definitions))
        {
            return WorldPlayerBuildingStatus.BranchLocked;
        }
        else if (IsRequiredMissing(buildings, buildingId, definitions))
        {
            return WorldPlayerBuildingStatus.MissingPrerequisite;
        }
        else
        {
            return WorldPlayerBuildingStatus.Constructible;
        }
    }

    private static bool IsRequiredMissing(
        WorldPlayerBuildings buildings,
        WorldPlayerBuildingDefinitionId buildingId,
        IGameDefinitionProvider definitions)
    {
        WorldPlayerBuildingDefinition building = definitions.GetPlayerBuilding(buildingId);

        return building.RequiredId is not null && !buildings.Acquired.Contains(building.RequiredId.Value);
    }

    private static bool IsBranchLocked(
        WorldPlayerBuildings buildings,
        WorldPlayerBuildingDefinitionId buildingId,
        IGameDefinitionProvider definitions)
    {
        WorldPlayerBuildingDefinitionId? currentId = buildingId;

        while (currentId is not null && !buildings.Acquired.Contains(currentId.Value))
        {
            WorldPlayerBuildingDefinition building = definitions.GetPlayerBuilding(currentId.Value);

            foreach (WorldPlayerBuildingDefinitionId acquiredId in buildings.Acquired)
            {
                WorldPlayerBuildingDefinition acquired = definitions.GetPlayerBuilding(acquiredId);

                if (IsAlternative(building, acquired))
                {
                    return true;
                }
            }

            currentId = building.RequiredId;
        }

        return false;
    }

    private static ImmutableArray<WorldPlayerBuildingDefinitionId> GetLockedIds(
        WorldPlayerBuildings buildings,
        WorldPlayerBuildingDefinitionId buildingId,
        IGameDefinitionProvider definitions)
    {
        ImmutableArray<WorldPlayerBuildingDefinitionId>.Builder result = ImmutableArray.CreateBuilder<WorldPlayerBuildingDefinitionId>();

        WorldPlayerBuildingDefinition building = definitions.GetPlayerBuilding(buildingId);

        foreach (WorldPlayerBuildingDefinitionId availableId in buildings.Available)
        {
            WorldPlayerBuildingDefinition available = definitions.GetPlayerBuilding(availableId);

            if (IsAlternative(building, available) && !IsBranchLocked(buildings, availableId, definitions))
            {
                result.Add(availableId);
            }
        }

        return result.ToImmutable();
    }

    private static bool IsAlternative(WorldPlayerBuildingDefinition building, WorldPlayerBuildingDefinition other)
    {
        return building.Id != other.Id &&
               building.UnitBranch is not null &&
               building.UnitBranch == other.UnitBranch &&
               building.Level == other.Level;
    }
}
