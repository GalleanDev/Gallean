namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldScenarioPopupEvent ev, WorldState state)
    {
        return state;
    }
}
