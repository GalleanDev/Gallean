namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldItemEquippedEvent ev, WorldState state)
    {
        WorldStack stack = state.Stacks[ev.StackId];

        WorldInventory updatedInventory = stack.Inventory.Remove(ev.Item);
        LeaderEquipment updatedEquipment = stack.Equipment.With(ev.Slot, ev.Item);

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
