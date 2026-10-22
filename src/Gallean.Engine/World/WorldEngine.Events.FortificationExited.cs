namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldFortificationExitedEvent ev, WorldState state)
    {
        return state;
    }
}
