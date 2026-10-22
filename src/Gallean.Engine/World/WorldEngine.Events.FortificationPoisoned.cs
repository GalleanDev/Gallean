namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldFortificationPoisonedEvent ev, WorldState state)
    {
        WorldFortification fortification = state.Fortifications[ev.FortificationId];
        WorldFortification updatedFortification = fortification with { Garrison = ev.PoisonedGarrison };

        return state with { Fortifications = state.Fortifications.SetItem(ev.FortificationId, updatedFortification) };
    }
}
