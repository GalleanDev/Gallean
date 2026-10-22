namespace Gallean.Engine;

public sealed record WorldInventoryItemTransferredEvent(
    WorldUnitGroup Source,
    WorldUnitGroup Target,
    WorldItem Item) : WorldEvent
{
    public override TResult Accept<TContext, TResult>(IWorldEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
