namespace Gallean.Engine;

public sealed record WorldItemDroppedEvent(
    WorldStackId StackId,
    WorldBagId BagId,
    WorldPosition Position,
    WorldItem Item) : WorldEvent
{
    public override TResult Accept<TContext, TResult>(IWorldEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
