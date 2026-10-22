namespace Gallean.Engine;

public interface IWorldThiefActionVisitor<in TContext, out TResult>
{
    TResult Visit(WorldThiefPoisonAction action, TContext context);
    TResult Visit(WorldThiefSpyAction action, TContext context);
    TResult Visit(WorldThiefStealItemAction action, TContext context);
    TResult Visit(WorldThiefAssassinateAction action, TContext context);
    TResult Visit(WorldThiefCounterfeitOrdersAction action, TContext context);
    TResult Visit(WorldThiefDuelAction action, TContext context);
    TResult Visit(WorldThiefStealSpellAction action, TContext context);
    TResult Visit(WorldThiefBribeAction action, TContext context);
    TResult Visit(WorldThiefStealGoldAction action, TContext context);
    TResult Visit(WorldThiefSpyRuinAction action, TContext context);
    TResult Visit(WorldThiefRiotAction action, TContext context);
}
