using System.Collections.Immutable;

namespace Gallean.Engine;

internal static partial class ObjectiveCalculator
{
    private sealed class TargetVisitor : IScenarioEventConditionVisitor<Context, ImmutableArray<WorldObjective>>
    {
        public ImmutableArray<WorldObjective> Visit(ScenarioEventFrequencyCondition condition, Context context)
        {
            return [];
        }

        public ImmutableArray<WorldObjective> Visit(ScenarioEventEnteringZoneCondition condition, Context context)
        {
            return ForZone(condition.Zone, context);
        }

        public ImmutableArray<WorldObjective> Visit(ScenarioEventEnteringCityCondition condition, Context context)
        {
            return ForFortification(condition.Fortification, context);
        }

        public ImmutableArray<WorldObjective> Visit(ScenarioEventOwningCityCondition condition, Context context)
        {
            return ForFortification(condition.Fortification, context);
        }

        public ImmutableArray<WorldObjective> Visit(ScenarioEventDestroyingStackCondition condition, Context context)
        {
            return ForStacks(condition.Stack, context);
        }

        public ImmutableArray<WorldObjective> Visit(ScenarioEventOwningItemCondition condition, Context context)
        {
            return [];
        }

        public ImmutableArray<WorldObjective> Visit(ScenarioEventLeaderOwningItemCondition condition, Context context)
        {
            return ForStacks(condition.Stack, context);
        }

        public ImmutableArray<WorldObjective> Visit(ScenarioEventDiplomacyRelationsCondition condition, Context context)
        {
            return [];
        }

        public ImmutableArray<WorldObjective> Visit(ScenarioEventAllianceCondition condition, Context context)
        {
            return [];
        }

        public ImmutableArray<WorldObjective> Visit(ScenarioEventLootingRuinCondition condition, Context context)
        {
            return ForFortification(condition.Ruin, context);
        }

        public ImmutableArray<WorldObjective> Visit(ScenarioEventTransformingLandCondition condition, Context context)
        {
            return [];
        }

        public ImmutableArray<WorldObjective> Visit(ScenarioEventVisitingSiteCondition condition, Context context)
        {
            return ForSite(condition.Site, context);
        }

        public ImmutableArray<WorldObjective> Visit(ScenarioEventStackToLocationCondition condition, Context context)
        {
            return [.. ForStacks(condition.Stack, context), .. ForZone(condition.Location, context)];
        }

        public ImmutableArray<WorldObjective> Visit(ScenarioEventStackToCityCondition condition, Context context)
        {
            return [.. ForStacks(condition.Stack, context), .. ForFortification(condition.Fortification, context)];
        }

        public ImmutableArray<WorldObjective> Visit(ScenarioEventItemToLocationCondition condition, Context context)
        {
            return ForZone(condition.Location, context);
        }

        public ImmutableArray<WorldObjective> Visit(ScenarioEventStackExistenceCondition condition, Context context)
        {
            return ForStacks(condition.Stack, context);
        }

        public ImmutableArray<WorldObjective> Visit(ScenarioEventVariableInRangeCondition condition, Context context)
        {
            return [];
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
}
