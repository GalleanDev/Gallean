using System.Collections.Immutable;
using Gallean.Common;

namespace Gallean.Engine;

internal sealed partial class WorldAiScenario
{
    private sealed class ItemVisitor : IScenarioEventConditionVisitor<Empty, ImmutableArray<ItemDefinitionId>>, IStaticInstance<ItemVisitor>
    {
        public ImmutableArray<ItemDefinitionId> Visit(ScenarioEventFrequencyCondition condition, Empty context)
        {
            return [];
        }

        public ImmutableArray<ItemDefinitionId> Visit(ScenarioEventEnteringZoneCondition condition, Empty context)
        {
            return [];
        }

        public ImmutableArray<ItemDefinitionId> Visit(ScenarioEventEnteringCityCondition condition, Empty context)
        {
            return [];
        }

        public ImmutableArray<ItemDefinitionId> Visit(ScenarioEventOwningCityCondition condition, Empty context)
        {
            return [];
        }

        public ImmutableArray<ItemDefinitionId> Visit(ScenarioEventDestroyingStackCondition condition, Empty context)
        {
            return [];
        }

        public ImmutableArray<ItemDefinitionId> Visit(ScenarioEventOwningItemCondition condition, Empty context)
        {
            return [condition.Item];
        }

        public ImmutableArray<ItemDefinitionId> Visit(ScenarioEventLeaderOwningItemCondition condition, Empty context)
        {
            return [condition.Item];
        }

        public ImmutableArray<ItemDefinitionId> Visit(ScenarioEventDiplomacyRelationsCondition condition, Empty context)
        {
            return [];
        }

        public ImmutableArray<ItemDefinitionId> Visit(ScenarioEventAllianceCondition condition, Empty context)
        {
            return [];
        }

        public ImmutableArray<ItemDefinitionId> Visit(ScenarioEventLootingRuinCondition condition, Empty context)
        {
            return [];
        }

        public ImmutableArray<ItemDefinitionId> Visit(ScenarioEventTransformingLandCondition condition, Empty context)
        {
            return [];
        }

        public ImmutableArray<ItemDefinitionId> Visit(ScenarioEventVisitingSiteCondition condition, Empty context)
        {
            return [];
        }

        public ImmutableArray<ItemDefinitionId> Visit(ScenarioEventStackToLocationCondition condition, Empty context)
        {
            return [];
        }

        public ImmutableArray<ItemDefinitionId> Visit(ScenarioEventStackToCityCondition condition, Empty context)
        {
            return [];
        }

        public ImmutableArray<ItemDefinitionId> Visit(ScenarioEventItemToLocationCondition condition, Empty context)
        {
            return [condition.Item];
        }

        public ImmutableArray<ItemDefinitionId> Visit(ScenarioEventStackExistenceCondition condition, Empty context)
        {
            return [];
        }

        public ImmutableArray<ItemDefinitionId> Visit(ScenarioEventVariableInRangeCondition condition, Empty context)
        {
            return [];
        }
    }
}
