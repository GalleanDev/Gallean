namespace Gallean.Engine;

public sealed record ScenarioEventStackDestroyedTrigger(WorldUnitGroup? Destroyer) : ScenarioEventTrigger;
