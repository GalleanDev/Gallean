namespace Gallean.Engine;

public sealed record ScenarioEventDiplomacyRelationsCondition(
    WorldPlayerId Player1,
    WorldPlayerId Player2,
    WorldRelationLevel Level) : ScenarioEventCondition
{
    public override TResult Accept<TContext, TResult>(IScenarioEventConditionVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
