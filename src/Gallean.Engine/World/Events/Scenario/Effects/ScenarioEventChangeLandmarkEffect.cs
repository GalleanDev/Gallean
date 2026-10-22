namespace Gallean.Engine;

public sealed record ScenarioEventChangeLandmarkEffect(WorldLandmarkId Landmark, WorldLandmarkDefinitionId LandmarkType) : ScenarioEventEffect
{
    public override TResult Accept<TContext, TResult>(IScenarioEventEffectVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
