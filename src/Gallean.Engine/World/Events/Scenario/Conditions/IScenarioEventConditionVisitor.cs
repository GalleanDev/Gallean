namespace Gallean.Engine;

public interface IScenarioEventConditionVisitor<in TContext, out TResult>
{
    TResult Visit(ScenarioEventFrequencyCondition condition, TContext context);
    TResult Visit(ScenarioEventEnteringZoneCondition condition, TContext context);
    TResult Visit(ScenarioEventEnteringCityCondition condition, TContext context);
    TResult Visit(ScenarioEventOwningCityCondition condition, TContext context);
    TResult Visit(ScenarioEventDestroyingStackCondition condition, TContext context);
    TResult Visit(ScenarioEventOwningItemCondition condition, TContext context);
    TResult Visit(ScenarioEventLeaderOwningItemCondition condition, TContext context);
    TResult Visit(ScenarioEventDiplomacyRelationsCondition condition, TContext context);
    TResult Visit(ScenarioEventAllianceCondition condition, TContext context);
    TResult Visit(ScenarioEventLootingRuinCondition condition, TContext context);
    TResult Visit(ScenarioEventTransformingLandCondition condition, TContext context);
    TResult Visit(ScenarioEventVisitingSiteCondition condition, TContext context);
    TResult Visit(ScenarioEventStackToLocationCondition condition, TContext context);
    TResult Visit(ScenarioEventStackToCityCondition condition, TContext context);
    TResult Visit(ScenarioEventItemToLocationCondition condition, TContext context);
    TResult Visit(ScenarioEventStackExistenceCondition condition, TContext context);
    TResult Visit(ScenarioEventVariableInRangeCondition condition, TContext context);
}
