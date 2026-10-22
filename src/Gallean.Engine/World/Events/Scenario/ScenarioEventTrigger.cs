namespace Gallean.Engine;

public abstract record ScenarioEventTrigger
{
    public abstract TResult Accept<TContext, TResult>(IScenarioEventTriggerVisitor<TContext, TResult> visitor, TContext context);
}
