using Gallean.Common;

namespace Gallean.Engine;

public sealed record WorldEndTurnCommand : IWorldCommand<Empty>
{
    public WorldCommandResult Execute(WorldState state, IEngineContext context)
    {
        WorldPlayerId currentPlayer = state.TurnInfo.CurrentPlayerId;

        List<WorldEvent> events = [new WorldTurnEndedEvent(currentPlayer)];
        WorldPlayerId nextPlayerId = currentPlayer;
        WorldPlan plan = WorldPlan.For(state);

        while (true)
        {
            nextPlayerId = state.GetNextPlayerId(nextPlayerId);

            if (!plan.IsPlayerDefeated(nextPlayerId))
            {
                break;
            }

            events.Add(new WorldTurnEndedEvent(nextPlayerId));

            if (nextPlayerId == currentPlayer)
            {
                break;
            }
        }

        events.Add(new WorldTurnStartingEvent(nextPlayerId));

        return WorldCommandResult.Success(Empty.Value, events);
    }
}
