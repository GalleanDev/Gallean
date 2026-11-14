namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldUnitModifierAppliedEvent ev, WorldState state)
    {
        UnitGrid<WorldUnit> units = WorldUnitGroupCalculator.GetUnits(state, ev.Group);
        WorldUnit target = units.GetUnit(ev.UnitId);
        UnitModifierDefinition definition = Context.Definitions.GetUnitModifier(ev.Modifier.DefinitionId);

        WorldUnit modified = target with
        {
            Modifiers = target.Modifiers.Add(ev.Modifier)
        };

        UnitGrid<WorldUnit> updatedUnits = units.Replace(target, IncreaseHitpoints(modified, definition.Hitpoints.Flat));

        return WorldUnitGroupCalculator.WithUnits(state, ev.Group, updatedUnits);
    }

    private WorldUnit IncreaseHitpoints(WorldUnit unit, int flatHitpoints)
    {
        if (flatHitpoints > 0)
        {
            UnitStats stats = Context.UnitStats.GetWorldUnitStats(unit);

            return unit with { Hitpoints = Math.Min(unit.Hitpoints + flatHitpoints, stats.MaxHitpoints) };
        }
        else
        {
            return unit;
        }
    }
}
