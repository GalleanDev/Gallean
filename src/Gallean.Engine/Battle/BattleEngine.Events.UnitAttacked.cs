namespace Gallean.Engine;

internal partial class BattleEngine
{
    BattleState IBattleEventVisitor<BattleState, BattleState>.Visit(BattleUnitAttackedEvent ev, BattleState state)
    {
        return state;
    }
}
