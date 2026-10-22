namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldStackMovedEvent ev, WorldState state)
    {
        return state;
    }
}
