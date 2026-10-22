namespace Gallean.Engine;

public abstract record ScenarioEventEffect
{
    public abstract TResult Accept<TContext, TResult>(IScenarioEventEffectVisitor<TContext, TResult> visitor, TContext context);
}
