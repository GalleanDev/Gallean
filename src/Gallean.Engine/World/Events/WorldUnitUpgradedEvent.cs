namespace Gallean.Engine;

public sealed record WorldUnitUpgradedEvent(
    WorldUnitGroup Group,
    WorldUnitId UnitId,
    UnitDefinitionId DefinitionId,
    int Level,
    int Hitpoints) : WorldEvent
{
    public override TResult Accept<TContext, TResult>(IWorldEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
