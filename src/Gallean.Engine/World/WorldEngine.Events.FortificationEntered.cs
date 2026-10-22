namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldFortificationEnteredEvent ev, WorldState state)
    {
        return state;
    }
}
