using Gallean.Common;

namespace Gallean.Engine;

internal static class CityCalculator
{
    public static int RollRiotTurns(IRandomProvider random, GlobalVariables globals)
    {
        return globals.RiotMinTurns + random.Next(globals.RiotMaxTurns - globals.RiotMinTurns + 1);
    }

    public static WorldResourceSet GetGrowthCost(WorldCityDefinition definition, WorldPlayer player, IEngineContext context)
    {
        LordDefinition lord = context.Definitions.GetLord(player.LordId);
        int discountPercent = GetDiscountPercent(lord.Category, context.Globals);
        WorldResourceSet cost = definition.UpgradeCost;

        return cost with { Gold = cost.Gold - (cost.Gold * discountPercent / 100) };
    }

    private static int GetDiscountPercent(LordCategory category, GlobalVariables globals)
    {
        return category switch
        {
            LordCategory.Warrior => 0,
            LordCategory.Mage => 0,
            LordCategory.Guildmaster => globals.LordGuildmasterCityGrowthDiscountPercent
        };
    }
}
