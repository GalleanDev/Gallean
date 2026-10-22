namespace Gallean.Engine;

internal partial class BattleEngine
{
    BattleState IBattleEventVisitor<BattleState, BattleState>.Visit(BattleUnitAttackBlockedEvent ev, BattleState state)
    {
        return state;
    }
}
