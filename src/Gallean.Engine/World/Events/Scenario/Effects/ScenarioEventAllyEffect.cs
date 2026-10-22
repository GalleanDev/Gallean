namespace Gallean.Engine;

public sealed record ScenarioEventAllyEffect(
    WorldPlayerId Player1,
    WorldPlayerId Player2,
    bool Permanent) : ScenarioEventEffect
{
    public override TResult Accept<TContext, TResult>(IScenarioEventEffectVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
