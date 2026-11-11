namespace Gallean.Engine;

public closed record WorldEvent
{
    public abstract TResult Accept<TContext, TResult>(IWorldEventVisitor<TContext, TResult> visitor, TContext context);
}
