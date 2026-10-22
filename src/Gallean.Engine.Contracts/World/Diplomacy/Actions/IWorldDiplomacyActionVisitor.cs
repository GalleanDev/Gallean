namespace Gallean.Engine.Contracts;

public interface IWorldDiplomacyActionVisitor<in TContext, out TResult>
{
    TResult Visit(WorldDiplomacyTradeItemAction action, TContext context);
    TResult Visit(WorldDiplomacyTradeSpellAction action, TContext context);
    TResult Visit(WorldDiplomacyGiveGoldAction action, TContext context);
    TResult Visit(WorldDiplomacyProposeAllianceAction action, TContext context);
    TResult Visit(WorldDiplomacyBreakAllianceAction action, TContext context);
    TResult Visit(WorldDiplomacyExchangeMapAction action, TContext context);
}
