namespace Gallean.Engine;

public sealed record WorldAllianceStartedEvent(
    WorldPlayerId PlayerId,
    WorldPlayerId TargetPlayerId,
    bool Permanent) : WorldEvent
{
    public override TResult Accept<TContext, TResult>(IWorldEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
