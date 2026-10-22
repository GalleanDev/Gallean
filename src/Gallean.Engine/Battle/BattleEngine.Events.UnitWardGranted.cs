namespace Gallean.Engine;

internal partial class BattleEngine
{
    BattleState IBattleEventVisitor<BattleState, BattleState>.Visit(BattleUnitWardGrantedEvent ev, BattleState state)
    {
        BattleUnit unit = state.GetUnit(ev.UnitId);

        return state.WithUnit(unit with
        {
            Wards = unit.Wards.Add(ev.Modifier.Ward),
            WardModifiers = unit.WardModifiers.Add(ev.Modifier)
        });
    }
}
