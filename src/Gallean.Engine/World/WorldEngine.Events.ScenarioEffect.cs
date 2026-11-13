namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldScenarioEffectEvent ev, WorldState state)
    {
        return state;
    }
}
