namespace Gallean.Engine;

public sealed record ScenarioEventStackExistenceCondition(ScenarioStackReference Stack, bool MustExist) : ScenarioEventCondition
{
    public override TResult Accept<TContext, TResult>(IScenarioEventConditionVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
