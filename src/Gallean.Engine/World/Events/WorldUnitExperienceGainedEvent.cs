namespace Gallean.Engine;

public sealed record WorldUnitExperienceGainedEvent(
    WorldUnitGroup Group,
    WorldUnitId UnitId,
    int Experience) : WorldEvent
{
    public override TResult Accept<TContext, TResult>(IWorldEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
