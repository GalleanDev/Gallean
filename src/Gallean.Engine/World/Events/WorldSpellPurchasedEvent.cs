namespace Gallean.Engine;

public sealed record WorldSpellPurchasedEvent(
    WorldSiteId SiteId,
    WorldStackId StackId,
    WorldPlayerId PlayerId,
    WorldSpellDefinitionId SpellId,
    WorldResourceSet Cost) : WorldEvent
{
    public override TResult Accept<TContext, TResult>(IWorldEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
