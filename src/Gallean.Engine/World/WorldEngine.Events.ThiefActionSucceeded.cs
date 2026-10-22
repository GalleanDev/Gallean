namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldThiefActionSucceededEvent ev, WorldState state)
    {
        return state;
    }
}
