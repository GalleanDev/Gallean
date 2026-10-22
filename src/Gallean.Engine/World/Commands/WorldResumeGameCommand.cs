using Gallean.Common;

namespace Gallean.Engine;

public sealed record WorldResumeGameCommand : IWorldCommand<Empty>
{
    public WorldCommandResult Execute(WorldState state, IEngineContext context)
    {
        return WorldCommandResult.Success(
            Empty.Value,
            [new WorldGameResumedEvent(state.TurnInfo.CurrentPlayerId)]);
    }
}
