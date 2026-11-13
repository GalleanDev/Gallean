namespace Gallean.Engine;

public sealed record ScenarioEventVariableInRangeCondition(
    ScenarioVariableRange Range,
    ScenarioVariableClause? Second) : ScenarioEventCondition;
