namespace Gallean.Engine;

public sealed record ScenarioEventVariableInRangeCondition(
    ScenarioVariableRange Range,
    ScenarioVariableClause? Second) : ScenarioEventCondition
{
    public override TResult Accept<TContext, TResult>(IScenarioEventConditionVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
