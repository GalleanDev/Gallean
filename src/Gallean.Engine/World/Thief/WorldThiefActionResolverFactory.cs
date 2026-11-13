namespace Gallean.Engine;

internal static class WorldThiefActionResolverFactory
{
    public static IWorldThiefActionResolver Create(WorldThiefAction action)
    {
        return action switch
        {
            WorldThiefPoisonAction poison => new PoisonThiefActionResolver(poison),
            WorldThiefSpyAction spy => new SpyThiefActionResolver(spy),
            WorldThiefStealItemAction stealItem => new StealItemThiefActionResolver(stealItem),
            WorldThiefAssassinateAction assassinate => new AssassinateThiefActionResolver(assassinate),
            WorldThiefCounterfeitOrdersAction counterfeitOrders => new CounterfeitOrdersThiefActionResolver(counterfeitOrders),
            WorldThiefDuelAction duel => new DuelThiefActionResolver(duel),
            WorldThiefStealSpellAction stealSpell => new StealSpellThiefActionResolver(stealSpell),
            WorldThiefBribeAction bribe => new BribeThiefActionResolver(bribe),
            WorldThiefStealGoldAction stealGold => new StealGoldThiefActionResolver(stealGold),
            WorldThiefSpyRuinAction spyRuin => new SpyRuinThiefActionResolver(spyRuin),
            WorldThiefRiotAction riot => new RiotThiefActionResolver(riot)
        };
    }
}
