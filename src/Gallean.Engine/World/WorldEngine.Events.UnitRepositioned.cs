namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldUnitRepositionedEvent ev, WorldState state)
    {
        if (!WorldUnitGroupCalculator.TryRepositionUnit(
            state,
            Context,
            ev.Group,
            ev.UnitId,
            ev.TargetPosition,
            out WorldState? newState,
            out WorldCommandError? error))
        {
            throw new InvalidOperationException($"Unexpected unit reposition: {error}.");
        }

        return newState;
    }
}
