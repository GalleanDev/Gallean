namespace Gallean.Engine;

public sealed record ScenarioEventChangeTerrainEffect(ScenarioLocationId Location, WorldMapTerrain Terrain, int AreaSize) : ScenarioEventEffect
{
    public override TResult Accept<TContext, TResult>(IScenarioEventEffectVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
