namespace Gallean.Engine.Contracts;

public abstract record WorldDiplomacyAction
{
    public abstract TResult Accept<TContext, TResult>(IWorldDiplomacyActionVisitor<TContext, TResult> visitor, TContext context);
}
