namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldAiActionStartedEvent ev, WorldState state)
    {
        return state;
    }
}
