namespace Gallean.Engine;

public sealed record WorldItemEquippedEvent(
    WorldStackId StackId,
    WorldItem Item,
    LeaderEquipmentSlot Slot) : WorldEvent
{
    public override TResult Accept<TContext, TResult>(IWorldEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
