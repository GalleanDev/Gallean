namespace Gallean.Engine;

public abstract record WorldEvent
{
    public abstract TResult Accept<TContext, TResult>(IWorldEventVisitor<TContext, TResult> visitor, TContext context);
}
