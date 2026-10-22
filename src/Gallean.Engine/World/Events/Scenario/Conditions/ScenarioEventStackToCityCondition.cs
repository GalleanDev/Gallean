namespace Gallean.Engine;

public sealed record ScenarioEventStackToCityCondition(ScenarioStackReference Stack, WorldFortificationId Fortification) : ScenarioEventCondition
{
    public override TResult Accept<TContext, TResult>(IScenarioEventConditionVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
