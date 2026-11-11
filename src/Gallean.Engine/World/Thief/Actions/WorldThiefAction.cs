namespace Gallean.Engine;

public closed record WorldThiefAction
{
    public abstract TResult Accept<TContext, TResult>(IWorldThiefActionVisitor<TContext, TResult> visitor, TContext context);
}
