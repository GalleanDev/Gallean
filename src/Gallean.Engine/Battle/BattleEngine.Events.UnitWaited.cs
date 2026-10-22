namespace Gallean.Engine;

internal partial class BattleEngine
{
    BattleState IBattleEventVisitor<BattleState, BattleState>.Visit(BattleUnitWaitedEvent ev, BattleState state)
    {
        return state with
        {
            WaitingQueue = state.WaitingQueue.Add(new BattleUnitTurn(ev.UnitId, BattleUnitTurnType.Resumed))
        };
    }
}
