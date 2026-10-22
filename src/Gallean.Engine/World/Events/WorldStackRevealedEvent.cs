namespace Gallean.Engine;

public sealed record WorldStackRevealedEvent(WorldStackId StackId) : WorldEvent
{
    public override TResult Accept<TContext, TResult>(IWorldEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
