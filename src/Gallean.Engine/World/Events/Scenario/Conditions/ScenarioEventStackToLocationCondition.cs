namespace Gallean.Engine;

public sealed record ScenarioEventStackToLocationCondition(ScenarioStackReference Stack, ScenarioLocationId Location) : ScenarioEventCondition;
