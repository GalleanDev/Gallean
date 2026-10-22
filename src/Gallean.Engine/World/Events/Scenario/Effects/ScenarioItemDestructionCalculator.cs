using System.Collections.Immutable;

namespace Gallean.Engine;

internal static class ScenarioItemDestructionCalculator
{
    public static WorldState DestroyInStack(
        WorldState state,
        ItemDefinitionId item,
        WorldStackId stackId,
        IGameDefinitionProvider definitions)
    {
        if (!state.Stacks.TryGetValue(stackId, out WorldStack? stack))
        {
            return state;
        }

        return state with { Stacks = state.Stacks.SetItem(stackId, WithoutItem(stack, item, definitions)) };
    }

    public static WorldState DestroyInWorld(WorldState state, ItemDefinitionId item, IGameDefinitionProvider definitions)
    {
        ImmutableSortedDictionary<WorldStackId, WorldStack>.Builder stacks = state.Stacks.ToBuilder();

        foreach (WorldStack stack in state.Stacks.Values)
        {
            stacks[stack.Id] = WithoutItem(stack, item, definitions);
        }

        ImmutableSortedDictionary<WorldFortificationId, WorldFortification>.Builder fortifications =
            state.Fortifications.ToBuilder();

        foreach (WorldFortification fortification in state.Fortifications.Values)
        {
            WorldInventory inventory = WithoutItem(fortification.Inventory, item);

            if (inventory != fortification.Inventory)
            {
                fortifications[fortification.Id] = fortification with { Inventory = inventory };
            }
        }

        ImmutableSortedDictionary<WorldBagId, WorldBag>.Builder bags = state.Bags.ToBuilder();

        foreach (WorldBag bag in state.Bags.Values)
        {
            WorldInventory inventory = WithoutItem(bag.Inventory, item);

            if (inventory.Items.IsEmpty)
            {
                bags.Remove(bag.Id);
            }
            else if (inventory != bag.Inventory)
            {
                bags[bag.Id] = bag with { Inventory = inventory };
            }
        }

        return state with
        {
            Stacks = stacks.ToImmutable(),
            Fortifications = fortifications.ToImmutable(),
            Bags = bags.ToImmutable()
        };
    }

    private static WorldStack WithoutItem(WorldStack stack, ItemDefinitionId item, IGameDefinitionProvider definitions)
    {
        WorldInventory inventory = WithoutItem(stack.Inventory, item);
        LeaderEquipment equipment = stack.Equipment;

        foreach ((LeaderEquipmentSlot slot, WorldItem equippedItem) in stack.Equipment.Items)
        {
            if (equippedItem.DefinitionId == item)
            {
                equipment = equipment.Without(slot);
            }
        }

        if (inventory != stack.Inventory || equipment != stack.Equipment)
        {
            return stack with
            {
                Units = LeaderEquipmentCalculator.UpdateUnitModifiers(stack.Units, equipment, definitions),
                Inventory = inventory,
                Equipment = equipment
            };
        }
        else
        {
            return stack;
        }
    }

    private static WorldInventory WithoutItem(
        WorldInventory inventory,
        ItemDefinitionId item)
    {
        ImmutableArray<WorldItem>.Builder result = ImmutableArray.CreateBuilder<WorldItem>();

        foreach (WorldItem candidate in inventory.Items)
        {
            if (candidate.DefinitionId != item)
            {
                result.Add(candidate);
            }
        }

        return new WorldInventory(result.ToImmutable());
    }
}
