using System.Collections.Immutable;
using Gallean.Common;

namespace Gallean.Engine;

internal static partial class ThiefCalculator
{
    private const int ChanceMax = 100;

    public static bool IsThief(WorldStack stack, IGameDefinitionProvider definitions)
    {
        (_, _, UnitDefinition leaderDefinition) = stack.GetLeader(definitions);

        return leaderDefinition.LeaderCategory == UnitLeaderCategory.Thief;
    }

    public static bool IsActionAllowed(WorldThiefActionType type, WorldPlayer player, LordDefinition lord)
    {
        if (player.IsAi)
        {
            return true;
        }

        return type switch
        {
            WorldThiefActionType.PoisonStack => true,
            WorldThiefActionType.Spy => true,
            WorldThiefActionType.StealItem => lord.Category == LordCategory.Guildmaster,
            WorldThiefActionType.Assassinate => lord.Category == LordCategory.Guildmaster,
            WorldThiefActionType.CounterfeitOrders => lord.Category == LordCategory.Guildmaster,
            WorldThiefActionType.Duel => true,
            WorldThiefActionType.PoisonCity => true,
            WorldThiefActionType.StealSpell => lord.Category == LordCategory.Guildmaster,
            WorldThiefActionType.Bribe => lord.Category == LordCategory.Guildmaster,
            WorldThiefActionType.StealGold => lord.Category == LordCategory.Guildmaster,
            WorldThiefActionType.StealMerchant => true,
            WorldThiefActionType.StealMage => true,
            WorldThiefActionType.SpyRuin => true,
            WorldThiefActionType.RiotCity => lord.Category == LordCategory.Guildmaster
        };
    }

    public static bool CanReachTarget(WorldStack thief, ThiefTarget target)
    {
        return target switch
        {
            ThiefStackTarget stackTarget => thief.Position.IsAdjacentTo(stackTarget.Stack.Position),
            ThiefFortificationTarget fortificationTarget => thief.Position.IsAdjacentTo(fortificationTarget.Fortification.Entrance),
            ThiefMerchantSiteTarget merchantTarget => thief.Position.IsAdjacentTo(merchantTarget.Site.Entrance),
            ThiefMageSiteTarget mageTarget => thief.Position.IsAdjacentTo(mageTarget.Site.Entrance),
            ThiefRuinTarget ruinTarget => thief.Position.IsAdjacentTo(ruinTarget.Ruin.Entrance)
        };
    }

    public static WorldUnitGroup? GetUnitGroup(ThiefTarget target)
    {
        return target switch
        {
            ThiefStackTarget stackTarget => new WorldUnitGroup.Stack(stackTarget.Stack.Id),
            ThiefFortificationTarget fortificationTarget => new WorldUnitGroup.Fortification(fortificationTarget.Fortification.Id),
            ThiefMerchantSiteTarget => null,
            ThiefMageSiteTarget => null,
            ThiefRuinTarget => null
        };
    }

    public static ImmutableArray<WorldThiefActionType> GetActionTypes(ThiefTarget target)
    {
        return target switch
        {
            ThiefStackTarget => GetStackActionTypes(),
            ThiefFortificationTarget fortificationTarget => GetFortificationActionTypes(fortificationTarget.Fortification),
            ThiefMerchantSiteTarget => [WorldThiefActionType.StealMerchant],
            ThiefMageSiteTarget => [WorldThiefActionType.StealMage],
            ThiefRuinTarget => [WorldThiefActionType.SpyRuin]
        };
    }

    private static ImmutableArray<WorldThiefActionType> GetStackActionTypes()
    {
        return
        [
            WorldThiefActionType.PoisonStack,
            WorldThiefActionType.Spy,
            WorldThiefActionType.StealItem,
            WorldThiefActionType.Assassinate,
            WorldThiefActionType.CounterfeitOrders,
            WorldThiefActionType.Duel
        ];
    }

    private static ImmutableArray<WorldThiefActionType> GetFortificationActionTypes(WorldFortification fortification)
    {
        if (fortification is WorldCity)
        {
            return
            [
                WorldThiefActionType.Spy,
                WorldThiefActionType.PoisonCity,
                WorldThiefActionType.StealGold,
                WorldThiefActionType.StealSpell,
                WorldThiefActionType.Assassinate,
                WorldThiefActionType.CounterfeitOrders,
                WorldThiefActionType.Bribe,
                WorldThiefActionType.RiotCity
            ];
        }
        else
        {
            return
            [
                WorldThiefActionType.Spy,
                WorldThiefActionType.PoisonCity,
                WorldThiefActionType.StealGold,
                WorldThiefActionType.StealSpell,
                WorldThiefActionType.CounterfeitOrders
            ];
        }
    }

    public static ImmutableArray<ItemDefinitionId> GetStealableItems(ThiefTarget target)
    {
        return target switch
        {
            ThiefStackTarget stackTarget => GetStackItems(stackTarget.Stack),
            ThiefFortificationTarget => [],
            ThiefMerchantSiteTarget merchantTarget => GetMerchantItems(merchantTarget.Site),
            ThiefMageSiteTarget => [],
            ThiefRuinTarget => []
        };
    }

    private static ImmutableArray<ItemDefinitionId> GetStackItems(WorldStack stack)
    {
        ImmutableArray<WorldItem> inventoryItems = stack.Inventory.Items;

        return [.. inventoryItems.Select(item => item.DefinitionId)];
    }

    private static ImmutableArray<ItemDefinitionId> GetMerchantItems(WorldMerchantSite site)
    {
        ImmutableArray<WorldMerchantItem> siteItems = site.Items;

        return [.. siteItems.Select(item => item.ItemId)];
    }

    public static ImmutableArray<WorldSpellDefinitionId> GetStealableSpells(ThiefTarget target, WorldState state)
    {
        return target switch
        {
            ThiefStackTarget => [],
            ThiefFortificationTarget fortificationTarget => GetOwnerSpells(fortificationTarget.Owner, state),
            ThiefMerchantSiteTarget => [],
            ThiefMageSiteTarget mageTarget => mageTarget.Site.Spells,
            ThiefRuinTarget => []
        };
    }

    private static ImmutableArray<WorldSpellDefinitionId> GetOwnerSpells(WorldPlayerId ownerId, WorldState state)
    {
        WorldPlayer owner = state.Players[ownerId];

        return [.. owner.Spellbook.Acquired];
    }

    public static bool IsIncorruptible(WorldStack stack, IEngineContext context)
    {
        (WorldUnit leader, GridPosition _, UnitDefinition _) = stack.GetLeader(context.Definitions);
        LeaderAbility abilities = context.UnitStats.GetWorldUnitAbilities(leader);

        return leader.IsAlive && abilities.HasFlag(LeaderAbility.Incorruptible);
    }

    public static bool RollSuccess(IRandomProvider random, WorldThiefActionDefinition definition, int protection)
    {
        int chance = GetChance(definition, protection);
        int roll = (random.Next(ChanceMax) + random.Next(ChanceMax)) / 2;

        return roll <= chance;
    }

    public static int GetChance(WorldThiefActionDefinition definition, int protection)
    {
        if (definition.Chance < ChanceMax)
        {
            return Math.Max(0, definition.Chance - protection);
        }
        else
        {
            return definition.Chance;
        }
    }
}
