namespace Gallean.Engine;

public sealed record ScenarioLocationDefinition(
    ScenarioLocationId Id,
    ScenarioTextId NameTextId,
    WorldPosition Position,
    int Radius);
