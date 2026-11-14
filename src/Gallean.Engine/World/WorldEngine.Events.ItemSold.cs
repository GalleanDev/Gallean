namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldItemSoldEvent ev, WorldState state)
    {
        WorldStack stack = state.Stacks[ev.StackId];
        WorldPlayer player = state.Players[stack.OwnerId];

        WorldResourceSet playerResources = player.Resources;
        playerResources = ResourceSetCalculator.Add(playerResources, ev.Value);
        playerResources = ResourceSetCalculator.Clamp(playerResources, Context.Globals.MaxResourceAmount);

        WorldInventory updatedInventory = stack.Inventory.Remove(ev.Item);

        WorldStack updatedStack = stack with { Inventory = updatedInventory };

        return state with
        {
            Players = state.Players.SetItem(player.Id, player with { Resources = playerResources }),
            Stacks = state.Stacks.SetItem(updatedStack.Id, updatedStack)
        };
    }
}
