namespace Gallean.Engine;

public closed record ScenarioEventCondition
{
    public abstract TResult Accept<TContext, TResult>(IScenarioEventConditionVisitor<TContext, TResult> visitor, TContext context);
}
