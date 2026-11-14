namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldItemUsedEvent ev, WorldState state)
    {
        if (ev.Consumed)
        {
            return RemoveItem(state, ev.Group, ev.Item);
        }
        else if (ev.RemainingCharges is { } remainingCharges)
        {
            return UpdateItem(state, ev.Group, ev.Item, ev.Item with { Charges = remainingCharges });
        }
        else
        {
            WorldPlayerId ownerId = WorldUnitGroupCalculator.GetOwner(state, ev.Group)!.Value;
            WorldPlayer player = state.Players[ownerId];
            WorldPlayerTurnActions actions = player.TurnActions;
            int uses = actions.ItemUses.GetValueOrDefault(ev.Item.Id);
            WorldPlayerTurnActions updatedActions = actions with { ItemUses = actions.ItemUses.SetItem(ev.Item.Id, uses + 1) };

            return state with { Players = state.Players.SetItem(ownerId, player with { TurnActions = updatedActions }) };
        }
    }

    private WorldState RemoveItem(WorldState state, WorldUnitGroup group, WorldItem item)
    {
        return group switch
        {
            WorldUnitGroup.Stack data => RemoveStackItem(state, state.Stacks[data.StackId], item),
            WorldUnitGroup.Fortification data => RemoveFortificationItem(state, state.Fortifications[data.FortificationId], item)
        };
    }

    private static WorldState UpdateItem(WorldState state, WorldUnitGroup group, WorldItem item, WorldItem newItem)
    {
        return group switch
        {
            WorldUnitGroup.Stack data => UpdateStackItem(state, state.Stacks[data.StackId], item, newItem),
            WorldUnitGroup.Fortification data => UpdateFortificationItem(state, state.Fortifications[data.FortificationId], item, newItem)
        };
    }

    private WorldState RemoveStackItem(WorldState state, WorldStack stack, WorldItem item)
    {
        LeaderEquipmentSlot? equipmentSlot = stack.GetEquipmentSlot(item.Id);
        WorldStack updatedStack;

        if (equipmentSlot is null)
        {
            updatedStack = stack with { Inventory = stack.Inventory.Remove(item) };
        }
        else
        {
            LeaderEquipment updatedEquipment = stack.Equipment.Without(equipmentSlot.Value);

            updatedStack = stack with
            {
                Units = LeaderEquipmentCalculator.UpdateUnitModifiers(stack.Units, updatedEquipment, Context.Definitions),
                Equipment = updatedEquipment
            };
        }

        return state with { Stacks = state.Stacks.SetItem(stack.Id, updatedStack) };
    }

    private static WorldState RemoveFortificationItem(WorldState state, WorldFortification fortification, WorldItem item)
    {
        WorldFortification updatedFortification = fortification with { Inventory = fortification.Inventory.Remove(item) };

        return state with { Fortifications = state.Fortifications.SetItem(fortification.Id, updatedFortification) };
    }

    private static WorldState UpdateStackItem(WorldState state, WorldStack stack, WorldItem item, WorldItem newItem)
    {
        LeaderEquipmentSlot? equipmentSlot = stack.GetEquipmentSlot(item.Id);
        WorldStack updatedStack;

        if (equipmentSlot is null)
        {
            updatedStack = stack with { Inventory = new WorldInventory(stack.Inventory.Items.Replace(item, newItem)) };
        }
        else
        {
            updatedStack = stack with { Equipment = stack.Equipment.With(equipmentSlot.Value, newItem) };
        }

        return state with { Stacks = state.Stacks.SetItem(stack.Id, updatedStack) };
    }

    private static WorldState UpdateFortificationItem(WorldState state, WorldFortification fortification, WorldItem item, WorldItem newItem)
    {
        WorldInventory updatedInventory = new(fortification.Inventory.Items.Replace(item, newItem));
        WorldFortification updatedFortification = fortification with { Inventory = updatedInventory };

        return state with { Fortifications = state.Fortifications.SetItem(fortification.Id, updatedFortification) };
    }
}
