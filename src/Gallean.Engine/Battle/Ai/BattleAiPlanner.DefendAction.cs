namespace Gallean.Engine;

internal sealed partial class BattleAiPlanner
{
    private sealed record DefendAction(IEngineContext Context, int Priority) : AbstractAction(Context, Priority)
    {
        public override BattleAiActionPlan? Plan(BattleState state, BattleUnit unit)
        {
            return MakePlan(0, new BattleDefendCommand(unit.Id));
        }
    }
}
