namespace Gallean.Engine;

public sealed record WorldResourcesStolenEvent(
    WorldStackId StackId,
    WorldPlayerId TargetPlayerId,
    WorldResourceSet Resources) : WorldEvent
{
    public override TResult Accept<TContext, TResult>(IWorldEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
