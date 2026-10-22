using System.Collections.Immutable;

namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldResourcesStolenEvent ev, WorldState state)
    {
        WorldPlayerId thiefPlayerId = state.Stacks[ev.StackId].OwnerId;
        WorldPlayer thiefPlayer = state.Players[thiefPlayerId];
        WorldPlayer targetPlayer = state.Players[ev.TargetPlayerId];

        ImmutableSortedDictionary<WorldPlayerId, WorldPlayer> players = state.Players
            .SetItem(thiefPlayerId, thiefPlayer with { Resources = ResourceSetCalculator.Add(thiefPlayer.Resources, ev.Resources) })
            .SetItem(ev.TargetPlayerId, targetPlayer with { Resources = ResourceSetCalculator.Subtract(targetPlayer.Resources, ev.Resources) });

        return state with { Players = players };
    }
}
