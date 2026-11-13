namespace Gallean.Engine;

public sealed record ScenarioEventStackExistenceCondition(ScenarioStackReference Stack, bool MustExist) : ScenarioEventCondition;
