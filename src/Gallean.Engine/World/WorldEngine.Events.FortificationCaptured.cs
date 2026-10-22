namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldFortificationCapturedEvent ev, WorldState state)
    {
        return state;
    }
}
