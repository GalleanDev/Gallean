namespace Gallean.Client;

internal readonly record struct InventorySlotReference(InventoryViewModel Inventory, int Index, int Count) : IItemDragSourceReference;
