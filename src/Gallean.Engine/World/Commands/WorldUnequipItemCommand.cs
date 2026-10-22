using Gallean.Common;

namespace Gallean.Engine;

public sealed record WorldUnequipItemCommand(
    WorldStackId StackId,
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

        if (stack.Equipment[Slot] is null)
        {
            return WorldCommandResult.Fail(WorldCommandError.EquipmentSlotEmpty);
        }

        return WorldCommandResult.Success(Empty.Value, [new WorldItemUnequippedEvent(StackId, Slot)]);
    }
}
