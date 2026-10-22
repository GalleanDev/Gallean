namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldGameResumedEvent ev, WorldState state)
    {
        return state;
    }
}
