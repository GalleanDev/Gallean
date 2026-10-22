namespace Gallean.Engine;

public sealed record ScenarioEventAllianceCondition(WorldPlayerId Player1, WorldPlayerId Player2) : ScenarioEventCondition
{
    public override TResult Accept<TContext, TResult>(IScenarioEventConditionVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
