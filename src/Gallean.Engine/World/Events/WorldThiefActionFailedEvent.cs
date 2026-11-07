namespace Gallean.Engine;

public sealed record WorldThiefActionFailedEvent(
    WorldStackId ThiefStackId,
    WorldThiefActionType Action,
    WorldPlayerId? TargetPlayerId,
    WorldUnitGroup? Target) : WorldEvent
{
    public override TResult Accept<TContext, TResult>(IWorldEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
