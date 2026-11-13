using Gallean.Common;

namespace Gallean.Engine;

internal static class WorldDiplomacyActionResolverFactory
{
    public static IWorldDiplomacyActionResolver Create(WorldDiplomacyAction action)
    {
        return action switch
        {
            WorldDiplomacyTradeItemAction tradeItem => new ProposeDiplomacyActionResolver(tradeItem),
            WorldDiplomacyTradeSpellAction tradeSpell => new ProposeDiplomacyActionResolver(tradeSpell),
            WorldDiplomacyGiveGoldAction giveGold => new GiveGoldDiplomacyActionResolver(giveGold),
            WorldDiplomacyProposeAllianceAction proposeAlliance => new ProposeDiplomacyActionResolver(proposeAlliance),
            WorldDiplomacyBreakAllianceAction => BreakAllianceDiplomacyActionResolver.Instance,
            WorldDiplomacyExchangeMapAction exchangeMap => new ProposeDiplomacyActionResolver(exchangeMap)
        };
    }
}
