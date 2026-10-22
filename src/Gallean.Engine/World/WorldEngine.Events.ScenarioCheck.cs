namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldScenarioCheckEvent ev, WorldState state)
    {
        return state;
    }
}
