namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldUnitUpgradedEvent ev, WorldState state)
    {
        UnitGrid<WorldUnit> units = WorldUnitGroupCalculator.GetUnits(state, ev.Group);
        WorldUnit target = units.GetUnit(ev.UnitId);

        UnitGrid<WorldUnit> updatedUnits = units.Replace(target, target with
        {
            DefinitionId = ev.DefinitionId,
            Level = ev.Level,
            Hitpoints = ev.Hitpoints,
            Experience = 0
        });

        return WorldUnitGroupCalculator.WithUnits(state, ev.Group, updatedUnits);
    }
}
