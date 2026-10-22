namespace Gallean.Engine;

internal static class SpellCalculator
{
    public sealed record CastInfo(WorldResourceSet Cost, WorldCommandError? Error);

    public static CastInfo GetSpellCastInfo(WorldSpellDefinitionId spellId, WorldPlayer player, IEngineContext context)
    {
        if (!player.Spellbook.Acquired.Contains(spellId))
        {
            return new CastInfo(WorldResourceSet.Empty, WorldCommandError.SpellNotKnown);
        }

        WorldSpellDefinition definition = context.Definitions.GetSpell(spellId);
        LordDefinition lord = context.Definitions.GetLord(player.LordId);
        int castsThisTurn = player.TurnActions.SpellCasts.GetValueOrDefault(spellId);

        if (castsThisTurn >= GetCastsPerTurn(lord.Category, context.Globals))
        {
            return new CastInfo(definition.CastingCost, WorldCommandError.SpellAlreadyCastThisTurn);
        }

        if (!ResourceSetCalculator.CanAfford(player.Resources, definition.CastingCost))
        {
            return new CastInfo(definition.CastingCost, WorldCommandError.InsufficientResources);
        }

        return new CastInfo(definition.CastingCost, Error: null);
    }

    public static CastInfo GetItemCastInfo(WorldItem item, WorldPlayer player, IEngineContext context)
    {
        ItemDefinition definition = context.Definitions.GetItem(item.DefinitionId);

        if (definition.Effect is not SpellItemEffect effect)
        {
            return new CastInfo(WorldResourceSet.Empty, WorldCommandError.ItemNotUsable);
        }

        LordDefinition lord = context.Definitions.GetLord(player.LordId);
        int usesThisTurn = player.TurnActions.ItemUses.GetValueOrDefault(item.Id);

        if (usesThisTurn >= GetCastsPerTurn(lord.Category, context.Globals))
        {
            return new CastInfo(effect.CastingCost, WorldCommandError.ItemAlreadyUsedThisTurn);
        }

        if (!ResourceSetCalculator.CanAfford(player.Resources, effect.CastingCost))
        {
            return new CastInfo(effect.CastingCost, WorldCommandError.InsufficientResources);
        }

        return new CastInfo(effect.CastingCost, Error: null);
    }

    public static WorldResourceSet GetResearchCost(WorldResourceSet listPrice, LordCategory category, GlobalVariables globals)
    {
        int discountPercent = GetDiscountPercent(category, globals);

        return new WorldResourceSet(
            Discount(listPrice.Gold, discountPercent),
            Discount(listPrice.DeathMana, discountPercent),
            Discount(listPrice.RuneMana, discountPercent),
            Discount(listPrice.LifeMana, discountPercent),
            Discount(listPrice.InfernalMana, discountPercent),
            Discount(listPrice.GroveMana, discountPercent));
    }

    private static int GetCastsPerTurn(LordCategory category, GlobalVariables globals)
    {
        return category switch
        {
            LordCategory.Warrior => globals.LordWarriorSpellCastsPerTurn,
            LordCategory.Mage => globals.LordMageSpellCastsPerTurn,
            LordCategory.Guildmaster => globals.LordGuildmasterSpellCastsPerTurn
        };
    }

    private static int GetDiscountPercent(LordCategory category, GlobalVariables globals)
    {
        return category switch
        {
            LordCategory.Warrior => 0,
            LordCategory.Mage => globals.LordMageSpellResearchDiscountPercent,
            LordCategory.Guildmaster => 0
        };
    }

    private static int Discount(int amount, int discountPercent)
    {
        return amount - (amount * discountPercent / 100);
    }
}
