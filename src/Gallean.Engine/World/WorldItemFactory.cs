namespace Gallean.Engine;

internal static class WorldItemFactory
{
    public static WorldItem Create(WorldItemId id, ItemDefinition definition, GlobalVariables globals)
    {
        return new WorldItem(id, definition.Id, GetInitialCharges(definition.Category, globals));
    }

    private static int? GetInitialCharges(ItemCategory category, GlobalVariables globals)
    {
        return category switch
        {
            ItemCategory.Talisman => globals.TalismanCharges,
            ItemCategory.Armor => null,
            ItemCategory.Jewel => null,
            ItemCategory.Weapon => null,
            ItemCategory.Banner => null,
            ItemCategory.PotionTemporaryBoost => null,
            ItemCategory.PotionHeal => null,
            ItemCategory.PotionRevive => null,
            ItemCategory.PotionPermanentBoost => null,
            ItemCategory.Scroll => null,
            ItemCategory.Wand => null,
            ItemCategory.Valuable => null,
            ItemCategory.Orb => null,
            ItemCategory.TravelItem => null,
            ItemCategory.Special => null
        };
    }
}
