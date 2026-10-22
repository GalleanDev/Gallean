namespace Gallean.Engine;

public sealed record WorldUnitTransferredEvent(
    WorldUnitGroup Source,
    WorldUnitGroup Target,
    WorldUnitId UnitId,
    GridPosition TargetPosition) : WorldEvent
{
    public override TResult Accept<TContext, TResult>(IWorldEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
