namespace Gallean.Engine;

internal static class PurchaseCalculator
{
    public static WorldResourceSet GetPurchaseCost(WorldResourceSet listPrice, WorldStack stack, IEngineContext context)
    {
        (WorldUnit leader, GridPosition _, UnitDefinition _) = stack.GetLeader(context.Definitions);

        int discountPercent = 0;

        foreach (UnitModifier modifier in leader.Modifiers)
        {
            discountPercent += context.Definitions.GetUnitModifier(modifier.DefinitionId).PurchaseDiscountPercent;
        }

        discountPercent = Math.Clamp(discountPercent, 0, context.Globals.MaxPurchaseDiscountPercent);

        return new WorldResourceSet(
            Discount(listPrice.Gold, discountPercent),
            Discount(listPrice.DeathMana, discountPercent),
            Discount(listPrice.RuneMana, discountPercent),
            Discount(listPrice.LifeMana, discountPercent),
            Discount(listPrice.InfernalMana, discountPercent),
            Discount(listPrice.GroveMana, discountPercent));
    }

    private static int Discount(int amount, int discountPercent)
    {
        return amount * (100 - discountPercent) / 100;
    }
}
