namespace Gallean.Engine;

internal static class WorldMovementCalculator
{
    public sealed record MovementProfile(
        UnitMoveAbility MoveAbility,
        bool IsWaterOnly,
        bool IsLeaderDead,
        bool IsThief);

    public static MovementProfile GetMovementProfile(WorldStack stack, IGameDefinitionProvider definitions)
    {
        (WorldUnit leader, GridPosition _, UnitDefinition leaderDefinition) = stack.GetLeader(definitions);

        UnitMoveAbility moveAbility = leaderDefinition.MoveAbility;

        foreach (UnitModifier modifier in leader.Modifiers)
        {
            UnitModifierDefinition modifierDefinition = definitions.GetUnitModifier(modifier.DefinitionId);

            moveAbility |= modifierDefinition.MoveAbility;
        }

        bool isWaterOnly = leaderDefinition.IsWaterOnly;
        bool leaderDead = !leader.IsAlive;
        bool isThief = leaderDefinition.LeaderCategory == UnitLeaderCategory.Thief;

        return new MovementProfile(moveAbility, isWaterOnly, leaderDead, isThief);
    }

    public static int GetMovementCost(WorldMap map, WorldPosition position, MovementProfile profile)
    {
        int cost = GetBaseMovementCost(map, position, profile);

        if (cost == int.MaxValue)
        {
            return int.MaxValue;
        }

        if (profile.IsLeaderDead)
        {
            return cost * 2;
        }

        return cost;
    }

    private static int GetBaseMovementCost(WorldMap map, WorldPosition position, MovementProfile profile)
    {
        WorldMapCell cell = map.GetCellAt(position);

        if (profile.IsWaterOnly)
        {
            if (cell.Terrain != WorldMapTerrain.Water)
            {
                return int.MaxValue;
            }

            return IsDeepWater(map, position) ? 2 : 6;
        }

        if ((profile.MoveAbility & UnitMoveAbility.Flying) == UnitMoveAbility.Flying)
        {
            if (cell.Feature == WorldMapFeature.Mountain)
            {
                return int.MaxValue;
            }

            return 2;
        }

        if (cell.Terrain == WorldMapTerrain.Water)
        {
            return (profile.MoveAbility & UnitMoveAbility.Water) != 0 ? 2 : 6;
        }

        return cell.Feature switch
        {
            WorldMapFeature.None => 2,
            WorldMapFeature.Forest => (profile.MoveAbility & UnitMoveAbility.Forest) != 0 ? 2 : 4,
            WorldMapFeature.Mountain => int.MaxValue,
            WorldMapFeature.Road => 1
        };
    }

    public static bool IsPassable(WorldMap map, WorldPosition position, MovementProfile profile)
    {
        return GetMovementCost(map, position, profile) != int.MaxValue;
    }

    public static bool CanBeginStep(int movementPoints, int stepCost, int totalCost)
    {
        if (stepCost > 0)
        {
            return totalCost < movementPoints;
        }
        else
        {
            return true;
        }
    }

    public static bool CanBeginBattle(int movementPoints, int totalCost)
    {
        return totalCost < movementPoints;
    }

    public static int GetActionCost(StackStats stackStats, GlobalVariables globals)
    {
        return (stackStats.MaxMovementPoints * globals.ActionMovementCostPercent + 99) / 100;
    }

    private static bool IsDeepWater(WorldMap map, WorldPosition position)
    {
        for (int dy = -1; dy <= 1; dy++)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                if (dx != 0 || dy != 0)
                {
                    WorldPosition neighbor = new(position.X + dx, position.Y + dy);

                    if (map.IsValidPosition(neighbor) && map.GetCellAt(neighbor).Terrain != WorldMapTerrain.Water)
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }
}
