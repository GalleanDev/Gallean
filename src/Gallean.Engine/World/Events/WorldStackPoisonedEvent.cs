namespace Gallean.Engine;

public sealed record WorldStackPoisonedEvent(
    WorldStackId StackId,
    UnitGrid<WorldUnit> PoisonedUnits) : WorldEvent
{
    public override TResult Accept<TContext, TResult>(IWorldEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
