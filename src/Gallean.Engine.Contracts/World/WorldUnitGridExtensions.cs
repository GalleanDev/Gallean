namespace Gallean.Engine.Contracts;

public static class WorldUnitGridExtensions
{
    extension(UnitGrid<WorldUnit> grid)
    {
        public bool TryGetUnit(WorldUnitId id, out (WorldUnit Unit, GridPosition Position) entry)
        {
            foreach ((WorldUnit Unit, GridPosition Position) candidate in grid.GetUnits())
            {
                if (candidate.Unit.Id == id)
                {
                    entry = candidate;

                    return true;
                }
            }

            entry = default;

            return false;
        }

        public WorldUnit GetUnit(WorldUnitId id)
        {
            foreach ((WorldUnit unit, GridPosition _) in grid.GetUnits())
            {
                if (unit.Id == id)
                {
                    return unit;
                }
            }

            throw new KeyNotFoundException($"Unit '{id}' was not found.");
        }
    }
}
