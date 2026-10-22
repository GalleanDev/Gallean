namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldTurnStartedEvent ev, WorldState state)
    {
        return state;
    }
}
