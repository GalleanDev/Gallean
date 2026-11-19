using Gallean.Engine;

namespace Gallean.Client;

internal static class InventoryItemTypeResolver
{
    public static InventoryItemType Resolve(ItemCategory category)
    {
        return category switch
        {
            ItemCategory.Armor => InventoryItemType.Equipment,
            ItemCategory.Jewel => InventoryItemType.Equipment,
            ItemCategory.Weapon => InventoryItemType.Equipment,
            ItemCategory.Banner => InventoryItemType.Equipment,
            ItemCategory.PotionTemporaryBoost => InventoryItemType.Consumable,
            ItemCategory.PotionHeal => InventoryItemType.Consumable,
            ItemCategory.PotionRevive => InventoryItemType.Consumable,
            ItemCategory.PotionPermanentBoost => InventoryItemType.Consumable,
            ItemCategory.Scroll => InventoryItemType.Consumable,
            ItemCategory.Wand => InventoryItemType.Equipment,
            ItemCategory.Valuable => InventoryItemType.Valuable,
            ItemCategory.Orb => InventoryItemType.Consumable,
            ItemCategory.Talisman => InventoryItemType.Equipment,
            ItemCategory.TravelItem => InventoryItemType.Equipment,
            ItemCategory.Special => InventoryItemType.Special
        };
    }
}
