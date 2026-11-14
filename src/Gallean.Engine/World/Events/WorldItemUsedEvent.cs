namespace Gallean.Engine;

public sealed record WorldItemUsedEvent(
    WorldUnitGroup Group,
    WorldItem Item,
    int? RemainingCharges,
    bool Consumed) : WorldEvent
{
    public override TResult Accept<TContext, TResult>(IWorldEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
