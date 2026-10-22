namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldRoundEndedEvent ev, WorldState state)
    {
        return state;
    }
}
