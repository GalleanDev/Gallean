namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldScenarioEventTriggeredEvent ev, WorldState state)
    {
        return state;
    }
}
