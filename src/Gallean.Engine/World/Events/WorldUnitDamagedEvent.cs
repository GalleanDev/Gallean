namespace Gallean.Engine;

public sealed record WorldUnitDamagedEvent(
    WorldUnitGroup Group,
    WorldUnitId UnitId,
    int Hitpoints,
    WorldPlayerId? SourcePlayerId) : WorldEvent
{
    public override TResult Accept<TContext, TResult>(IWorldEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
