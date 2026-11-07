namespace Gallean.Engine;

public sealed record WorldResourcesGivenEvent(
    WorldPlayerId PlayerId,
    WorldPlayerId TargetPlayerId,
    WorldResourceSet Resources) : WorldEvent
{
    public override TResult Accept<TContext, TResult>(IWorldEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
