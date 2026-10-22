namespace Gallean.Engine;

internal partial class BattleEngine
{
    BattleState IBattleEventVisitor<BattleState, BattleState>.Visit(BattleUnitWardExpiredEvent ev, BattleState state)
    {
        BattleUnit unit = state.GetUnit(ev.UnitId);

        return state.WithUnit(unit with
        {
            Wards = unit.Wards.Remove(ev.Modifier.Ward),
            WardModifiers = unit.WardModifiers.Remove(ev.Modifier)
        });
    }
}
