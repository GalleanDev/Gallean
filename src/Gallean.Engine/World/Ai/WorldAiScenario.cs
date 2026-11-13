using System.Collections.Immutable;

namespace Gallean.Engine;

internal sealed class WorldAiScenario(IEngineContext engine)
{
    public ImmutableHashSet<WorldAiTarget> GetObjectiveTargets(WorldState state, WorldPlayerId playerId)
    {
        WorldPlayer player = state.Players[playerId];

        ImmutableHashSet<WorldAiTarget>.Builder targets = ImmutableHashSet.CreateBuilder<WorldAiTarget>();

        foreach (ScenarioEventDefinition definition in GetEvents(state, player))
        {
            foreach (ScenarioEventCondition condition in definition.Conditions)
            {
                targets.UnionWith(GetConditionTargets(condition, state));
            }
        }

        return targets.ToImmutable();
    }

    public ImmutableHashSet<WorldAiTarget> GetBlockedTargets(WorldState state, WorldPlayerId playerId)
    {
        WorldPlayer player = state.Players[playerId];
        ImmutableHashSet<ItemDefinitionId> items = GetItems(state);
        ImmutableHashSet<WorldAiTarget>.Builder targets = ImmutableHashSet.CreateBuilder<WorldAiTarget>();

        foreach (WorldBag bag in state.Bags.Values)
        {
            if (bag.Inventory.Items.Any(item => items.Contains(item.DefinitionId)))
            {
                targets.Add(new WorldAiTarget.Bag(bag.Id));
            }
        }

        foreach (ScenarioLocationDefinition zone in GetDefeatZones(state, player))
        {
            foreach (WorldPosition position in ScenarioLocationCalculator.EnumerateTiles(zone, state.Map))
            {
                targets.Add(new WorldAiTarget.Location(position));
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
                        ImmutableArray<WorldAiTarget> targets = GetConditionTargets(condition, state);

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

    private ImmutableArray<ScenarioLocationDefinition> GetDefeatZones(WorldState state, WorldPlayer player)
    {
        ImmutableArray<ScenarioLocationDefinition>.Builder zones = ImmutableArray.CreateBuilder<ScenarioLocationDefinition>();

        foreach (ScenarioEventDefinition definition in engine.Scenario.Events)
        {
            ScenarioEventWinEffect? effect = CheckWinScenarioEffect(definition, state, player);

            if (effect is not null && effect.WinnerId is null)
            {
                foreach (ScenarioEventCondition condition in definition.Conditions)
                {
                    if (condition is ScenarioEventEnteringZoneCondition zoneCondition)
                    {
                        zones.Add(engine.Scenario.GetLocation(zoneCondition.Zone));
                    }
                }
            }
        }

        return zones.ToImmutable();
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
                    items.UnionWith(GetConditionItems(condition));
                }
            }
        }

        return items.ToImmutable();
    }

    private static ImmutableArray<ItemDefinitionId> GetConditionItems(ScenarioEventCondition condition)
    {
        return condition switch
        {
            ScenarioEventFrequencyCondition => [],
            ScenarioEventEnteringZoneCondition => [],
            ScenarioEventEnteringCityCondition => [],
            ScenarioEventOwningCityCondition => [],
            ScenarioEventDestroyingStackCondition => [],
            ScenarioEventOwningItemCondition owningItem => [owningItem.Item],
            ScenarioEventLeaderOwningItemCondition leaderOwningItem => [leaderOwningItem.Item],
            ScenarioEventDiplomacyRelationsCondition => [],
            ScenarioEventAllianceCondition => [],
            ScenarioEventLootingRuinCondition => [],
            ScenarioEventTransformingLandCondition => [],
            ScenarioEventVisitingSiteCondition => [],
            ScenarioEventStackToLocationCondition => [],
            ScenarioEventStackToCityCondition => [],
            ScenarioEventItemToLocationCondition itemToLocation => [itemToLocation.Item],
            ScenarioEventStackExistenceCondition => [],
            ScenarioEventVariableInRangeCondition => []
        };
    }

    private static ImmutableArray<WorldAiTarget> GetConditionTargets(ScenarioEventCondition condition, WorldState state)
    {
        return condition switch
        {
            ScenarioEventFrequencyCondition => [],
            ScenarioEventEnteringZoneCondition => [],
            ScenarioEventEnteringCityCondition enteringCity => ForFortification(enteringCity.Fortification, state),
            ScenarioEventOwningCityCondition owningCity => ForFortification(owningCity.Fortification, state),
            ScenarioEventDestroyingStackCondition destroyingStack => ForStacks(destroyingStack.Stack, state),
            ScenarioEventOwningItemCondition owningItem => ForItem(owningItem.Item, state),
            ScenarioEventLeaderOwningItemCondition leaderOwningItem => [.. ForStacks(leaderOwningItem.Stack, state), .. ForItem(leaderOwningItem.Item, state)],
            ScenarioEventDiplomacyRelationsCondition => [],
            ScenarioEventAllianceCondition => [],
            ScenarioEventLootingRuinCondition lootingRuin => ForFortification(lootingRuin.Ruin, state),
            ScenarioEventTransformingLandCondition => [],
            ScenarioEventVisitingSiteCondition => [],
            ScenarioEventStackToLocationCondition stackToLocation => ForStacks(stackToLocation.Stack, state),
            ScenarioEventStackToCityCondition stackToCity => [.. ForStacks(stackToCity.Stack, state), .. ForFortification(stackToCity.Fortification, state)],
            ScenarioEventItemToLocationCondition itemToLocation => ForItem(itemToLocation.Item, state),
            ScenarioEventStackExistenceCondition stackExistence => ForStacks(stackExistence.Stack, state),
            ScenarioEventVariableInRangeCondition => []
        };
    }

    private static ImmutableArray<WorldAiTarget> ForFortification(WorldFortificationId id, WorldState state)
    {
        if (state.Fortifications.ContainsKey(id))
        {
            return [new WorldAiTarget.Fortification(id)];
        }
        else
        {
            return [];
        }
    }

    private static ImmutableArray<WorldAiTarget> ForStacks(ScenarioStackReference reference, WorldState state)
    {
        ImmutableArray<WorldAiTarget>.Builder result = ImmutableArray.CreateBuilder<WorldAiTarget>();

        foreach (WorldStackId stackId in ScenarioStackReferenceCalculator.GetLiveInstances(reference, state))
        {
            result.Add(new WorldAiTarget.Stack(stackId));
        }

        return result.ToImmutable();
    }

    private static ImmutableArray<WorldAiTarget> ForItem(ItemDefinitionId id, WorldState state)
    {
        ImmutableArray<WorldAiTarget>.Builder result = ImmutableArray.CreateBuilder<WorldAiTarget>();

        foreach (WorldFortification fortification in state.Fortifications.Values)
        {
            if (ContainsItem(fortification, id))
            {
                result.Add(new WorldAiTarget.Fortification(fortification.Id));
            }
        }

        return result.ToImmutable();
    }

    private static bool ContainsItem(WorldFortification fortification, ItemDefinitionId id)
    {
        foreach (WorldItem item in fortification.Inventory.Items)
        {
            if (item.DefinitionId == id)
            {
                return true;
            }
        }

        return false;
    }
}
