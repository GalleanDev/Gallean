namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldItemUnequippedEvent ev, WorldState state)
    {
        WorldStack stack = state.Stacks[ev.StackId];
        WorldItem equippedItem = stack.Equipment[ev.Slot]!;

        WorldInventory updatedInventory = stack.Inventory.Add(equippedItem);
        LeaderEquipment updatedEquipment = stack.Equipment.Without(ev.Slot);

        return state with
        {
            Stacks = state.Stacks.SetItem(stack.Id, stack with
            {
                Units = LeaderEquipmentCalculator.UpdateUnitModifiers(stack.Units, updatedEquipment, Context.Definitions),
                Inventory = updatedInventory,
                Equipment = updatedEquipment
            })
        };
    }
}
