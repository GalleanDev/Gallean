namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldLeaderRenamedEvent ev, WorldState state)
    {
        WorldUnitGroup group = new WorldUnitGroup.Stack(ev.StackId);
        UnitGrid<WorldUnit> units = WorldUnitGroupCalculator.GetUnits(state, group);
        WorldUnit leader = units.GetUnit(ev.UnitId);
        UnitGrid<WorldUnit> updatedUnits = units.Replace(leader, leader with { Name = ev.Name });

        return WorldUnitGroupCalculator.WithUnits(state, group, updatedUnits);
    }
}
