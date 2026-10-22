namespace Gallean.Engine;

public sealed record ScenarioEventFrequencyCondition(int Frequency) : ScenarioEventCondition
{
    public override TResult Accept<TContext, TResult>(IScenarioEventConditionVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
