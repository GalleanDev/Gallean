namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldUnitPurchasedEvent ev, WorldState state)
    {
        WorldFortification fortification = state.Fortifications[ev.FortificationId];
        WorldPlayerId ownerId = FortificationCalculator.GetOwner(fortification)!.Value;
        WorldPlayer player = state.Players[ownerId];

        WorldResourceSet updatedResources = ResourceSetCalculator.Subtract(player.Resources, ev.Cost);
        WorldPlayer updatedPlayer = player with { Resources = updatedResources };

        int capacity = GarrisonCalculator.GetTotalCapacity(fortification, Context.Globals);
        WorldGarrisonBuilder garrisonBuilder = new(Context.Definitions, fortification.Garrison, capacity);

        foreach ((WorldUnit unit, GridPosition position) in fortification.Garrison.GetUnits())
        {
            garrisonBuilder.PlaceUnit(unit, position);
        }

        garrisonBuilder.PlaceUnit(ev.Unit, ev.Position);

        WorldFortification updatedFortification = fortification with { Garrison = garrisonBuilder.Build() };

        return state with
        {
            Players = state.Players.SetItem(updatedPlayer.Id, updatedPlayer),
            Fortifications = state.Fortifications.SetItem(updatedFortification.Id, updatedFortification)
        };
    }
}
