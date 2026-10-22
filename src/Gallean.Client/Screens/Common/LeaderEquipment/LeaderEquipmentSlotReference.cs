namespace Gallean.Client;

internal readonly record struct LeaderEquipmentSlotReference(
    LeaderEquipmentViewModel Equipment,
    LeaderEquipmentSlot Slot) : IItemDragSourceReference, IDragTargetReference;
