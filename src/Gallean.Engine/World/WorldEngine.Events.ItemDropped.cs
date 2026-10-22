namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldItemDroppedEvent ev, WorldState state)
    {
        WorldStack stack = state.Stacks[ev.StackId];
        WorldInventory remaining = stack.Inventory.Remove(ev.Item);

        WorldBag updatedBag;

        if (state.Bags.TryGetValue(ev.BagId, out WorldBag? bag))
        {
            updatedBag = bag with { Inventory = bag.Inventory.Add(ev.Item) };
        }
        else
        {
            updatedBag = new WorldBag(
                ev.BagId,
                ev.Position,
                new WorldObjectVariant(0),
                new WorldInventory([ev.Item]),
                Context.Globals.AiDefaultPriority);
        }

        return state with
        {
            Stacks = state.Stacks.SetItem(ev.StackId, stack with { Inventory = remaining }),
            Bags = state.Bags.SetItem(ev.BagId, updatedBag)
        };
    }
}
