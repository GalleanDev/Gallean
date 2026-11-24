namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldUnitModifierDurationDecreasedEvent ev, WorldState state)
    {
        UnitGrid<WorldUnit> units = WorldUnitGroupCalculator.GetUnits(state, ev.Group);
        WorldUnit target = units.GetUnit(ev.UnitId);

        UnitModifier modifier = target.Modifiers.First(modifier => modifier.DefinitionId == ev.ModifierId);
        int index = target.Modifiers.IndexOf(modifier);

        UnitGrid<WorldUnit> updatedUnits = units.Replace(target, target with
        {
            Modifiers = target.Modifiers.SetItem(index, modifier with { RemainingTurns = ev.RemainingTurns })
        });

        return WorldUnitGroupCalculator.WithUnits(state, ev.Group, updatedUnits);
    }
}
