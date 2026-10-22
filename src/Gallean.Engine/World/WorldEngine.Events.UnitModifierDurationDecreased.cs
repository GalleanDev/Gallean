namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldUnitModifierDurationDecreasedEvent ev, WorldState state)
    {
        UnitGrid<WorldUnit> units = WorldUnitGroupCalculator.GetUnits(state, ev.Group);
        WorldUnit target = units.GetUnit(ev.UnitId);

        int index = target.Modifiers.IndexOf(target.Modifiers.First(modifier => modifier.DefinitionId == ev.ModifierId));

        UnitGrid<WorldUnit> updatedUnits = units.Replace(target, target with
        {
            Modifiers = target.Modifiers.SetItem(index, new UnitModifier(ev.ModifierId, ev.RemainingTurns))
        });

        return WorldUnitGroupCalculator.WithUnits(state, ev.Group, updatedUnits);
    }
}
