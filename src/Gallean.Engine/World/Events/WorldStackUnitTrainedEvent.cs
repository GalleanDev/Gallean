namespace Gallean.Engine;

public sealed record WorldStackUnitTrainedEvent(
    WorldStackId StackId,
    WorldUnitId UnitId,
    int Experience,
    WorldResourceSet Cost) : WorldEvent
{
    public override TResult Accept<TContext, TResult>(IWorldEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
