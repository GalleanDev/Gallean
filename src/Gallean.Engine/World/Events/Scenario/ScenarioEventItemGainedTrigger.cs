namespace Gallean.Engine;

public sealed record ScenarioEventItemGainedTrigger(WorldUnitGroup Receiver) : ScenarioEventTrigger
{
    public override TResult Accept<TContext, TResult>(IScenarioEventTriggerVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
