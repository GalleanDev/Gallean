namespace Gallean.Engine;

public sealed record ScenarioEventEnableEventEffect(ScenarioEventId TargetEvent, bool Enabled) : ScenarioEventEffect
{
    public override TResult Accept<TContext, TResult>(IScenarioEventEffectVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
