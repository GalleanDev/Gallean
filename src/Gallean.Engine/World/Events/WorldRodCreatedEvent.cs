namespace Gallean.Engine;

public sealed record WorldRodCreatedEvent(
    WorldRodId RodId,
    WorldPlayerId PlayerId,
    WorldPosition Position) : WorldEvent
{
    public override TResult Accept<TContext, TResult>(IWorldEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
