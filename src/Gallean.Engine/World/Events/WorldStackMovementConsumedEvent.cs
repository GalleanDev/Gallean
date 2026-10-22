namespace Gallean.Engine;

public sealed record WorldStackMovementConsumedEvent(WorldStackId StackId, int NewMovementPoints) : WorldEvent
{
    public override TResult Accept<TContext, TResult>(IWorldEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
