namespace Gallean.Engine;

public sealed record ScenarioEventRemoveStackEffect(ScenarioStackReference Stack, bool FirstOnly) : ScenarioEventEffect
{
    public override TResult Accept<TContext, TResult>(IScenarioEventEffectVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
