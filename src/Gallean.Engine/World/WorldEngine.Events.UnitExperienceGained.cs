namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldUnitExperienceGainedEvent ev, WorldState state)
    {
        UnitGrid<WorldUnit> units = WorldUnitGroupCalculator.GetUnits(state, ev.Group);
        WorldUnit target = units.GetUnit(ev.UnitId);

        UnitGrid<WorldUnit> updatedUnits = units.Replace(target, target with
        {
            Experience = target.Experience + ev.Experience
        });

        return WorldUnitGroupCalculator.WithUnits(state, ev.Group, updatedUnits);
    }
}
