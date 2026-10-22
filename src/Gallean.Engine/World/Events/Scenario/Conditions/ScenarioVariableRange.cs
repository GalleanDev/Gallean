namespace Gallean.Engine;

public sealed record ScenarioVariableRange(
    ScenarioVariableId Variable,
    int Minimum,
    int Maximum);
