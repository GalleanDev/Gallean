namespace Gallean.Engine.Contracts;

public sealed record BattleItem(
    BattleSide Side,
    LeaderEquipmentSlot Slot,
    ItemDefinitionId ItemId,
    int? Charges,
    bool Used);
