using System.Collections.Immutable;
using Gallean.Common;

namespace Gallean.Engine;

internal sealed partial class WorldAiScenario(IEngineContext engine)
{
    public ImmutableHashSet<WorldAiTarget> GetObjectiveTargets(WorldState state, WorldPlayerId playerId)
    {
        WorldPlayer player = state.Players[playerId];

        ImmutableHashSet<WorldAiTarget>.Builder targets = ImmutableHashSet.CreateBuilder<WorldAiTarget>();

        foreach (ScenarioEventDefinition definition in GetEvents(state, player))
        {
            foreach (ScenarioEventCondition condition in definition.Conditions)
            {
                targets.UnionWith(condition.Accept(TargetVisitor.Instance, state));
            }
        }

        return targets.ToImmutable();
    }

    public ImmutableHashSet<WorldAiTarget> GetBlockedTargets(WorldState state)
    {
        ImmutableHashSet<ItemDefinitionId> items = GetItems(state);
        ImmutableHashSet<WorldAiTarget>.Builder targets = ImmutableHashSet.CreateBuilder<WorldAiTarget>();

        foreach (WorldBag bag in state.Bags.Values)
        {
            if (bag.Inventory.Items.Any(item => items.Contains(item.DefinitionId)))
            {
                targets.Add(new WorldAiTarget.Bag(bag.Id));
            }
        }

        return targets.ToImmutable();
    }

    public bool IsStackVital(WorldState state, WorldStack stack)
    {
        WorldPlayer owner = state.Players[stack.OwnerId];

        if (WorldHelpers.IsRacePlayable(owner.Race))
        {
            foreach (ScenarioEventDefinition definition in engine.Scenario.Events)
            {
                if (ScenarioEventCalculator.IsEnabled(state.ScenarioState, definition) && definition.Effects is [.., ScenarioEventWinEffect])
                {
                    foreach (ScenarioEventCondition condition in definition.Conditions)
                    {
                        ImmutableArray<WorldAiTarget> targets = condition.Accept(TargetVisitor.Instance, state);

                        if (targets.Contains(new WorldAiTarget.Stack(stack.Id)))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }
        else
        {
            return false;
        }
    }

    private ImmutableArray<ScenarioEventDefinition> GetEvents(WorldState state, WorldPlayer player)
    {
        ImmutableArray<ScenarioEventDefinition>.Builder victoryEvents = ImmutableArray.CreateBuilder<ScenarioEventDefinition>();
        ImmutableArray<ScenarioEventDefinition>.Builder defeatEvents = ImmutableArray.CreateBuilder<ScenarioEventDefinition>();

        foreach (ScenarioEventDefinition definition in engine.Scenario.Events)
        {
            ScenarioEventWinEffect? effect = CheckWinScenarioEffect(definition, state, player);

            if (effect is not null)
            {
                if (effect.WinnerId is { } winnerId)
                {
                    if (state.Players.TryGetValue(winnerId, out WorldPlayer? winnerPlayer) &&
                        winnerPlayer.Race == player.Race)
                    {
                        victoryEvents.Add(definition);
                    }
                }
                else
                {
                    defeatEvents.Add(definition);
                }
            }
        }

        if (victoryEvents.Count > 0)
        {
            return victoryEvents.ToImmutable();
        }
        else
        {
            return defeatEvents.ToImmutable();
        }
    }

    private static ScenarioEventWinEffect? CheckWinScenarioEffect(ScenarioEventDefinition definition, WorldState state, WorldPlayer player)
    {
        if (ScenarioEventCalculator.IsEnabled(state.ScenarioState, definition) &&
            definition.AffectedRaces.Contains(player.Race) &&
            definition.Effects is [.., ScenarioEventWinEffect effect])
        {
            return effect;
        }
        else
        {
            return null;
        }
    }

    private ImmutableHashSet<ItemDefinitionId> GetItems(WorldState state)
    {
        ImmutableHashSet<ItemDefinitionId>.Builder items = ImmutableHashSet.CreateBuilder<ItemDefinitionId>();

        foreach (ScenarioEventDefinition definition in engine.Scenario.Events)
        {
            if (ScenarioEventCalculator.IsEnabled(state.ScenarioState, definition))
            {
                foreach (ScenarioEventCondition condition in definition.Conditions)
                {
                    items.UnionWith(condition.Accept(ItemVisitor.Instance, Empty.Value));
                }
            }
        }

        return items.ToImmutable();
    }
}
