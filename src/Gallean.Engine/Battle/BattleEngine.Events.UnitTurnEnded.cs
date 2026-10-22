namespace Gallean.Engine;

internal partial class BattleEngine
{
    BattleState IBattleEventVisitor<BattleState, BattleState>.Visit(BattleUnitTurnEndedEvent ev, BattleState state)
    {
        return state;
    }
}
