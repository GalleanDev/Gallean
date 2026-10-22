namespace Gallean.Engine;

public sealed record ScenarioEventLootingRuinCondition(WorldFortificationId Ruin) : ScenarioEventCondition
{
    public override TResult Accept<TContext, TResult>(IScenarioEventConditionVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
