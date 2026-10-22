namespace Gallean.Engine;

public sealed record ScenarioEventDestroyingStackCondition(ScenarioStackReference Stack) : ScenarioEventCondition
{
    public override TResult Accept<TContext, TResult>(IScenarioEventConditionVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
