namespace Gallean.Engine;

public sealed record WorldRuinSpiedEvent(
    WorldPlayerId PlayerId,
    WorldFortificationId RuinId) : WorldEvent
{
    public override TResult Accept<TContext, TResult>(IWorldEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
