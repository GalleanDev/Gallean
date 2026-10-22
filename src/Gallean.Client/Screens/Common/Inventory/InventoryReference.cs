namespace Gallean.Client;

internal readonly record struct InventoryReference(InventoryViewModel Inventory) : IDragTargetReference;
