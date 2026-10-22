using System.Collections.Immutable;
using Gallean.Common;
using CommandResult = Gallean.Engine.ICommandResult<Gallean.Engine.WorldCommandError, Gallean.Engine.WorldEvent, Gallean.Engine.WorldGetStackInfoCommand.Result>;

namespace Gallean.Engine;

public sealed record WorldGetStackInfoCommand(WorldStackId StackId) : IWorldCommand<WorldGetStackInfoCommand.Result>
{
    public sealed record Result(ImmutableArray<WorldUnitActionInfo> Units, int OccupiedLeadership, int TotalLeadership)
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

            return Units.SequenceEqual(other.Units) &&
                   OccupiedLeadership == other.OccupiedLeadership &&
                   TotalLeadership == other.TotalLeadership;
        }

        public override int GetHashCode()
        {
            HashCode hash = new();

            hash.AddSequence(Units);
            hash.Add(OccupiedLeadership);
            hash.Add(TotalLeadership);

            return hash.ToHashCode();
        }
    }

    public CommandResult Execute(WorldState state, IEngineContext context)
    {
        if (!WorldCommandHelpers.TryGetCurrentPlayerStack(state, StackId, out WorldStack? stack))
        {
            return CommandResult.Fail(WorldCommandError.StackNotFound);
        }

        WorldUnitGroup group = new WorldUnitGroup.Stack(StackId);
        ImmutableArray<WorldUnitActionInfo>.Builder units = ImmutableArray.CreateBuilder<WorldUnitActionInfo>();

        foreach ((WorldUnit unit, GridPosition _) in stack.Units.GetUnits())
        {
            WorldCommandResult healResult = new WorldHealUnitCommand(group, unit.Id).Execute(state, context);
            WorldCommandResult resurrectResult = new WorldResurrectUnitCommand(group, unit.Id).Execute(state, context);
            WorldCommandResult disbandResult = new WorldDisbandUnitCommand(group, unit.Id).Execute(state, context);
            WorldCommandResult lockTypeResult = new WorldLockUnitTypeCommand(group, unit.Id).Execute(state, context);

            units.Add(new WorldUnitActionInfo(unit.Id, healResult.Error, resurrectResult.Error, disbandResult.Error, lockTypeResult.Error));
        }

        int occupiedLeadership = StackCalculator.GetOccupiedLeadership(stack.Units, context.Definitions);
        int totalLeadership = StackCalculator.GetTotalLeadership(stack.Units, context.Definitions);

        return CommandResult.Success(new Result(units.ToImmutable(), occupiedLeadership, totalLeadership), []);
    }
}
