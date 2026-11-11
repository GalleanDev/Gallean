namespace Gallean.Engine.Contracts;

public closed record WorldDiplomacyAction
{
    public abstract TResult Accept<TContext, TResult>(IWorldDiplomacyActionVisitor<TContext, TResult> visitor, TContext context);
}
