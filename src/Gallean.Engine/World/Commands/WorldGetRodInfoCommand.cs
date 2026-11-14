using System.Collections.Immutable;
using Gallean.Common;
using CommandResult = Gallean.Engine.ICommandResult<Gallean.Engine.WorldCommandError, Gallean.Engine.WorldEvent, Gallean.Engine.WorldGetRodInfoCommand.Result>;

namespace Gallean.Engine;

public sealed record WorldGetRodInfoCommand(WorldStackId StackId) : IWorldCommand<WorldGetRodInfoCommand.Result>
{
    public sealed record Result(
        WorldResourceSet Cost,
        ImmutableArray<WorldPosition> PlantPositions,
        ImmutableArray<WorldPosition> RemovePositions)
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

            return Cost == other.Cost &&
                   PlantPositions.SequenceEqual(other.PlantPositions) &&
                   RemovePositions.SequenceEqual(other.RemovePositions);
        }

        public override int GetHashCode()
        {
            HashCode hash = new();

            hash.Add(Cost);
            hash.AddSequence(PlantPositions);
            hash.AddSequence(RemovePositions);

            return hash.ToHashCode();
        }
    }

    public CommandResult Execute(WorldState state, IEngineContext context)
    {
        if (!WorldCommandHelpers.TryGetCurrentPlayerStack(state, StackId, out WorldStack? stack))
        {
            return CommandResult.Fail(WorldCommandError.StackNotFound);
        }

        ImmutableArray<WorldPosition>.Builder plantPositions = ImmutableArray.CreateBuilder<WorldPosition>();
        int range = context.Globals.RodPlantRange;

        for (int y = stack.Position.Y - range; y <= stack.Position.Y + range; y++)
        {
            for (int x = stack.Position.X - range; x <= stack.Position.X + range; x++)
            {
                WorldPosition position = new(x, y);
                WorldCreateRodCommand createCommand = new(StackId, position);
                WorldCommandResult createResult = createCommand.Execute(state, context);

                if (createResult.Error is null)
                {
                    plantPositions.Add(position);
                }
            }
        }

        ImmutableArray<WorldPosition>.Builder removePositions = ImmutableArray.CreateBuilder<WorldPosition>();

        foreach (WorldRod rod in state.Rods.Values)
        {
            WorldRemoveRodCommand removeCommand = new(StackId, rod.Position);
            WorldCommandResult removeResult = removeCommand.Execute(state, context);

            if (removeResult.Error is null)
            {
                removePositions.Add(rod.Position);
            }
        }

        return CommandResult.Success(
            new Result(context.Globals.RodCost, plantPositions.ToImmutable(), removePositions.ToImmutable()),
            []);
    }
}
