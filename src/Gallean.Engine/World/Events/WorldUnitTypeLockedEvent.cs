namespace Gallean.Engine;

public sealed record WorldUnitTypeLockedEvent(
    WorldUnitGroup Group,
    WorldUnitId UnitId) : WorldEvent
{
    public override TResult Accept<TContext, TResult>(IWorldEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
