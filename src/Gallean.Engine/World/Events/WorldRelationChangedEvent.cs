namespace Gallean.Engine;

public sealed record WorldRelationChangedEvent(
    WorldPlayerId PlayerId,
    WorldPlayerId TargetPlayerId,
    int Relation) : WorldEvent
{
    public override TResult Accept<TContext, TResult>(IWorldEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
