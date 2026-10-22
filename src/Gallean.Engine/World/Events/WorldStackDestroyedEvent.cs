namespace Gallean.Engine;

public sealed record WorldStackDestroyedEvent(
    WorldStackId StackId,
    WorldUnitGroup? Destroyer,
    WorldPlayerId? DestroyerPlayerId) : WorldEvent
{
    public override TResult Accept<TContext, TResult>(IWorldEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
