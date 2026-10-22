using Gallean.Common;

namespace Gallean.Engine;

public sealed record WorldTrainUnitCommand(
    WorldStackId StackId,
    WorldUnitId UnitId) : IWorldCommand<Empty>
{
    public WorldCommandResult Execute(WorldState state, IEngineContext context)
    {
        if (!WorldCommandHelpers.TryGetCurrentPlayerStack(state, StackId, out WorldStack? stack))
        {
            return WorldCommandResult.Fail(WorldCommandError.StackNotFound);
        }

        WorldPlan plan = WorldPlan.For(state);

        if (plan.GetSiteAt(stack.Position) is not WorldTrainerSite)
        {
            return WorldCommandResult.Fail(WorldCommandError.StackNotAtTrainer);
        }

        if (!stack.Units.TryGetUnit(UnitId, out (WorldUnit Unit, GridPosition Position) entry))
        {
            return WorldCommandResult.Fail(WorldCommandError.UnitNotFound);
        }

        WorldPlayer player = state.CurrentPlayer;
        TrainingCalculator.TrainingInfo training = TrainingCalculator.GetTrainingInfo(entry.Unit, player.Resources, context);

        if (training.Error is { } error)
        {
            return WorldCommandResult.Fail(error);
        }

        int experience = Math.Min(training.MaxExperience - training.CurrentExperience, training.AffordableExperience);

        return WorldCommandResult.Success(
            Empty.Value,
            [
                new WorldStackUnitTrainedEvent(
                    StackId,
                    UnitId,
                    training.CurrentExperience + experience,
                    ResourceSetCalculator.Multiply(training.CostPerExperience, experience))
            ]);
    }
}
