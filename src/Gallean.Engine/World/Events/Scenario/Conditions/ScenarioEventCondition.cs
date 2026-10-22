namespace Gallean.Engine;

public abstract record ScenarioEventCondition
{
    public abstract TResult Accept<TContext, TResult>(IScenarioEventConditionVisitor<TContext, TResult> visitor, TContext context);
}
