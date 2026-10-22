namespace Gallean.Engine;

public sealed record WorldUnitPurchasedEvent(
    WorldFortificationId FortificationId,
    WorldUnit Unit,
    GridPosition Position,
    WorldResourceSet Cost) : WorldEvent
{
    public override TResult Accept<TContext, TResult>(IWorldEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
