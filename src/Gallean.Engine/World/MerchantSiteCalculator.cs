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
        WorldResourceSet value = GetItemValue(definition, item.Charges ?? context.Globals.TalismanCharges);

        return new SellInfo(ResourceSetCalculator.ScalePercent(value, context.Globals.SellItemPercent), Error: null);
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
