namespace Gallean.Engine;

public sealed record ScenarioEventLeaderOwningItemCondition(ScenarioStackReference Stack, ItemDefinitionId Item) : ScenarioEventCondition;
