namespace Gallean.Engine;

public sealed record WorldBagPickedUpEvent(
    WorldStackId StackId,
    WorldBagId BagId,
    WorldInventory Items) : WorldEvent
{
    public override TResult Accept<TContext, TResult>(IWorldEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
