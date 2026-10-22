namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldCityBribedEvent ev, WorldState state)
    {
        WorldPlayerId briberId = state.Stacks[ev.StackId].OwnerId;
        WorldPlayer briber = state.Players[briberId];

        return state with
        {
            Players = state.Players.SetItem(
                briberId,
                briber with { Resources = ResourceSetCalculator.Subtract(briber.Resources, ev.Cost) })
        };
    }
}
