using Gallean.Common;

namespace Gallean.Engine;

public sealed record WorldLockUnitTypeCommand(
    WorldUnitGroup Group,
    WorldUnitId UnitId) : IWorldCommand<Empty>
{
    public WorldCommandResult Execute(WorldState state, IEngineContext context)
    {
        return Group switch
        {
            WorldUnitGroup.Stack stackGroup => ExecuteForStack(state, context, stackGroup),
            WorldUnitGroup.Fortification fortificationGroup => ExecuteForFortification(state, context, fortificationGroup)
        };
    }

    private WorldCommandResult ExecuteForStack(WorldState state, IEngineContext context, WorldUnitGroup.Stack stackGroup)
    {
        if (!WorldCommandHelpers.TryGetCurrentPlayerStack(state, stackGroup.StackId, out _))
        {
            return WorldCommandResult.Fail(WorldCommandError.StackNotFound);
        }

        return Lock(state, context);
    }

    private WorldCommandResult ExecuteForFortification(WorldState state, IEngineContext context, WorldUnitGroup.Fortification fortificationGroup)
    {
        if (!WorldCommandHelpers.TryGetCurrentPlayerFortification(state, fortificationGroup.FortificationId, out _))
        {
            return WorldCommandResult.Fail(WorldCommandError.GarrisonNotFound);
        }

        return Lock(state, context);
    }

    private WorldCommandResult Lock(WorldState state, IEngineContext context)
    {
        UnitGrid<WorldUnit> units = WorldUnitGroupCalculator.GetUnits(state, Group);

        if (!units.TryGetUnit(UnitId, out (WorldUnit Unit, GridPosition Position) entry))
        {
            return WorldCommandResult.Fail(WorldCommandError.UnitNotFound);
        }

        UnitUpgrade upgrade = UnitUpgradeCalculator.GetUpgrade(
            entry.Unit.DefinitionId,
            entry.Unit.IsTypeLocked,
            state.CurrentPlayer.Buildings,
            context.Definitions);

        if (upgrade is UnitUpgrade.DynamicUpgrade)
        {
            return WorldCommandResult.Fail(WorldCommandError.UnitTypeLockForbidden);
        }

        return WorldCommandResult.Success(
            Empty.Value,
            [new WorldUnitTypeLockedEvent(Group, UnitId)]);
    }
}
