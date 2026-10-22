namespace Gallean.Engine;

public sealed record WorldSpyRemovedEvent(
    WorldPlayerId PlayerId,
    WorldPlayerId TargetPlayerId) : WorldEvent
{
    public override TResult Accept<TContext, TResult>(IWorldEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
