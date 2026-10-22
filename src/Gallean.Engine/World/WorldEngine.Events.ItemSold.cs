namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldItemSoldEvent ev, WorldState state)
    {
        WorldStack stack = state.Stacks[ev.StackId];
        WorldPlayer player = state.Players[stack.OwnerId];

        WorldResourceSet updatedResources = ResourceSetCalculator.Add(player.Resources, ev.Value);

        WorldInventory updatedInventory = stack.Inventory.Remove(ev.Item);

        WorldStack updatedStack = stack with { Inventory = updatedInventory };

        return state with
        {
            Players = state.Players.SetItem(player.Id, player with { Resources = updatedResources }),
            Stacks = state.Stacks.SetItem(updatedStack.Id, updatedStack)
        };
    }
}
