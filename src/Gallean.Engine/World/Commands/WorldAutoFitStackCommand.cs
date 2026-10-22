using Gallean.Common;

namespace Gallean.Engine;

public sealed record WorldAutoFitStackCommand(WorldStackId StackId) : IWorldCommand<Empty>
{
    public WorldCommandResult Execute(WorldState state, IEngineContext context)
    {
        if (!WorldCommandHelpers.TryGetCurrentPlayerStack(state, StackId, out WorldStack? stack))
        {
            return WorldCommandResult.Fail(WorldCommandError.StackNotFound);
        }

        WorldPlan plan = WorldPlan.For(state);
        WorldFortification? fortification = plan.GetFortificationAt(stack.Position);

        if (fortification is not (WorldCity or WorldCapital) ||
            FortificationCalculator.GetOwner(fortification) != stack.OwnerId)
        {
            return WorldCommandResult.Fail(WorldCommandError.GarrisonNotFound);
        }

        if (FortificationCalculator.IsRioting(fortification))
        {
            return WorldCommandResult.Fail(WorldCommandError.CityRioting);
        }

        UnitGrid<WorldUnit> garrisonUnits = fortification.Garrison;
        WorldInventory garrisonInventory = fortification.Inventory;

        StackAutoFitCalculator.Result result = StackAutoFitCalculator.Calculate(
            stack.Units,
            fortification.Garrison,
            stack.Inventory,
            fortification.Inventory,
            stack.Equipment,
            GarrisonCalculator.GetTotalCapacity(fortification, context.Globals),
            context.Definitions);

        if (result.StackUnits == stack.Units &&
            result.GarrisonUnits == garrisonUnits &&
            result.StackInventory == stack.Inventory &&
            result.GarrisonInventory == garrisonInventory &&
            result.Equipment == stack.Equipment)
        {
            return WorldCommandResult.Success(Empty.Value, []);
        }

        return WorldCommandResult.Success(
            Empty.Value,
            [
                new WorldStackAutoFittedEvent(
                    StackId,
                    result.StackUnits,
                    result.GarrisonUnits,
                    result.StackInventory,
                    result.GarrisonInventory,
                    result.Equipment)
            ]);
    }
}
