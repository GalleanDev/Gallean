namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldUnitsShuffledEvent ev, WorldState state)
    {
        return WorldUnitGroupCalculator.WithUnits(state, ev.Group, ev.Units);
    }
}
