namespace Gallean.Engine;

public sealed record WorldUnitRepositionedEvent(
    WorldUnitGroup Group,
    WorldUnitId UnitId,
    GridPosition TargetPosition) : WorldEvent
{
    public override TResult Accept<TContext, TResult>(IWorldEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
