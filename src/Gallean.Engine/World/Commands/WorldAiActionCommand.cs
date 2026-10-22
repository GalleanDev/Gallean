using Gallean.Common;

namespace Gallean.Engine;

internal sealed record WorldAiActionCommand : IWorldCommand<Empty>
{
    public WorldCommandResult Execute(WorldState state, IEngineContext context)
    {
        WorldPlayerId playerId = state.TurnInfo.CurrentPlayerId;
        WorldPlayer player = state.CurrentPlayer;

        if (!player.IsAi)
        {
            return WorldCommandResult.Fail(WorldCommandError.NotAiTurn);
        }

        WorldPlan worldPlan = WorldPlan.For(state);

        if (state.Players.Values.All(candidate => candidate.IsAi || worldPlan.IsPlayerDefeated(candidate.Id)))
        {
            return WorldCommandResult.Fail(WorldCommandError.NoPlayersRemaining);
        }

        WorldAiMemory memory = state.AiMemories.GetValueOrDefault(playerId, WorldAiMemory.Empty);

        WorldAiController controller = new(context);
        WorldAiActionPlan plan = controller.DecideAction(state, memory);

        WorldCommandResult commandResult = plan.Command.Execute(state, context);
        if (commandResult.Failed)
        {
            return commandResult;
        }

        List<WorldEvent> events =
        [
            new WorldAiActionStartedEvent(plan),
            .. commandResult.Events,
            new WorldAiActionEndedEvent(playerId, plan.Memory)
        ];

        return WorldCommandResult.Success(Empty.Value, events);
    }
}
