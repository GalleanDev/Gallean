using System.Collections.Immutable;

namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldBuildingConstructedEvent ev, WorldState state)
    {
        WorldPlayer player = state.Players[ev.PlayerId];
        WorldPlayerBuildingDefinition building = Context.Definitions.GetPlayerBuilding(ev.BuildingId);

        WorldResourceSet updatedResources = ResourceSetCalculator.Subtract(player.Resources, building.Cost);

        WorldPlayerBuildings buildings = player.Buildings with
        {
            Acquired = player.Buildings.Acquired.Add(ev.BuildingId)
        };

        WorldPlayer updatedPlayer = player with
        {
            Resources = updatedResources,
            Buildings = buildings,
            TurnActions = player.TurnActions with { BuildingConstructed = true }
        };

        ImmutableSortedDictionary<WorldPlayerId, WorldPlayer> updatedPlayers = state.Players.SetItem(ev.PlayerId, updatedPlayer);

        return state with { Players = updatedPlayers };
    }
}
