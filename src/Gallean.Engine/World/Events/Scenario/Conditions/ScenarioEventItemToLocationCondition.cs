namespace Gallean.Engine;

public sealed record ScenarioEventItemToLocationCondition(ItemDefinitionId Item, ScenarioLocationId Location) : ScenarioEventCondition
{
    public override TResult Accept<TContext, TResult>(IScenarioEventConditionVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
