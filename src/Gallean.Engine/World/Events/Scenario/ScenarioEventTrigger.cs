namespace Gallean.Engine;

public closed record ScenarioEventTrigger
{
    public abstract TResult Accept<TContext, TResult>(IScenarioEventTriggerVisitor<TContext, TResult> visitor, TContext context);
}
