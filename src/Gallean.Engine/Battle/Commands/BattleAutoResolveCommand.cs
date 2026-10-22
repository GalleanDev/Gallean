using Gallean.Common;

namespace Gallean.Engine;

public sealed record BattleAutoResolveCommand : IBattleCommand<Empty>
{
    public BattleCommandResult Execute(BattleState state, IEngineContext context)
    {
        return BattleCommandResult.Success(
            Empty.Value,
            [new BattleAutoResolveStepEvent(state.RoundNumber)]);
    }
}
