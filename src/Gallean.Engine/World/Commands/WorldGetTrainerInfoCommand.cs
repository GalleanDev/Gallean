using System.Collections.Immutable;
using Gallean.Common;
using CommandResult = Gallean.Engine.ICommandResult<Gallean.Engine.WorldCommandError, Gallean.Engine.WorldEvent, Gallean.Engine.WorldGetTrainerInfoCommand.Result>;

namespace Gallean.Engine;

public sealed record WorldGetTrainerInfoCommand(WorldStackId StackId) : IWorldCommand<WorldGetTrainerInfoCommand.Result>
{
    public sealed record TrainingUnit(
        WorldUnitId UnitId,
        int CurrentExperience,
        int MaxExperience,
        int AffordableExperience,
        WorldResourceSet CostPerExperience,
        WorldCommandError? Error);

    public sealed record Result(ImmutableArray<TrainingUnit> Units)
    {
        public bool Equals(Result? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Units.SequenceEqual(other.Units);
        }

        public override int GetHashCode()
        {
            HashCode hash = new();

            hash.AddSequence(Units);

            return hash.ToHashCode();
        }
    }

    public CommandResult Execute(WorldState state, IEngineContext context)
    {
        WorldPlayer player = state.CurrentPlayer;

        if (!WorldCommandHelpers.TryGetCurrentPlayerStack(state, StackId, out WorldStack? stack))
        {
            return CommandResult.Fail(WorldCommandError.StackNotFound);
        }

        WorldPlan plan = WorldPlan.For(state);

        if (plan.GetSiteAt(stack.Position) is not WorldTrainerSite)
        {
            return CommandResult.Fail(WorldCommandError.StackNotAtTrainer);
        }

        ImmutableArray<TrainingUnit>.Builder units = ImmutableArray.CreateBuilder<TrainingUnit>();

        foreach ((WorldUnit unit, GridPosition _) in stack.Units.GetUnits())
        {
            units.Add(GetTrainingUnit(unit, player, context));
        }

        return CommandResult.Success(new Result(units.ToImmutable()), []);
    }

    private static TrainingUnit GetTrainingUnit(
        WorldUnit unit,
        WorldPlayer player,
        IEngineContext context)
    {
        TrainingCalculator.TrainingInfo training = TrainingCalculator.GetTrainingInfo(unit, player.Resources, context);

        return new TrainingUnit(
            unit.Id,
            training.CurrentExperience,
            training.MaxExperience,
            training.AffordableExperience,
            training.CostPerExperience,
            training.Error);
    }
}
