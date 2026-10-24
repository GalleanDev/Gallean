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
                WorldPlayer player = remainingPlayers[0];

                if (!HasWinEvent(player.Race))
                {
                    return EventRuleResult<WorldEvent>.Append(new WorldScenarioEndedEvent(player.Id));
                }
            }

            if (anyHumanExists && !anyHumanRemains)
            {
                return EventRuleResult<WorldEvent>.Append(new WorldScenarioEndedEvent(null));
            }

            return EventRuleResult<WorldEvent>.Empty;
        }

        private bool HasWinEvent(WorldRace race)
        {
            foreach (ScenarioEventDefinition definition in Context.Scenario.Events)
            {
                if (definition.AffectedRaces.Contains(race) && definition.Effects is [.., ScenarioEventWinEffect])
                {
                    return true;
                }
            }

            return false;
        }
    }
}
