namespace Gallean.Engine;

internal static class MageSiteCalculator
{
    public sealed record PurchaseInfo(WorldResourceSet Cost, WorldCommandError? Error);

    public static PurchaseInfo GetPurchaseInfo(
        WorldMageSite site,
        WorldStack stack,
        WorldPlayer player,
        WorldSpellDefinitionId spellId,
        IEngineContext context)
    {
        if (!site.Spells.Contains(spellId))
        {
            return new PurchaseInfo(WorldResourceSet.Empty, WorldCommandError.SpellNotAvailable);
        }

        WorldSpellDefinition definition = context.Definitions.GetSpell(spellId);
        WorldResourceSet cost = PurchaseCalculator.GetPurchaseCost(definition.PurchaseCost, stack, context);

        if (player.Spellbook.Acquired.Contains(spellId))
        {
            return new PurchaseInfo(cost, WorldCommandError.SpellAlreadyKnown);
        }

        if (!ResourceSetCalculator.CanAfford(player.Resources, cost))
        {
            return new PurchaseInfo(cost, WorldCommandError.InsufficientResources);
        }

        return new PurchaseInfo(cost, Error: null);
    }
}
