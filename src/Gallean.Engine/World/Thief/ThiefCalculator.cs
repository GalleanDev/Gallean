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

    public static bool IsActionAllowed(WorldThiefActionType type, LordDefinition lord)
    {
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
        return target.Accept(ReachVisitor.Instance, thief);
    }

    public static ImmutableArray<WorldThiefActionType> GetActionTypes(ThiefTarget target)
    {
        return target.Accept(ActionTypesVisitor.Instance, Empty.Value);
    }

    public static ImmutableArray<ItemDefinitionId> GetStealableItems(ThiefTarget target)
    {
        return target.Accept(StealableItemsVisitor.Instance, Empty.Value);
    }

    public static ImmutableArray<WorldSpellDefinitionId> GetStealableSpells(ThiefTarget target, WorldState state)
    {
        return target.Accept(StealableSpellsVisitor.Instance, state);
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
