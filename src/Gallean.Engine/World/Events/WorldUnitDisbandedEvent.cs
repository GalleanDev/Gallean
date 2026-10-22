namespace Gallean.Engine;

public sealed record WorldUnitDisbandedEvent(
    WorldUnitGroup Group,
    WorldUnitId UnitId) : WorldEvent
{
    public override TResult Accept<TContext, TResult>(IWorldEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
