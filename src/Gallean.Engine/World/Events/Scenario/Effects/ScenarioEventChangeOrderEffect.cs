namespace Gallean.Engine;

public sealed record ScenarioEventChangeOrderEffect(
    ScenarioStackReference Stack,
    WorldCanonicalStackOrder Order,
    bool FirstOnly) : ScenarioEventEffect
{
    public override TResult Accept<TContext, TResult>(IScenarioEventEffectVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
