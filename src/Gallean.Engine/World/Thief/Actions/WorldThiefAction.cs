namespace Gallean.Engine;

public abstract record WorldThiefAction
{
    public abstract TResult Accept<TContext, TResult>(IWorldThiefActionVisitor<TContext, TResult> visitor, TContext context);
}
