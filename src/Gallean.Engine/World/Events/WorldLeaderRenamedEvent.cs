namespace Gallean.Engine;

public sealed record WorldLeaderRenamedEvent(
    WorldStackId StackId,
    WorldUnitId UnitId,
    UnitName Name) : WorldEvent
{
    public override TResult Accept<TContext, TResult>(IWorldEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
