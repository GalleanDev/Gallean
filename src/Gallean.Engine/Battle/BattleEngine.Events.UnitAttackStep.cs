namespace Gallean.Engine;

internal partial class BattleEngine
{
    BattleState IBattleEventVisitor<BattleState, BattleState>.Visit(BattleUnitAttackStepEvent ev, BattleState state)
    {
        return state;
    }
}
