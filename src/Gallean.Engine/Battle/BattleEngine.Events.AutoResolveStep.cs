namespace Gallean.Engine;

internal partial class BattleEngine
{
    BattleState IBattleEventVisitor<BattleState, BattleState>.Visit(BattleAutoResolveStepEvent ev, BattleState state)
    {
        return state;
    }
}
