namespace Gallean.Engine;

public sealed record WorldStackOwnerChangedEvent(
    WorldStackId StackId,
    WorldPlayerId OwnerId,
    WorldSubraceId SubraceId,
    bool PlayAnimation) : WorldEvent
{
    public override TResult Accept<TContext, TResult>(IWorldEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
