namespace Gallean.Engine;

public static class BattleStateExtensions
{
    extension(BattleState state)
    {
        public BattleUnit CurrentUnit => state.GetUnit(state.CurrentUnitId);

        public UnitGrid<BattleUnit> GetGrid(BattleSide side)
        {
            if (side == BattleSide.Attacker)
            {
                return state.AttackerGrid;
            }
            else
            {
                return state.DefenderGrid;
            }
        }

        public BattleUnit GetUnit(BattleUnitId id)
        {
            BattleUnit? unit = state.TryGetUnit(id);

            if (unit is null)
            {
                throw new KeyNotFoundException($"Battle unit with ID '{id}' was not found.");
            }

            return unit;
        }

        public BattleUnit? TryGetUnit(BattleUnitId id)
        {
            return FindUnit(state.AttackerGrid, id) ?? FindUnit(state.DefenderGrid, id);
        }

        public IReadOnlyList<BattleUnitId> GetAllUnitIds()
        {
            List<BattleUnitId> ids = [];

            foreach ((BattleUnit unit, _) in state.AttackerGrid.GetUnits())
            {
                ids.Add(unit.Id);
            }

            foreach ((BattleUnit unit, _) in state.DefenderGrid.GetUnits())
            {
                ids.Add(unit.Id);
            }

            return ids;
        }

        public BattleUnitId GetNextUnitId()
        {
            int nextId = 0;

            foreach (BattleUnitId id in state.GetAllUnitIds())
            {
                if (id.Value > nextId)
                {
                    nextId = id.Value;
                }
            }

            return new BattleUnitId(nextId + 1);
        }

        public BattleState WithUnit(BattleUnit unit)
        {
            UnitGrid<BattleUnit> attackerGrid = state.AttackerGrid;
            UnitGrid<BattleUnit> defenderGrid = state.DefenderGrid;

            if (unit.Side == BattleSide.Attacker)
            {
                attackerGrid = attackerGrid.Replace(u => u.Id == unit.Id, unit);
            }
            else
            {
                defenderGrid = defenderGrid.Replace(u => u.Id == unit.Id, unit);
            }

            return state with
            {
                AttackerGrid = attackerGrid,
                DefenderGrid = defenderGrid
            };
        }
    }

    private static BattleUnit? FindUnit(UnitGrid<BattleUnit> grid, BattleUnitId id)
    {
        foreach (GridPosition position in GridPosition.All)
        {
            BattleUnit? unit = grid[position];

            if (unit is not null && unit.Id == id)
            {
                return unit;
            }
        }

        return null;
    }
}
