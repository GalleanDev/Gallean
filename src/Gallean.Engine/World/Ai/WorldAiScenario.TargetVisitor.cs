using System.Collections.Immutable;
using Gallean.Common;

namespace Gallean.Engine;

internal sealed partial class WorldAiScenario
{
    private sealed class TargetVisitor : IScenarioEventConditionVisitor<WorldState, ImmutableArray<WorldAiTarget>>, IStaticInstance<TargetVisitor>
    {
        public ImmutableArray<WorldAiTarget> Visit(ScenarioEventFrequencyCondition condition, WorldState context)
        {
            return [];
        }

        public ImmutableArray<WorldAiTarget> Visit(ScenarioEventEnteringZoneCondition condition, WorldState context)
        {
            return [];
        }

        public ImmutableArray<WorldAiTarget> Visit(ScenarioEventEnteringCityCondition condition, WorldState context)
        {
            return ForFortification(condition.Fortification, context);
        }

        public ImmutableArray<WorldAiTarget> Visit(ScenarioEventOwningCityCondition condition, WorldState context)
        {
            return ForFortification(condition.Fortification, context);
        }

        public ImmutableArray<WorldAiTarget> Visit(ScenarioEventDestroyingStackCondition condition, WorldState context)
        {
            return ForStacks(condition.Stack, context);
        }

        public ImmutableArray<WorldAiTarget> Visit(ScenarioEventOwningItemCondition condition, WorldState context)
        {
            return ForItem(condition.Item, context);
        }

        public ImmutableArray<WorldAiTarget> Visit(ScenarioEventLeaderOwningItemCondition condition, WorldState context)
        {
            return [.. ForStacks(condition.Stack, context), .. ForItem(condition.Item, context)];
        }

        public ImmutableArray<WorldAiTarget> Visit(ScenarioEventDiplomacyRelationsCondition condition, WorldState context)
        {
            return [];
        }

        public ImmutableArray<WorldAiTarget> Visit(ScenarioEventAllianceCondition condition, WorldState context)
        {
            return [];
        }

        public ImmutableArray<WorldAiTarget> Visit(ScenarioEventLootingRuinCondition condition, WorldState context)
        {
            return ForFortification(condition.Ruin, context);
        }

        public ImmutableArray<WorldAiTarget> Visit(ScenarioEventTransformingLandCondition condition, WorldState context)
        {
            return [];
        }

        public ImmutableArray<WorldAiTarget> Visit(ScenarioEventVisitingSiteCondition condition, WorldState context)
        {
            return [];
        }

        public ImmutableArray<WorldAiTarget> Visit(ScenarioEventStackToLocationCondition condition, WorldState context)
        {
            return ForStacks(condition.Stack, context);
        }

        public ImmutableArray<WorldAiTarget> Visit(ScenarioEventStackToCityCondition condition, WorldState context)
        {
            return [.. ForStacks(condition.Stack, context), .. ForFortification(condition.Fortification, context)];
        }

        public ImmutableArray<WorldAiTarget> Visit(ScenarioEventItemToLocationCondition condition, WorldState context)
        {
            return ForItem(condition.Item, context);
        }

        public ImmutableArray<WorldAiTarget> Visit(ScenarioEventStackExistenceCondition condition, WorldState context)
        {
            return ForStacks(condition.Stack, context);
        }

        public ImmutableArray<WorldAiTarget> Visit(ScenarioEventVariableInRangeCondition condition, WorldState context)
        {
            return [];
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
}
