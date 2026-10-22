namespace Gallean.Engine;

internal partial class BattleEngine
{
    BattleState IBattleEventVisitor<BattleState, BattleState>.Visit(BattleUnitWardConsumedEvent ev, BattleState state)
    {
        BattleUnit unit = state.GetUnit(ev.UnitId);

        return state.WithUnit(unit with { Wards = unit.Wards.Remove(ev.Ward) });
    }
}
