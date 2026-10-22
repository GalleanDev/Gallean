namespace Gallean.Engine;

public sealed record WorldDiplomacyItemTradedEvent(
    WorldPlayerId SellerId,
    WorldPlayerId BuyerId,
    WorldFortificationId SourceFortificationId,
    WorldItem Item,
    int Price) : WorldEvent
{
    public override TResult Accept<TContext, TResult>(IWorldEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
