using Gallean.Common;

namespace Gallean.Engine;

public sealed record WorldEquipItemCommand(
    WorldStackId StackId,
    WorldItemId ItemId,
    LeaderEquipmentSlot Slot) : IWorldCommand<Empty>
{
    public WorldCommandResult Execute(WorldState state, IEngineContext context)
    {
        if (!WorldCommandHelpers.TryGetCurrentPlayerStack(state, StackId, out WorldStack? stack))
        {
            return WorldCommandResult.Fail(WorldCommandError.StackNotFound);
        }

        WorldPlan plan = WorldPlan.For(state);
        WorldFortification? fortification = plan.GetFortificationAt(stack.Position);

        if (fortification is not null && FortificationCalculator.IsRioting(fortification))
        {
            return WorldCommandResult.Fail(WorldCommandError.CityRioting);
        }

        LeaderEquipmentSlot? sourceSlot = null;

        if (!stack.Inventory.TryGetItem(ItemId, out WorldItem? worldItem))
        {
            sourceSlot = stack.GetEquipmentSlot(ItemId);

            if (sourceSlot is null)
            {
                return WorldCommandResult.Fail(WorldCommandError.ItemNotFound);
            }

            worldItem = stack.Equipment.Items[sourceSlot.Value];
        }

        ItemDefinition item = context.Definitions.GetItem(worldItem.DefinitionId);

        if (!LeaderEquipmentCalculator.CanPlace(item.Category, Slot))
        {
            return WorldCommandResult.Fail(WorldCommandError.ItemSlotMismatch);
        }

        (WorldUnit leader, _, _) = stack.GetLeader(context.Definitions);

        LeaderAbility? requiredAbility = LeaderEquipmentCalculator.GetRequiredAbility(item.Category);

        if (requiredAbility is not null && !context.UnitStats.GetWorldUnitAbilities(leader).HasFlag(requiredAbility.Value))
        {
            return WorldCommandResult.Fail(WorldCommandError.MissingLeaderAbility);
        }

        if (stack.Equipment[Slot] is not null)
        {
            return WorldCommandResult.Fail(WorldCommandError.EquipmentSlotOccupied);
        }

        WorldItemEquippedEvent equipped = new(StackId, worldItem, Slot);

        if (sourceSlot is not null)
        {
            return WorldCommandResult.Success(Empty.Value, [new WorldItemUnequippedEvent(StackId, sourceSlot.Value), equipped]);
        }
        else
        {
            return WorldCommandResult.Success(Empty.Value, [equipped]);
        }
    }
}
