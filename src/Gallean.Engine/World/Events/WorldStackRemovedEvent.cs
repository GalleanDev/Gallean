namespace Gallean.Engine;

public sealed record WorldStackRemovedEvent(WorldStackId StackId) : WorldEvent
{
    public override TResult Accept<TContext, TResult>(IWorldEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
