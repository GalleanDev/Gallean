namespace Gallean.Engine;

public sealed record WorldFortificationPoisonedEvent(
    WorldFortificationId FortificationId,
    UnitGrid<WorldUnit> PoisonedGarrison) : WorldEvent
{
    public override TResult Accept<TContext, TResult>(IWorldEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
