namespace Gallean.Engine;

public sealed record WorldUnitHealedEvent(
    WorldUnitGroup Group,
    WorldUnitId UnitId,
    int Hitpoints,
    WorldResourceSet Cost) : WorldEvent
{
    public override TResult Accept<TContext, TResult>(IWorldEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
