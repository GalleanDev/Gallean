namespace Gallean.Engine;

public sealed record ScenarioEventFortificationCapturedTrigger(
    WorldFortificationId FortificationId,
    WorldStackId CapturingStackId) : ScenarioEventTrigger
{
    public override TResult Accept<TContext, TResult>(IScenarioEventTriggerVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
