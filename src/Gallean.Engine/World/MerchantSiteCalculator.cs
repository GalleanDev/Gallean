namespace Gallean.Engine;

internal static class MerchantSiteCalculator
{
    public sealed record PurchaseInfo(WorldResourceSet Cost, WorldCommandError? Error);

    public sealed record SellInfo(WorldResourceSet Value, WorldCommandError? Error);

    public static PurchaseInfo GetPurchaseInfo(
        WorldMerchantSite site,
        WorldStack stack,
        WorldPlayer player,
        ItemDefinitionId itemId,
        IEngineContext context)
    {
        if (!site.Contains(itemId))
        {
            return new PurchaseInfo(WorldResourceSet.Empty, WorldCommandError.ItemNotAvailable);
        }

        ItemDefinition definition = context.Definitions.GetItem(itemId);
        WorldResourceSet value = GetItemValue(definition, context.Globals.TalismanCharges);
        WorldResourceSet cost = PurchaseCalculator.GetPurchaseCost(value, stack, context);

        if (!ResourceSetCalculator.CanAfford(player.Resources, cost))
        {
            return new PurchaseInfo(cost, WorldCommandError.InsufficientResources);
        }

        return new PurchaseInfo(cost, Error: null);
    }

    public static SellInfo GetSellInfo(WorldItem item, IEngineContext context)
    {
        ItemDefinition definition = context.Definitions.GetItem(item.DefinitionId);
        WorldResourceSet itemValue = GetItemValue(definition, item.Charges ?? context.Globals.TalismanCharges);
        WorldResourceSet value = ResourceSetCalculator.ScalePercent(itemValue, context.Globals.SellItemPercent);

        if (!CanSellItem(definition.Category))
        {
            return new SellInfo(value, WorldCommandError.ItemNotSellable);
        }

        return new SellInfo(value, Error: null);
    }

    private static bool CanSellItem(ItemCategory category)
    {
        return category switch
        {
            ItemCategory.Armor => true,
            ItemCategory.Jewel => true,
            ItemCategory.Weapon => true,
            ItemCategory.Banner => true,
            ItemCategory.PotionTemporaryBoost => true,
            ItemCategory.PotionHeal => true,
            ItemCategory.PotionRevive => true,
            ItemCategory.PotionPermanentBoost => true,
            ItemCategory.Scroll => true,
            ItemCategory.Wand => true,
            ItemCategory.Valuable => true,
            ItemCategory.Orb => true,
            ItemCategory.Talisman => true,
            ItemCategory.TravelItem => true,
            ItemCategory.Special => false
        };
    }

    private static WorldResourceSet GetItemValue(ItemDefinition definition, int charges)
    {
        if (definition.Category == ItemCategory.Talisman)
        {
            return definition.Value with { Gold = definition.Value.Gold * charges };
        }
        else
        {
            return definition.Value;
        }
    }
}
