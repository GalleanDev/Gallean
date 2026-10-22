namespace Gallean.Engine;

public sealed record WorldRodDestroyedEvent(
    WorldRodId RodId,
    WorldPlayerId PlayerId,
    WorldPlayerId TargetPlayerId,
    WorldPosition Position) : WorldEvent
{
    public override TResult Accept<TContext, TResult>(IWorldEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
