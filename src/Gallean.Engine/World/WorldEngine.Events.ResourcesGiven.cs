using System.Collections.Immutable;

namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldResourcesGivenEvent ev, WorldState state)
    {
        WorldPlayer giver = state.Players[ev.PlayerId];
        WorldPlayer receiver = state.Players[ev.TargetPlayerId];

        WorldResourceSet giverResources = giver.Resources;
        giverResources = ResourceSetCalculator.Subtract(giverResources, ev.Resources);

        WorldResourceSet receiverResources = receiver.Resources;
        receiverResources = ResourceSetCalculator.Add(receiverResources, ev.Resources);
        receiverResources = ResourceSetCalculator.Clamp(receiverResources, Context.Globals.MaxResourceAmount);

        ImmutableSortedDictionary<WorldPlayerId, WorldPlayer> players = state.Players
            .SetItem(ev.PlayerId, giver with { Resources = giverResources })
            .SetItem(ev.TargetPlayerId, receiver with { Resources = receiverResources });

        return state with { Players = players };
    }
}
