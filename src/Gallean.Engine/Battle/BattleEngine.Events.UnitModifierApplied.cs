namespace Gallean.Engine;

internal partial class BattleEngine
{
    BattleState IBattleEventVisitor<BattleState, BattleState>.Visit(BattleUnitModifierAppliedEvent ev, BattleState state)
    {
        BattleUnit unit = state.GetUnit(ev.UnitId);
        UnitModifierDefinition modifier = Context.Definitions.GetUnitModifier(ev.Modifier.DefinitionId);

        return state.WithUnit(unit with
        {
            Modifiers = unit.Modifiers.Add(ev.Modifier),
            Wards = unit.Wards.AddRange(modifier.Wards)
        });
    }
}
