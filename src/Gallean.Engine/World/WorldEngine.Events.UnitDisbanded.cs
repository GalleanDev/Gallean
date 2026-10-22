namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldUnitDisbandedEvent ev, WorldState state)
    {
        UnitGrid<WorldUnit> units = WorldUnitGroupCalculator.GetUnits(state, ev.Group);
        WorldUnit target = units.GetUnit(ev.UnitId);

        UnitGrid<WorldUnit>.Builder builder = new();

        foreach ((WorldUnit unit, GridPosition position) in units.GetUnits())
        {
            if (ReferenceEquals(unit, target))
            {
                continue;
            }

            builder.Place(unit, position, units.IsLargeAt(position));
        }

        return WorldUnitGroupCalculator.WithUnits(state, ev.Group, builder.Build());
    }
}
