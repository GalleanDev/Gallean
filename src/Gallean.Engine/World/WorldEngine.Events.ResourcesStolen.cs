using System.Collections.Immutable;

namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldResourcesStolenEvent ev, WorldState state)
    {
        WorldPlayerId thiefPlayerId = state.Stacks[ev.StackId].OwnerId;
        WorldPlayer thiefPlayer = state.Players[thiefPlayerId];
        WorldPlayer targetPlayer = state.Players[ev.TargetPlayerId];

        WorldResourceSet thiefResources = thiefPlayer.Resources;
        thiefResources = ResourceSetCalculator.Add(thiefResources, ev.Resources);
        thiefResources = ResourceSetCalculator.Clamp(thiefResources, Context.Globals.MaxResourceAmount);

        WorldResourceSet targetResources = targetPlayer.Resources;
        targetResources = ResourceSetCalculator.Subtract(targetResources, ev.Resources);

        ImmutableSortedDictionary<WorldPlayerId, WorldPlayer> players = state.Players
            .SetItem(thiefPlayerId, thiefPlayer with { Resources = thiefResources })
            .SetItem(ev.TargetPlayerId, targetPlayer with { Resources = targetResources });

        return state with { Players = players };
    }
}
