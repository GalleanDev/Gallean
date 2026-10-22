using System.Collections.Immutable;

namespace Gallean.Engine;

internal static class ScenarioEventPopupCalculator
{
    public static ImmutableArray<WorldPlayerId> ResolveAudience(
        ScenarioEventDefinition definition,
        ScenarioEventPopupEffect popup,
        WorldPlayerId triggererId,
        WorldState state)
    {
        WorldPlan plan = WorldPlan.For(state);

        ImmutableArray<WorldPlayerId>.Builder audience = ImmutableArray.CreateBuilder<WorldPlayerId>();

        foreach (WorldPlayer player in state.Players.Values)
        {
            if (CheckAudience(definition, popup, plan, triggererId, player))
            {
                audience.Add(player.Id);
            }
        }

        return audience.ToImmutable();
    }

    private static bool CheckAudience(
        ScenarioEventDefinition definition,
        ScenarioEventPopupEffect popup,
        WorldPlan plan,
        WorldPlayerId triggererId,
        WorldPlayer player)
    {
        return popup.Audience switch
        {
            ScenarioEventPopupAudience.Triggerer =>
                !plan.IsPlayerDefeated(player.Id) && player.Id == triggererId,
            ScenarioEventPopupAudience.AllPlayers =>
                !plan.IsPlayerDefeated(player.Id),
            ScenarioEventPopupAudience.AffectedRaces =>
                !plan.IsPlayerDefeated(player.Id) && WorldHelpers.IsRacePlayable(player.Race) && definition.AffectedRaces.Contains(player.Race)
        };
    }
}
