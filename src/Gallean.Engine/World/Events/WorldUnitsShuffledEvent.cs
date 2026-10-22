namespace Gallean.Engine;

public sealed record WorldUnitsShuffledEvent(
    WorldUnitGroup Group,
    UnitGrid<WorldUnit> Units) : WorldEvent
{
    public override TResult Accept<TContext, TResult>(IWorldEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
