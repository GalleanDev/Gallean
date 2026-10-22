namespace Gallean.Engine;

internal partial class WorldEngine
{
    private sealed class PlayerDefeatedEndScenarioRule(IEngineContext context) : WorldEventRule(context)
    {
        public override EventRuleResult<WorldEvent> Visit(WorldPlayerDefeatedEvent ev, WorldState state)
        {
            if (state.ScenarioState.IsEnded)
            {
                return EventRuleResult<WorldEvent>.Empty;
            }

            bool anyHumanExists = false;
            bool anyHumanRemains = false;
            List<WorldPlayer> remainingPlayers = [];
            WorldPlan plan = WorldPlan.For(state);

            foreach (WorldPlayer player in state.Players.Values)
            {
                if (!WorldHelpers.IsRacePlayable(player.Race))
                {
                    continue;
                }

                if (!player.IsAi)
                {
                    anyHumanExists = true;
                }

                if (!plan.IsPlayerDefeated(player.Id))
                {
                    remainingPlayers.Add(player);

                    if (!player.IsAi)
                    {
                        anyHumanRemains = true;
                    }
                }
            }

            if (remainingPlayers.Count == 1)
            {
                return EventRuleResult<WorldEvent>.Append(new WorldScenarioEndedEvent(remainingPlayers[0].Id));
            }

            if (anyHumanExists && !anyHumanRemains)
            {
                return EventRuleResult<WorldEvent>.Append(new WorldScenarioEndedEvent(null));
            }

            return EventRuleResult<WorldEvent>.Empty;
        }
    }
}
