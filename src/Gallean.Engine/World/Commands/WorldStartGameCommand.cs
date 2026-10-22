using Gallean.Common;

namespace Gallean.Engine;

public sealed record WorldStartGameCommand : IWorldCommand<Empty>
{
    public WorldCommandResult Execute(WorldState state, IEngineContext context)
    {
        WorldPlayerId firstPlayerId = state.GetFirstPlayerId();

        return WorldCommandResult.Success(Empty.Value, [new WorldTurnStartingEvent(firstPlayerId)]);
    }
}
