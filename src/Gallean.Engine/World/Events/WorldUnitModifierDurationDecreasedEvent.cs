namespace Gallean.Engine;

public sealed record WorldUnitModifierDurationDecreasedEvent(
    WorldUnitGroup Group,
    WorldUnitId UnitId,
    UnitModifierDefinitionId ModifierId,
    int RemainingTurns) : WorldEvent
{
    public override TResult Accept<TContext, TResult>(IWorldEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
