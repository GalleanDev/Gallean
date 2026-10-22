namespace Gallean.Engine;

public sealed record ScenarioEventMoveBesideEffect(ScenarioStackReference Stack) : ScenarioEventEffect
{
    public override TResult Accept<TContext, TResult>(IScenarioEventEffectVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
