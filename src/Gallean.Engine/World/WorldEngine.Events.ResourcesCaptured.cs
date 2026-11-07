using System.Collections.Immutable;

namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldResourcesCapturedEvent ev, WorldState state)
    {
        WorldPlayer player = state.Players[ev.PlayerId];
        WorldPlayer targetPlayer = state.Players[ev.TargetPlayerId];

        ImmutableSortedDictionary<WorldPlayerId, WorldPlayer> players = state.Players
            .SetItem(ev.PlayerId, player with { Resources = ResourceSetCalculator.Add(player.Resources, ev.Resources) })
            .SetItem(ev.TargetPlayerId, targetPlayer with { Resources = ResourceSetCalculator.Subtract(targetPlayer.Resources, ev.Resources) });

        return state with { Players = players };
    }
}
