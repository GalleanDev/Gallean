namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldUnitModifierExpiredEvent ev, WorldState state)
    {
        UnitGrid<WorldUnit> units = WorldUnitGroupCalculator.GetUnits(state, ev.Group);
        WorldUnit target = units.GetUnit(ev.UnitId);

        WorldUnit unmodified = target with
        {
            Modifiers = target.Modifiers.RemoveAll(modifier => modifier.DefinitionId == ev.ModifierId)
        };

        UnitStats stats = Context.UnitStats.GetWorldUnitStats(unmodified);

        UnitGrid<WorldUnit> updatedUnits = units.Replace(target, unmodified with
        {
            Hitpoints = Math.Min(unmodified.Hitpoints, stats.MaxHitpoints)
        });

        return WorldUnitGroupCalculator.WithUnits(state, ev.Group, updatedUnits);
    }
}
