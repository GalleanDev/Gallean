namespace Gallean.Engine;

public sealed record ScenarioEventLeaderOwningItemCondition(ScenarioStackReference Stack, ItemDefinitionId Item) : ScenarioEventCondition
{
    public override TResult Accept<TContext, TResult>(IScenarioEventConditionVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
