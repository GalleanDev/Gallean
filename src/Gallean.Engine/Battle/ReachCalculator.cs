namespace Gallean.Engine;

internal static class ReachCalculator
{
    public static bool IsTargetReachable(BattleState state, BattleUnit attacker, BattleUnit target, UnitAttackDefinition attack)
    {
        if (attack.Reach is UnitAttackReach.Any or UnitAttackReach.All)
        {
            return true;
        }

        UnitGrid<BattleUnit> attackerGrid = state.GetGrid(attacker.Side);
        UnitGrid<BattleUnit> targetGrid = state.GetGrid(attacker.Side.GetOpposite());

        GridPosition? attackerPosition = FindPosition(attackerGrid, attacker);

        if (attackerPosition is not null && attackerPosition.IsBackLine && !IsGridFrontLineEmpty(attackerGrid))
        {
            return false;
        }

        GridPosition? targetPosition = FindPosition(targetGrid, target);

        if (targetPosition is null)
        {
            return true;
        }

        if (targetPosition.IsBackLine && !IsGridFrontLineEmpty(targetGrid))
        {
            return false;
        }

        if (attackerPosition is not null && Math.Abs(attackerPosition.Flank - targetPosition.Flank) > 1)
        {
            BattleUnit? centerBlocker = targetGrid[new GridPosition(targetPosition.Line, 1)];
            if (centerBlocker is not null && centerBlocker.IsActive())
            {
                return false;
            }

            BattleUnit? flankBlocker = targetGrid[new GridPosition(targetPosition.Line, attackerPosition.Flank)];
            if (flankBlocker is not null && flankBlocker.IsActive())
            {
                return false;
            }
        }

        return true;
    }

    private static GridPosition? FindPosition(UnitGrid<BattleUnit> grid, BattleUnit unit)
    {
        foreach ((BattleUnit gridUnit, GridPosition position) in grid.GetUnits())
        {
            if (gridUnit.Id == unit.Id)
            {
                return position;
            }
        }

        return null;
    }

    private static bool IsGridFrontLineEmpty(UnitGrid<BattleUnit> grid)
    {
        for (int flank = 0; flank < GridPosition.FlankCount; flank++)
        {
            GridPosition position = GridPosition.AtFrontLine(flank);
            BattleUnit? unit = grid[position];

            if (unit is not null && unit.IsActive())
            {
                return false;
            }
        }

        return true;
    }
}
