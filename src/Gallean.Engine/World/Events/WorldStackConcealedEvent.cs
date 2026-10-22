namespace Gallean.Engine;

public sealed record WorldStackConcealedEvent(WorldStackId StackId) : WorldEvent
{
    public override TResult Accept<TContext, TResult>(IWorldEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
