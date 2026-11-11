namespace Gallean.Engine;

public closed record ScenarioEventEffect
{
    public abstract TResult Accept<TContext, TResult>(IScenarioEventEffectVisitor<TContext, TResult> visitor, TContext context);
}
