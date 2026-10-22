namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldItemUsedEvent ev, WorldState state)
    {
        WorldStack stack = state.Stacks[ev.StackId];

        if (ev.Consumed)
        {
            return state with { Stacks = state.Stacks.SetItem(stack.Id, RemoveItem(stack, ev.Item)) };
        }
        else if (ev.RemainingCharges is { } remainingCharges)
        {
            return state with { Stacks = state.Stacks.SetItem(stack.Id, UpdateItem(stack, ev.Item, ev.Item with { Charges = remainingCharges })) };
        }
        else
        {
            WorldPlayer player = state.Players[stack.OwnerId];
            WorldPlayerTurnActions actions = player.TurnActions;
            int uses = actions.ItemUses.GetValueOrDefault(ev.Item.Id);
            WorldPlayerTurnActions updatedActions = actions with { ItemUses = actions.ItemUses.SetItem(ev.Item.Id, uses + 1) };

            return state with { Players = state.Players.SetItem(stack.OwnerId, player with { TurnActions = updatedActions }) };
        }
    }

    private WorldStack RemoveItem(WorldStack stack, WorldItem item)
    {
        LeaderEquipmentSlot? equipmentSlot = stack.GetEquipmentSlot(item.Id);

        if (equipmentSlot is null)
        {
            return stack with { Inventory = stack.Inventory.Remove(item) };
        }
        else
        {
            LeaderEquipment updatedEquipment = stack.Equipment.Without(equipmentSlot.Value);

            return stack with
            {
                Units = LeaderEquipmentCalculator.UpdateUnitModifiers(stack.Units, updatedEquipment, Context.Definitions),
                Equipment = updatedEquipment
            };
        }
    }

    private static WorldStack UpdateItem(WorldStack stack, WorldItem item, WorldItem newItem)
    {
        LeaderEquipmentSlot? equipmentSlot = stack.GetEquipmentSlot(item.Id);

        if (equipmentSlot is null)
        {
            return stack with { Inventory = new WorldInventory(stack.Inventory.Items.Replace(item, newItem)) };
        }
        else
        {
            return stack with { Equipment = stack.Equipment.With(equipmentSlot.Value, newItem) };
        }
    }
}
