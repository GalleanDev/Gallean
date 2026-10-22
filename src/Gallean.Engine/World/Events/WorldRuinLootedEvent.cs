namespace Gallean.Engine;

public sealed record WorldRuinLootedEvent(
    WorldFortificationId RuinId,
    WorldStackId StackId,
    WorldResourceSet Resources,
    WorldItem? Item) : WorldEvent
{
    public override TResult Accept<TContext, TResult>(IWorldEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
