namespace Gallean.Engine;

internal partial class BattleEngine
{
    BattleState IBattleEventVisitor<BattleState, BattleState>.Visit(BattleUnitAttackGrantedEvent ev, BattleState state)
    {
        return state with
        {
            TurnQueue = state.TurnQueue.Insert(0, new BattleUnitTurn(ev.UnitId, BattleUnitTurnType.GrantedAttack))
        };
    }
}
