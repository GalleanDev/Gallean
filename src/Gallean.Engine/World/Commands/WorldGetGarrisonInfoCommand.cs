using System.Collections.Immutable;
using Gallean.Common;
using CommandResult = Gallean.Engine.ICommandResult<Gallean.Engine.WorldCommandError, Gallean.Engine.WorldEvent, Gallean.Engine.WorldGetGarrisonInfoCommand.Result>;

namespace Gallean.Engine;

public sealed record WorldGetGarrisonInfoCommand(WorldFortificationId FortificationId) : IWorldCommand<WorldGetGarrisonInfoCommand.Result>
{
    public sealed record Result(ImmutableArray<WorldUnitActionInfo> Units, int OccupiedCapacity, int TotalCapacity)
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
                   OccupiedCapacity == other.OccupiedCapacity &&
                   TotalCapacity == other.TotalCapacity;
        }

        public override int GetHashCode()
        {
            HashCode hash = new();

            hash.AddSequence(Units);
            hash.Add(OccupiedCapacity);
            hash.Add(TotalCapacity);

            return hash.ToHashCode();
        }
    }

    public CommandResult Execute(WorldState state, IEngineContext context)
    {
        if (!WorldCommandHelpers.TryGetCurrentPlayerFortification(state, FortificationId, out WorldFortification? fortification))
        {
            return CommandResult.Fail(WorldCommandError.FortificationNotFound);
        }

        WorldUnitGroup group = new WorldUnitGroup.Fortification(FortificationId);
        ImmutableArray<WorldUnitActionInfo>.Builder units = ImmutableArray.CreateBuilder<WorldUnitActionInfo>();

        foreach ((WorldUnit unit, GridPosition _) in fortification.Garrison.GetUnits())
        {
            WorldCommandResult healResult = new WorldHealUnitCommand(group, unit.Id).Execute(state, context);
            WorldCommandResult resurrectResult = new WorldResurrectUnitCommand(group, unit.Id).Execute(state, context);
            WorldCommandResult disbandResult = new WorldDisbandUnitCommand(group, unit.Id).Execute(state, context);
            WorldCommandResult lockTypeResult = new WorldLockUnitTypeCommand(group, unit.Id).Execute(state, context);

            units.Add(new WorldUnitActionInfo(unit.Id, healResult.Error, resurrectResult.Error, disbandResult.Error, lockTypeResult.Error));
        }

        int occupiedCapacity = GarrisonCalculator.GetOccupiedCapacity(fortification.Garrison, context.Definitions);
        int totalCapacity = GarrisonCalculator.GetTotalCapacity(fortification, context.Globals);

        return CommandResult.Success(new Result(units.ToImmutable(), occupiedCapacity, totalCapacity), []);
    }
}
