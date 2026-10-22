namespace Gallean.Engine;

internal partial class BattleEngine
{
    BattleState IBattleEventVisitor<BattleState, BattleState>.Visit(BattleUnitRetreatedEvent ev, BattleState state)
    {
        BattleUnit unit = state.GetUnit(ev.UnitId);

        return state.WithUnit(unit with
        {
            StatusEffects = unit.StatusEffects
                .RemoveAll(effect => effect is BattleRetreatStatusEffect)
                .Add(new BattleRetreatedStatusEffect())
        });
    }
}
