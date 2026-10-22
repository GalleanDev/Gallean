namespace Gallean.Engine;

public sealed record WorldGoldGivenEvent(
    WorldPlayerId PlayerId,
    WorldPlayerId TargetPlayerId,
    int Gold) : WorldEvent
{
    public override TResult Accept<TContext, TResult>(IWorldEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
