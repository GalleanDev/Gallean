namespace Gallean.Engine;

public sealed record WorldItemUnequippedEvent(
    WorldStackId StackId,
    LeaderEquipmentSlot Slot) : WorldEvent
{
    public override TResult Accept<TContext, TResult>(IWorldEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
