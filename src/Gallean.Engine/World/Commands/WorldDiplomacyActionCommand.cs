using Gallean.Common;

namespace Gallean.Engine;

public sealed record WorldDiplomacyActionCommand(
    WorldPlayerId TargetPlayerId,
    WorldDiplomacyAction Action) : IWorldCommand<Empty>
{
    public WorldCommandResult Execute(WorldState state, IEngineContext context)
    {
        WorldPlayerId currentPlayer = state.TurnInfo.CurrentPlayerId;

        if (TargetPlayerId == currentPlayer)
        {
            return WorldCommandResult.Fail(WorldCommandError.DiplomacyTargetInvalid);
        }

        if (!state.Players.TryGetValue(TargetPlayerId, out WorldPlayer? target))
        {
            return WorldCommandResult.Fail(WorldCommandError.DiplomacyTargetInvalid);
        }

        WorldPlan plan = WorldPlan.For(state);

        if (plan.IsPlayerDefeated(TargetPlayerId) || target.Race == WorldRace.Neutral)
        {
            return WorldCommandResult.Fail(WorldCommandError.DiplomacyTargetInvalid);
        }

        IWorldDiplomacyActionResolver resolver = WorldDiplomacyActionResolverFactory.Create(Action);

        return resolver.Resolve(state, context, currentPlayer, TargetPlayerId);
    }
}
