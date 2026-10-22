using System.Collections.Immutable;

namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldGoldGivenEvent ev, WorldState state)
    {
        WorldPlayer giver = state.Players[ev.PlayerId];
        WorldPlayer receiver = state.Players[ev.TargetPlayerId];
        WorldResourceSet gold = new(ev.Gold, 0, 0, 0, 0, 0);

        ImmutableSortedDictionary<WorldPlayerId, WorldPlayer> players = state.Players
            .SetItem(ev.PlayerId, giver with { Resources = ResourceSetCalculator.Subtract(giver.Resources, gold) })
            .SetItem(ev.TargetPlayerId, receiver with { Resources = ResourceSetCalculator.Add(receiver.Resources, gold) });

        return state with { Players = players };
    }
}
