namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldPlayerDefeatedEvent ev, WorldState state)
    {
        return state;
    }
}
