namespace Gallean.Engine;

public sealed record ScenarioEventDestroyItemEffect(ItemDefinitionId Item, bool TriggererOnly) : ScenarioEventEffect
{
    public override TResult Accept<TContext, TResult>(IScenarioEventEffectVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
