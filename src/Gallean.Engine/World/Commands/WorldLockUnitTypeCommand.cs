using Gallean.Common;

namespace Gallean.Engine;

public sealed record WorldLockUnitTypeCommand(
    WorldUnitGroup Group,
    WorldUnitId UnitId) : IWorldCommand<Empty>
{
    public WorldCommandResult Execute(WorldState state, IEngineContext context)
    {
        if (!WorldUnitGroupCalculator.TryGetCity(state, Group, out _, out WorldCommandError? cityError))
        {
            return WorldCommandResult.Fail(cityError.Value);
        }

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
