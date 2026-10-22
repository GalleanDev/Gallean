using Gallean.Common;

namespace Gallean.Engine;

public sealed record WorldExitStackCommand(WorldStackId StackId) : IWorldCommand<Empty>
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
            return WorldCommandResult.Fail(WorldCommandError.StackNotInCity);
        }

        if (FortificationCalculator.IsRioting(fortification))
        {
            return WorldCommandResult.Fail(WorldCommandError.CityRioting);
        }

        WorldPosition? exit = FortificationCalculator.FindExit(fortification, stack, state, context.Definitions);

        if (exit is null)
        {
            return WorldCommandResult.Fail(WorldCommandError.FortificationExitBlocked);
        }

        WorldStackDirection direction = StackStepCalculator.GetDirection(stack.Position, exit.Value);

        return WorldCommandResult.Success(
            Empty.Value,
            [new WorldStackRelocatedEvent(stack.Id, stack.Position, exit.Value, direction)]);
    }
}
