namespace Gallean.Engine;

public interface IScenarioEventTriggerVisitor<in TContext, out TResult>
{
    TResult Visit(ScenarioEventTurnStartedTrigger trigger, TContext context);
    TResult Visit(ScenarioEventStackMovedTrigger trigger, TContext context);
    TResult Visit(ScenarioEventStackCreatedTrigger trigger, TContext context);
    TResult Visit(ScenarioEventStackDestroyedTrigger trigger, TContext context);
    TResult Visit(ScenarioEventFortificationCapturedTrigger trigger, TContext context);
    TResult Visit(ScenarioEventItemGainedTrigger trigger, TContext context);
}
