using System.Collections.Immutable;

namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldResourcesGivenEvent ev, WorldState state)
    {
        WorldPlayer giver = state.Players[ev.PlayerId];
        WorldPlayer receiver = state.Players[ev.TargetPlayerId];

        ImmutableSortedDictionary<WorldPlayerId, WorldPlayer> players = state.Players
            .SetItem(ev.PlayerId, giver with { Resources = ResourceSetCalculator.Subtract(giver.Resources, ev.Resources) })
            .SetItem(ev.TargetPlayerId, receiver with { Resources = ResourceSetCalculator.Add(receiver.Resources, ev.Resources) });

        return state with { Players = players };
    }
}
