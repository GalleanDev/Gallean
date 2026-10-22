using Gallean.Common;

namespace Gallean.Engine;

public sealed record WorldUseItemCommand(
    WorldStackId StackId,
    WorldItemId ItemId,
    WorldItemTarget? Target) : IWorldCommand<Empty>
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

        if (!stack.Inventory.TryGetItem(ItemId, out WorldItem? worldItem))
        {
            LeaderEquipmentSlot? equipmentSlot = stack.GetEquipmentSlot(ItemId);

            if (equipmentSlot is null)
            {
                return WorldCommandResult.Fail(WorldCommandError.ItemNotFound);
            }

            worldItem = stack.Equipment.Items[equipmentSlot.Value];
        }

        ItemDefinition item = context.Definitions.GetItem(worldItem.DefinitionId);

        if (item.Effect is null)
        {
            return WorldCommandResult.Fail(WorldCommandError.ItemNotUsable);
        }

        if (LeaderEquipmentCalculator.GetRequiredAbility(item.Category) is { } requiredAbility)
        {
            (WorldUnit leader, _, _) = stack.GetLeader(context.Definitions);

            if (!leader.IsAlive)
            {
                return WorldCommandResult.Fail(WorldCommandError.UnitDead);
            }

            if (!context.UnitStats.GetWorldUnitAbilities(leader).HasFlag(requiredAbility))
            {
                return WorldCommandResult.Fail(WorldCommandError.MissingLeaderAbility);
            }
        }

        WorldItemResolver.Context resolution = new(state, context, stack, worldItem, Target);
        WorldCommandResult effectResult = item.Effect.Accept(WorldItemResolver.Instance, resolution);

        if (effectResult.Error is not null)
        {
            return effectResult;
        }

        return WorldCommandResult.Success(
            Empty.Value,
            [
                new WorldItemUsedEvent(
                    StackId,
                    worldItem,
                    RemainingCharges: null,
                    LeaderEquipmentCalculator.IsConsumable(item.Category)),
                .. effectResult.Events
            ]);
    }
}
