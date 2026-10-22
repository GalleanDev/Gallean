namespace Gallean.Engine;

public sealed record ScenarioVariableDefinition(
    ScenarioVariableId Id,
    string Name,
    int InitialValue);
