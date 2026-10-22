namespace Gallean.Engine;

public sealed record WorldUnitModifierAppliedEvent(
    WorldUnitGroup Group,
    WorldUnitId UnitId,
    UnitModifier Modifier) : WorldEvent
{
    public override TResult Accept<TContext, TResult>(IWorldEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
