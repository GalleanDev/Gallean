using Gallean.Common;

namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldCityUpgradedEvent ev, WorldState state)
    {
        WorldCity city = (WorldCity)state.Fortifications[ev.CityId];
        WorldPlayer owner = state.Players[city.OwnerId];

        WorldResourceSet updatedResources = ResourceSetCalculator.Subtract(owner.Resources, ev.Cost);

        state = state with
        {
            Fortifications = state.Fortifications.SetItem(ev.CityId, city with
            {
                DefinitionId = ev.DefinitionId,
                GrowthTurn = state.TurnInfo.TurnNumber
            }),
            Players = state.Players.SetItem(owner.Id, owner with
            {
                Resources = updatedResources
            })
        };

        WorldPlayer updatedOwner = state.Players[city.OwnerId];
        ImmutableBitmap updatedFog = FogOfWarCalculator.Update(state, updatedOwner.Id, Context);

        WorldState updatedState = state with
        {
            Players = state.Players.SetItem(updatedOwner.Id, updatedOwner with { FogOfWar = updatedFog })
        };

        return FortificationCalculator.UpdateUnitModifiers(updatedState, Context.Definitions);
    }
}
