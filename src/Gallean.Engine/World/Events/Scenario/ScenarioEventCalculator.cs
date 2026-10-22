using System.Collections.Immutable;
using Gallean.Common;

namespace Gallean.Engine;

internal static partial class ScenarioEventCalculator
{
    private const int TriggerChanceMax = 100;

    public static bool IsEnabled(WorldScenarioState scenarioState, ScenarioEventDefinition definition)
    {
        if (scenarioState.EventStates.TryGetValue(definition.Id, out bool enabled))
        {
            return enabled;
        }
        else
        {
            return definition.Enabled;
        }
    }

    public static ImmutableArray<WorldEvent> CascadeEvents(
        IGameScenarioProvider scenario,
        IRandomProvider random,
        GlobalVariables globals,
        WorldState state,
        ScenarioEventTrigger trigger,
        ImmutableSortedSet<ScenarioEventId> triggeredEvents)
    {
        if (state.ScenarioState.IsEnded)
        {
            return [];
        }

        if (state.TurnInfo.TurnNumber == 0)
        {
            return [];
        }

        WorldStack? triggererStack = ScenarioEventTriggererCalculator.FindStack(trigger, state);

        foreach (ScenarioEventDefinition definition in scenario.Events)
        {
            if (triggeredEvents.Contains(definition.Id) || !IsEnabled(state.ScenarioState, definition))
            {
                continue;
            }

            ImmutableArray<WorldEvent>.Builder events = ImmutableArray.CreateBuilder<WorldEvent>();

            foreach (WorldPlayer player in state.Players.Values)
            {
                if (definition.VerifiedRaces.Contains(player.Race) &&
                    (triggererStack is null || triggererStack.OwnerId == player.Id))
                {
                    ScenarioEventConditionContext context = new(
                        state,
                        scenario,
                        globals,
                        player.Id,
                        triggererStack,
                        definition.AffectedRaces,
                        trigger);

                    if (CheckConditions(definition.Conditions, context) && RollChance(random, definition))
                    {
                        events.Add(new WorldScenarioEventTriggeringEvent(definition.Id, player.Id, trigger));
                    }
                }
            }

            if (events.Count > 0)
            {
                if (!HasBattleEffect(definition))
                {
                    events.Add(new WorldScenarioCheckEvent(trigger, triggeredEvents.Add(definition.Id)));
                }

                return events.ToImmutable();
            }
        }

        return [];
    }

    private static bool HasBattleEffect(ScenarioEventDefinition definition)
    {
        foreach (ScenarioEventEffect effect in definition.Effects)
        {
            if (effect is ScenarioEventBattleEffect)
            {
                return true;
            }
        }

        return false;
    }

    private static bool RollChance(IRandomProvider random, ScenarioEventDefinition definition)
    {
        if (definition.Chance < TriggerChanceMax)
        {
            return random.Next(TriggerChanceMax) < definition.Chance;
        }
        else
        {
            return true;
        }
    }

    private static bool CheckConditions(ImmutableArray<ScenarioEventCondition> conditions, ScenarioEventConditionContext context)
    {
        foreach (ScenarioEventCondition condition in conditions)
        {
            if (!condition.Accept(ConditionChecker.Instance, context))
            {
                return false;
            }
        }

        return true;
    }
}
