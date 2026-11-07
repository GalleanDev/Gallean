using System.Collections.Immutable;
using Gallean.Common;
using CommandResult = Gallean.Engine.ICommandResult<Gallean.Engine.WorldCommandError, Gallean.Engine.WorldEvent, Gallean.Engine.WorldGetRodInfoCommand.Result>;

namespace Gallean.Engine;

public sealed record WorldGetRodInfoCommand(WorldStackId StackId) : IWorldCommand<WorldGetRodInfoCommand.Result>
{
    public sealed record Result(WorldResourceSet Cost, ImmutableArray<WorldPosition> Positions)
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
                   Positions.SequenceEqual(other.Positions);
        }

        public override int GetHashCode()
        {
            HashCode hash = new();

            hash.Add(Cost);
            hash.AddSequence(Positions);

            return hash.ToHashCode();
        }
    }

    public CommandResult Execute(WorldState state, IEngineContext context)
    {
        if (!WorldCommandHelpers.TryGetCurrentPlayerStack(state, StackId, out WorldStack? stack))
        {
            return CommandResult.Fail(WorldCommandError.StackNotFound);
        }

        ImmutableArray<WorldPosition>.Builder positions = ImmutableArray.CreateBuilder<WorldPosition>();
        int range = context.Globals.RodPlantRange;

        for (int y = stack.Position.Y - range; y <= stack.Position.Y + range; y++)
        {
            for (int x = stack.Position.X - range; x <= stack.Position.X + range; x++)
            {
                WorldPosition position = new(x, y);
                WorldCreateRodCommand command = new(StackId, position);
                WorldCommandResult commandResult = command.Execute(state, context);

                if (commandResult.Error is null)
                {
                    positions.Add(position);
                }
            }
        }

        return CommandResult.Success(new Result(context.Globals.RodCost, positions.ToImmutable()), []);
    }
}
