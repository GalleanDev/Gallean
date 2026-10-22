namespace Gallean.Engine;

public sealed record WorldScenarioEventTriggeredEvent(
    ScenarioEventId EventId,
    WorldPlayerId TriggererId,
    ScenarioEventTrigger Trigger) : WorldEvent, IScopeEndEvent
{
    public override TResult Accept<TContext, TResult>(IWorldEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
