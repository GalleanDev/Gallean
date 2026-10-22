using Gallean.Common;

namespace Gallean.Engine;

internal static class WorldDiplomacyActionResolverFactory
{
    private sealed class ActionVisitor :
        IWorldDiplomacyActionVisitor<Empty, IWorldDiplomacyActionResolver>,
        IStaticInstance<ActionVisitor>
    {
        public IWorldDiplomacyActionResolver Visit(WorldDiplomacyTradeItemAction action, Empty context) => new ProposeDiplomacyActionResolver(action);
        public IWorldDiplomacyActionResolver Visit(WorldDiplomacyTradeSpellAction action, Empty context) => new ProposeDiplomacyActionResolver(action);
        public IWorldDiplomacyActionResolver Visit(WorldDiplomacyGiveGoldAction action, Empty context) => new GiveGoldDiplomacyActionResolver(action);
        public IWorldDiplomacyActionResolver Visit(WorldDiplomacyProposeAllianceAction action, Empty context) => new ProposeDiplomacyActionResolver(action);
        public IWorldDiplomacyActionResolver Visit(WorldDiplomacyBreakAllianceAction action, Empty context) => BreakAllianceDiplomacyActionResolver.Instance;
        public IWorldDiplomacyActionResolver Visit(WorldDiplomacyExchangeMapAction action, Empty context) => new ProposeDiplomacyActionResolver(action);
    }

    public static IWorldDiplomacyActionResolver Create(WorldDiplomacyAction action)
    {
        return action.Accept(ActionVisitor.Instance, Empty.Value);
    }
}
