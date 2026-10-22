namespace Gallean.Engine;

public sealed record WorldSpellStolenEvent(
    WorldStackId StackId,
    WorldPlayerId? TargetPlayerId,
    WorldSpellDefinitionId SpellId) : WorldEvent
{
    public override TResult Accept<TContext, TResult>(IWorldEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
