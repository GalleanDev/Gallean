namespace Gallean.Engine;

internal sealed partial class BattleAiPlanner
{
    private sealed record SkipAction(IEngineContext Context, int Priority) : AbstractAction(Context, Priority)
    {
        public override BattleAiActionPlan? Plan(BattleState state, BattleUnit unit)
        {
            return MakePlan(1, new BattleSkipCommand(unit.Id));
        }
    }
}
