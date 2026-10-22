namespace Gallean.Engine;

public sealed record ScenarioEventCreateStackEffect(
    ScenarioStackTemplateId Template,
    ScenarioLocationId Location) : ScenarioEventEffect
{
    public override TResult Accept<TContext, TResult>(IScenarioEventEffectVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
