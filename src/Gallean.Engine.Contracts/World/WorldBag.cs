namespace Gallean.Engine.Contracts;

public sealed record WorldBag(
    WorldBagId Id,
    WorldPosition Position,
    WorldObjectVariant Variant,
    WorldInventory Inventory,
    int AiPriority);
