namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldBagPickedUpEvent ev, WorldState state)
    {
        WorldStack stack = state.Stacks[ev.StackId];
        WorldStack updatedStack = stack with
        {
            Inventory = stack.Inventory.AddRange(ev.Items.Items)
        };

        return state with
        {
            Stacks = state.Stacks.SetItem(ev.StackId, updatedStack),
            Bags = state.Bags.Remove(ev.BagId)
        };
    }
}
