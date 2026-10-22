namespace Gallean.Engine;

internal static class WorldUnitGridExtensions
{
    extension(UnitGrid<WorldUnit> grid)
    {
        public bool HasAliveUnits()
        {
            foreach ((WorldUnit unit, _) in grid.GetUnits())
            {
                if (unit.IsAlive)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
