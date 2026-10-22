using System.Collections.Immutable;

namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldRuinLootedEvent ev, WorldState state)
    {
        if (state.Fortifications[ev.RuinId] is not WorldRuin ruin)
        {
            throw new InvalidOperationException($"Unexpected type of fortification '{ev.RuinId}'.");
        }

        WorldStack stack = state.Stacks[ev.StackId];
        WorldPlayer looter = state.Players[stack.OwnerId];

        ImmutableSortedDictionary<WorldStackId, WorldStack> updatedStacks = state.Stacks;

        if (ev.Item is { } item)
        {
            updatedStacks = updatedStacks.SetItem(stack.Id, stack with { Inventory = stack.Inventory.Add(item) });
        }

        WorldPlayer updatedLooter = looter with
        {
            Resources = ResourceSetCalculator.Add(looter.Resources, ev.Resources)
        };

        return state with
        {
            Stacks = updatedStacks,
            Players = state.Players.SetItem(looter.Id, updatedLooter),
            Fortifications = state.Fortifications.SetItem(ruin.Id, ruin with { LooterId = stack.OwnerId })
        };
    }
}
