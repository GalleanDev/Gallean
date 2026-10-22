namespace Gallean.Engine;

public sealed record ScenarioEventGiveItemEffect(ItemDefinitionId Item, ScenarioEventGiveItemTarget Target) : ScenarioEventEffect
{
    public override TResult Accept<TContext, TResult>(IScenarioEventEffectVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
