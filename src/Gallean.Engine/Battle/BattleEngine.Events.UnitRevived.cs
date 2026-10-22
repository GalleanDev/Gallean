namespace Gallean.Engine;

internal partial class BattleEngine
{
    BattleState IBattleEventVisitor<BattleState, BattleState>.Visit(BattleUnitRevivedEvent ev, BattleState state)
    {
        BattleUnit unit = state.GetUnit(ev.UnitId);

        return state.WithUnit(unit with { Hitpoints = ev.HitpointsAfter, IsRevived = true });
    }
}
