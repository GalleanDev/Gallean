namespace Gallean.Engine;

internal partial class BattleEngine
{
    BattleState IBattleEventVisitor<BattleState, BattleState>.Visit(BattleUnitRetreatingEvent ev, BattleState state)
    {
        BattleUnit unit = state.GetUnit(ev.UnitId);

        return state.WithUnit(unit with { StatusEffects = unit.StatusEffects.Add(new BattleRetreatStatusEffect()) });
    }
}
