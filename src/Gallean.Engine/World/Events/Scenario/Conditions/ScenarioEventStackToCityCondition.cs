namespace Gallean.Engine;

public sealed record ScenarioEventStackToCityCondition(ScenarioStackReference Stack, WorldFortificationId Fortification) : ScenarioEventCondition;
