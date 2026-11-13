using System.Collections.Immutable;

namespace Gallean.Engine;

internal static class ObjectiveCalculator
{
    private readonly record struct Context(WorldState State, IGameScenarioProvider Scenario, WorldPlayerId PlayerId);

    public static ImmutableArray<WorldObjective> GetObjectives(
        IGameScenarioProvider scenario,
        WorldState state,
        WorldPlayerId playerId)
    {
        if (!state.Players.TryGetValue(playerId, out WorldPlayer? player))
        {
            return [];
        }

        Context context = new(state, scenario, playerId);
        HashSet<WorldObjective> result = [];

        foreach (ScenarioEventDefinition definition in scenario.Events)
        {
            if (IsObjective(definition, state, player))
            {
                foreach (ScenarioEventCondition condition in definition.Conditions)
                {
                    foreach (WorldObjective objective in GetConditionObjectives(condition, context))
                    {
                        result.Add(objective);
                    }
                }
            }
        }

        return
        [
            .. result
                .OrderBy(objective => objective.Footprint.TopLeft.Y)
                .ThenBy(objective => objective.Footprint.TopLeft.X)
                .ThenBy(objective => objective.Footprint.Width)
                .ThenBy(objective => objective.Footprint.Height)
                .ThenBy(objective => objective.StackId)
        ];
    }

    public static int? GetRequiredLandPercentage(IGameScenarioProvider scenario, WorldState state, WorldPlayerId playerId)
    {
        WorldPlayer player = state.Players[playerId];

        foreach (ScenarioEventDefinition definition in scenario.Events)
        {
            if (IsObjective(definition, state, player))
            {
                foreach (ScenarioEventCondition condition in definition.Conditions)
                {
                    if (condition is ScenarioEventTransformingLandCondition landCondition)
                    {
                        return landCondition.LandPercentage;
                    }
                }
            }
        }

        return null;
    }

    private static bool IsObjective(ScenarioEventDefinition definition, WorldState state, WorldPlayer player)
    {
        if (!ScenarioEventCalculator.IsEnabled(state.ScenarioState, definition))
        {
            return false;
        }

        if (!definition.AffectedRaces.Contains(player.Race))
        {
            return false;
        }

        if (definition.Effects is [.., ScenarioEventWinEffect { WinnerId: { } winnerId }])
        {
            if (state.Players.TryGetValue(winnerId, out WorldPlayer? winner))
            {
                return winner.Race == player.Race;
            }
        }

        return false;
    }

    private static ImmutableArray<WorldObjective> GetConditionObjectives(ScenarioEventCondition condition, Context context)
    {
        return condition switch
        {
            ScenarioEventFrequencyCondition => [],
            ScenarioEventEnteringZoneCondition enteringZone => ForZone(enteringZone.Zone, context),
            ScenarioEventEnteringCityCondition enteringCity => ForFortification(enteringCity.Fortification, context),
            ScenarioEventOwningCityCondition owningCity => ForFortification(owningCity.Fortification, context),
            ScenarioEventDestroyingStackCondition destroyingStack => ForStacks(destroyingStack.Stack, context),
            ScenarioEventOwningItemCondition => [],
            ScenarioEventLeaderOwningItemCondition leaderOwningItem => ForStacks(leaderOwningItem.Stack, context),
            ScenarioEventDiplomacyRelationsCondition => [],
            ScenarioEventAllianceCondition => [],
            ScenarioEventLootingRuinCondition lootingRuin => ForFortification(lootingRuin.Ruin, context),
            ScenarioEventTransformingLandCondition => [],
            ScenarioEventVisitingSiteCondition visitingSite => ForSite(visitingSite.Site, context),
            ScenarioEventStackToLocationCondition stackToLocation => [.. ForStacks(stackToLocation.Stack, context), .. ForZone(stackToLocation.Location, context)],
            ScenarioEventStackToCityCondition stackToCity => [.. ForStacks(stackToCity.Stack, context), .. ForFortification(stackToCity.Fortification, context)],
            ScenarioEventItemToLocationCondition itemToLocation => ForZone(itemToLocation.Location, context),
            ScenarioEventStackExistenceCondition stackExistence => ForStacks(stackExistence.Stack, context),
            ScenarioEventVariableInRangeCondition => []
        };
    }

    private static ImmutableArray<WorldObjective> ForFortification(WorldFortificationId id, Context context)
    {
        if (!context.State.Fortifications.TryGetValue(id, out WorldFortification? fortification))
        {
            return [];
        }

        if (fortification is WorldRuin { LooterId: not null })
        {
            return [];
        }
        else
        {
            return [new WorldObjective(fortification.Footprint, StackId: null)];
        }
    }

    private static ImmutableArray<WorldObjective> ForSite(WorldSiteId id, Context context)
    {
        if (context.State.Sites.TryGetValue(id, out WorldSite? site))
        {
            return [new WorldObjective(site.Footprint, StackId: null)];
        }
        else
        {
            return [];
        }
    }

    private static ImmutableArray<WorldObjective> ForStacks(ScenarioStackReference reference, Context context)
    {
        WorldPlan plan = WorldPlan.For(context.State);

        ImmutableArray<WorldObjective>.Builder result = ImmutableArray.CreateBuilder<WorldObjective>();

        foreach (WorldStackId stackId in ScenarioStackReferenceCalculator.GetLiveInstances(reference, context.State))
        {
            WorldStack stack = context.State.Stacks[stackId];
            WorldFortification? fortification = plan.GetFortificationAt(stack.Position);

            if (stack.OwnerId == context.PlayerId)
            {
                if (fortification is null)
                {
                    result.Add(new WorldObjective(new WorldFootprint(stack.Position, 1, 1), stackId));
                }
            }
            else if (!stack.Concealed)
            {
                if (fortification is not null)
                {
                    result.Add(new WorldObjective(fortification.Footprint, stackId));
                }
                else
                {
                    result.Add(new WorldObjective(new WorldFootprint(stack.Position, 1, 1), stackId));
                }
            }
        }

        return result.ToImmutable();
    }

    private static ImmutableArray<WorldObjective> ForZone(ScenarioLocationId id, Context context)
    {
        ScenarioLocationDefinition location = context.Scenario.GetLocation(id);

        return [new WorldObjective(new WorldFootprint(location.Position, 1, 1), StackId: null)];
    }
}
