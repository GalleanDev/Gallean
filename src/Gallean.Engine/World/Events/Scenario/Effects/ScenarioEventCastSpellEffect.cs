namespace Gallean.Engine;

public sealed record ScenarioEventCastSpellEffect(
    WorldSpellDefinitionId SpellId,
    WorldPlayerId? CasterId) : ScenarioEventEffect
{
    public override TResult Accept<TContext, TResult>(IScenarioEventEffectVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
