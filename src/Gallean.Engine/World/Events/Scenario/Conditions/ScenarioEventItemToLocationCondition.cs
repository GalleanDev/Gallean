namespace Gallean.Engine;

public sealed record ScenarioEventItemToLocationCondition(ItemDefinitionId Item, ScenarioLocationId Location) : ScenarioEventCondition;
