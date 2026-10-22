namespace Gallean.Engine;

public sealed record ScenarioEventPopupEffect(
    ScenarioTextId TextId,
    ScenarioEventPopupAudience Audience,
    ScenarioEventPortraitId? PortraitId,
    bool IsPortraitLeft,
    bool IsPortraitTriggerer) : ScenarioEventEffect
{
    public override TResult Accept<TContext, TResult>(IScenarioEventEffectVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
