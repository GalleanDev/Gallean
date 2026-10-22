using Gallean.Common;

namespace Gallean.Engine;

internal static class SummonCalculator
{
    public static bool CanSummon(
        UnitAttackDefinition attack,
        BattleState state,
        BattleSide side,
        GridPosition position,
        IGameDefinitionProvider definitions)
    {
        IReadOnlyList<UnitDefinitionId> candidates = GetCandidates(attack, state, side, position, definitions);

        return candidates.Count > 0;
    }

    public static IReadOnlyList<GridPosition> GetSummonPositions(BattleState state, BattleSide side)
    {
        List<GridPosition> positions = [];

        foreach (GridPosition position in GridPosition.All)
        {
            if (CanPlace(state, side, position, false))
            {
                positions.Add(position);
            }
        }

        return positions;
    }

    public static IReadOnlyList<BattleUnitId> GetOrphanedSummons(BattleState state)
    {
        List<BattleUnitId> result = [];

        foreach (BattleUnitId unitId in state.GetAllUnitIds())
        {
            BattleUnit summon = state.GetUnit(unitId);

            if (summon.SummonerId is { } summonerId && summon.IsActive())
            {
                BattleUnit? summoner = state.TryGetUnit(summonerId);

                if (summoner is null || !summoner.IsActive())
                {
                    result.Add(unitId);
                }
            }
        }

        return result;
    }

    public static IReadOnlyList<BattleSummonPlacement> GetSummonPlacements(
        UnitAttackDefinition attack,
        BattleState state,
        BattleSide side,
        IReadOnlyList<GridPosition> positions,
        IGameDefinitionProvider definitions,
        IRandomProvider random)
    {
        List<BattleSummonPlacement> placements = [];
        HashSet<GridPosition> occupiedPositions = [];

        foreach (GridPosition position in positions)
        {
            if (!occupiedPositions.Contains(position))
            {
                UnitDefinitionId? unitDefinitionId = SelectSummon(attack, state, side, position, definitions, random);

                if (unitDefinitionId is not null)
                {
                    bool isLarge = definitions.GetUnit(unitDefinitionId.Value).IsLarge;
                    GridPosition normalizedPosition = NormalizePosition(position, isLarge);

                    placements.Add(new BattleSummonPlacement(unitDefinitionId.Value, normalizedPosition));
                    occupiedPositions.UnionWith(EnumerateGridPositions(normalizedPosition, isLarge));
                }
            }
        }

        return placements;
    }

    private static UnitDefinitionId? SelectSummon(
        UnitAttackDefinition attack,
        BattleState state,
        BattleSide side,
        GridPosition position,
        IGameDefinitionProvider definitions,
        IRandomProvider random)
    {
        IReadOnlyList<UnitDefinitionId> candidates = GetCandidates(attack, state, side, position, definitions);

        if (candidates.Count <= 0)
        {
            return null;
        }

        int start = random.Next(candidates.Count);

        if (position.IsFrontLine)
        {
            return candidates[start];
        }
        else
        {
            for (int offset = 0; offset < candidates.Count; offset++)
            {
                int index = (start + offset) % candidates.Count;
                UnitDefinition definition = definitions.GetUnit(candidates[index]);

                if (definition.Attack.Reach != UnitAttackReach.Adjacent)
                {
                    return candidates[index];
                }
            }

            return candidates[start];
        }
    }

    private static IReadOnlyList<UnitDefinitionId> GetCandidates(
        UnitAttackDefinition attack,
        BattleState state,
        BattleSide side,
        GridPosition position,
        IGameDefinitionProvider definitions)
    {
        if (!CanPlace(state, side, position, false))
        {
            return [];
        }

        List<UnitDefinitionId> smallCandidates = [];
        List<UnitDefinitionId> largeCandidates = [];

        foreach (UnitDefinitionId summonUnitId in attack.SummonUnitIds)
        {
            UnitDefinition summonDefinition = definitions.GetUnit(summonUnitId);

            if (summonDefinition.IsLarge)
            {
                largeCandidates.Add(summonUnitId);
            }
            else
            {
                smallCandidates.Add(summonUnitId);
            }
        }

        if (largeCandidates.Count > 0 && CanPlace(state, side, position, true))
        {
            return largeCandidates;
        }
        else
        {
            return smallCandidates;
        }
    }

    private static bool CanPlace(BattleState state, BattleSide side, GridPosition position, bool isLarge)
    {
        UnitGrid<BattleUnit> grid = state.GetGrid(side);
        UnitGrid<BattleUnit> overlayGrid = grid.GetOverlayGrid();

        foreach (GridPosition gridPosition in EnumerateGridPositions(position, isLarge))
        {
            if (overlayGrid[gridPosition] is not null)
            {
                return false;
            }

            BattleUnit? unit = grid[gridPosition];

            if (unit is not null && unit.IsActive())
            {
                return false;
            }
        }

        return true;
    }

    private static GridPosition NormalizePosition(GridPosition position, bool isLarge)
    {
        if (isLarge)
        {
            return GridPosition.AtFrontLine(position.Flank);
        }
        else
        {
            return position;
        }
    }

    private static IEnumerable<GridPosition> EnumerateGridPositions(GridPosition position, bool isLarge)
    {
        if (isLarge)
        {
            yield return GridPosition.AtFrontLine(position.Flank);
            yield return GridPosition.AtBackLine(position.Flank);
        }
        else
        {
            yield return position;
        }
    }
}
