namespace Gallean.Engine;

public sealed record ScenarioVariableClause(
    ScenarioVariableOperator Operator,
    ScenarioVariableRange Range);
