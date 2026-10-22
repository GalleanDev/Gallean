namespace Gallean.Engine;

public sealed record ScenarioEventGiveSpellEffect(WorldSpellDefinitionId SpellId) : ScenarioEventEffect
{
    public override TResult Accept<TContext, TResult>(IScenarioEventEffectVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
