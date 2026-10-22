namespace Gallean.Engine;

internal partial class BattleEngine
{
    BattleState IBattleEventVisitor<BattleState, BattleState>.Visit(BattleUnitHealedEvent ev, BattleState state)
    {
        BattleUnit unit = state.GetUnit(ev.UnitId);

        return state.WithUnit(unit with { Hitpoints = ev.HitpointsAfter });
    }
}
