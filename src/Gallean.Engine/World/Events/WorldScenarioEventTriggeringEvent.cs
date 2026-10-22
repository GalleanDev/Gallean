namespace Gallean.Engine;

public sealed record WorldScenarioEventTriggeringEvent(
    ScenarioEventId EventId,
    WorldPlayerId TriggererId,
    ScenarioEventTrigger Trigger) : WorldEvent, IScopeBeginEvent<WorldScenarioEventTriggeredEvent>
{
    public WorldScenarioEventTriggeredEvent CreateScopeEndEvent()
    {
        return new WorldScenarioEventTriggeredEvent(EventId, TriggererId, Trigger);
    }

    public override TResult Accept<TContext, TResult>(IWorldEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
