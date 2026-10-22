namespace Gallean.Engine;

internal partial class BattleEngine
{
    BattleState IBattleEventVisitor<BattleState, BattleState>.Visit(BattleUnitTurnSkippedEvent ev, BattleState state)
    {
        return state;
    }
}
