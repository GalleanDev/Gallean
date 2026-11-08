using Gallean.Common;

namespace Gallean.Engine;

public sealed record BattleAiActionCommand(bool CanUseItems) : IBattleCommand<Empty>
{
    public BattleCommandResult Execute(BattleState state, IEngineContext context)
    {
        if (state.Outcome is not null)
        {
            return BattleCommandResult.Fail(BattleCommandError.ActionNotAvailable);
        }

        BattleAiActionPlan plan = new BattleAiPlanner(context, canUseItems: CanUseItems).DecideAction(state);

        return plan.Command.Execute(state, context);
    }
}
