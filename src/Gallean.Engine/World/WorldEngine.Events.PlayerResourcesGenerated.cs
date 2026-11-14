namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldTurnResourcesGeneratedEvent ev, WorldState state)
    {
        WorldPlayer player = state.Players[ev.PlayerId];

        WorldResourceSet playerResources = player.Resources;
        playerResources = ResourceSetCalculator.Add(playerResources, ev.Resources);
        playerResources = ResourceSetCalculator.Clamp(playerResources, Context.Globals.MaxResourceAmount);

        return state with
        {
            Players = state.Players.SetItem(ev.PlayerId, player with { Resources = playerResources })
        };
    }
}
