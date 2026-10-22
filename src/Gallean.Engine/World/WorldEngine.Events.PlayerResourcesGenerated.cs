namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldTurnResourcesGeneratedEvent ev, WorldState state)
    {
        WorldPlayer player = state.Players[ev.PlayerId];

        return state with
        {
            Players = state.Players.SetItem(ev.PlayerId, player with
            {
                Resources = ResourceSetCalculator.Add(player.Resources, ev.Resources)
            })
        };
    }
}
