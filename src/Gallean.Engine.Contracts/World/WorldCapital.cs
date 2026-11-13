namespace Gallean.Engine.Contracts;

public sealed record WorldCapital(
    WorldFortificationId Id,
    ScenarioTextId NameTextId,
    WorldPlayerId OwnerId,
    WorldFootprint Footprint,
    UnitGrid<WorldUnit> Garrison,
    WorldInventory Inventory,
    int AiPriority,
    WorldPlayerId? CapturedById) : WorldFortification(Id, NameTextId, Footprint, Garrison, Inventory, AiPriority);
