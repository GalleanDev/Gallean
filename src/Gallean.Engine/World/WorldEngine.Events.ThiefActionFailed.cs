namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldThiefActionFailedEvent ev, WorldState state)
    {
        return state;
    }
}
