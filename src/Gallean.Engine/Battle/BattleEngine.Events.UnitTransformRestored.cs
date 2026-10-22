namespace Gallean.Engine;

internal partial class BattleEngine
{
    BattleState IBattleEventVisitor<BattleState, BattleState>.Visit(BattleUnitTransformRestoredEvent ev, BattleState state)
    {
        return BattleTransformationCalculator.Restore(state, Context, ev.UnitId);
    }
}
