using Gallean.Common;

namespace Gallean.Engine;

internal static class WorldThiefActionResolverFactory
{
    private sealed class ActionVisitor :
        IWorldThiefActionVisitor<Empty, IWorldThiefActionResolver>,
        IStaticInstance<ActionVisitor>
    {
        public IWorldThiefActionResolver Visit(WorldThiefPoisonAction action, Empty context) => new PoisonThiefActionResolver(action);
        public IWorldThiefActionResolver Visit(WorldThiefSpyAction action, Empty context) => new SpyThiefActionResolver(action);
        public IWorldThiefActionResolver Visit(WorldThiefStealItemAction action, Empty context) => new StealItemThiefActionResolver(action);
        public IWorldThiefActionResolver Visit(WorldThiefAssassinateAction action, Empty context) => new AssassinateThiefActionResolver(action);
        public IWorldThiefActionResolver Visit(WorldThiefCounterfeitOrdersAction action, Empty context) => new CounterfeitOrdersThiefActionResolver(action);
        public IWorldThiefActionResolver Visit(WorldThiefDuelAction action, Empty context) => new DuelThiefActionResolver(action);
        public IWorldThiefActionResolver Visit(WorldThiefStealSpellAction action, Empty context) => new StealSpellThiefActionResolver(action);
        public IWorldThiefActionResolver Visit(WorldThiefBribeAction action, Empty context) => new BribeThiefActionResolver(action);
        public IWorldThiefActionResolver Visit(WorldThiefStealGoldAction action, Empty context) => new StealGoldThiefActionResolver(action);
        public IWorldThiefActionResolver Visit(WorldThiefSpyRuinAction action, Empty context) => new SpyRuinThiefActionResolver(action);
        public IWorldThiefActionResolver Visit(WorldThiefRiotAction action, Empty context) => new RiotThiefActionResolver(action);
    }

    public static IWorldThiefActionResolver Create(WorldThiefAction action)
    {
        return action.Accept(ActionVisitor.Instance, Empty.Value);
    }
}
