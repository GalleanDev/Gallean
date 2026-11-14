using System.Collections.Immutable;

namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldResourcesCapturedEvent ev, WorldState state)
    {
        WorldPlayer player = state.Players[ev.PlayerId];
        WorldPlayer targetPlayer = state.Players[ev.TargetPlayerId];

        WorldResourceSet playerResources = player.Resources;
        playerResources = ResourceSetCalculator.Add(playerResources, ev.Resources);
        playerResources = ResourceSetCalculator.Clamp(playerResources, Context.Globals.MaxResourceAmount);

        WorldResourceSet targetResources = targetPlayer.Resources;
        targetResources = ResourceSetCalculator.Subtract(targetResources, ev.Resources);

        ImmutableSortedDictionary<WorldPlayerId, WorldPlayer> players = state.Players
            .SetItem(ev.PlayerId, player with { Resources = playerResources })
            .SetItem(ev.TargetPlayerId, targetPlayer with { Resources = targetResources });

        return state with { Players = players };
    }
}
