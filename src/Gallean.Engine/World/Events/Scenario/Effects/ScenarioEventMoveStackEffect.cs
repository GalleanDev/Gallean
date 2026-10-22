namespace Gallean.Engine;

public sealed record ScenarioEventMoveStackEffect(ScenarioStackReference Stack, ScenarioLocationId Location) : ScenarioEventEffect
{
    public override TResult Accept<TContext, TResult>(IScenarioEventEffectVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
