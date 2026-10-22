namespace Gallean.Engine;

public sealed record WorldUnitModifierExpiredEvent(
    WorldUnitGroup Group,
    WorldUnitId UnitId,
    UnitModifierDefinitionId ModifierId) : WorldEvent
{
    public override TResult Accept<TContext, TResult>(IWorldEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
