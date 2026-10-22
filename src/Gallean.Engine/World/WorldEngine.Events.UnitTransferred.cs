namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldUnitTransferredEvent ev, WorldState state)
    {
        if (!WorldUnitGroupCalculator.TryTransferUnit(
            state,
            Context,
            ev.Source,
            ev.Target,
            ev.UnitId,
            ev.TargetPosition,
            out WorldState? newState,
            out WorldCommandError? error))
        {
            throw new InvalidOperationException($"Unexpected unit transfer: {error}.");
        }

        return newState;
    }
}
