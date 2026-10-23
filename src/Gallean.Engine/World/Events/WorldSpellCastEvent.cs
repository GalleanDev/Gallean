namespace Gallean.Engine;

public sealed record WorldSpellCastEvent(
    WorldPlayerId? CasterId,
    WorldSpellDefinitionId SpellId,
    WorldSpellTarget? Target,
    WorldResourceSet Cost,
    WorldSpellCastSource Source) : WorldEvent
{
    public override TResult Accept<TContext, TResult>(IWorldEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
