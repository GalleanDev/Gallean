namespace Gallean.Engine;

public sealed record ScenarioEventDiplomacyEffect(
    WorldPlayerId Player1,
    WorldPlayerId Player2,
    int Relation,
    bool? PermanentWar) : ScenarioEventEffect
{
    public override TResult Accept<TContext, TResult>(IScenarioEventEffectVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
