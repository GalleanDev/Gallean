using Gallean.Common;

namespace Gallean.Engine;

public sealed record WorldDisbandUnitCommand(
    WorldUnitGroup Group,
    WorldUnitId UnitId) : IWorldCommand<Empty>
{
    public WorldCommandResult Execute(WorldState state, IEngineContext context)
    {
        if (Group is WorldUnitGroup.Stack stackGroup)
        {
            if (!WorldCommandHelpers.TryGetCurrentPlayerStack(state, stackGroup.StackId, out WorldStack? stack))
            {
                return WorldCommandResult.Fail(WorldCommandError.StackNotFound);
            }

            WorldPlan plan = WorldPlan.For(state);
            WorldFortification? fortification = plan.GetFortificationAt(stack.Position);

            if (fortification is not null && FortificationCalculator.IsRioting(fortification))
            {
                return WorldCommandResult.Fail(WorldCommandError.CityRioting);
            }
        }
        else if (Group is WorldUnitGroup.Fortification fortificationGroup)
        {
            WorldFortificationId id = fortificationGroup.FortificationId;

            if (!WorldCommandHelpers.TryGetCurrentPlayerFortification(state, id, out WorldFortification? fortification))
            {
                return WorldCommandResult.Fail(WorldCommandError.GarrisonNotFound);
            }

            if (fortification is not (WorldCity or WorldCapital))
            {
                return WorldCommandResult.Fail(WorldCommandError.GarrisonNotFound);
            }

            if (FortificationCalculator.IsRioting(fortification))
            {
                return WorldCommandResult.Fail(WorldCommandError.CityRioting);
            }
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(Group));
        }

        UnitGrid<WorldUnit> units = WorldUnitGroupCalculator.GetUnits(state, Group);

        if (!units.TryGetUnit(UnitId, out (WorldUnit Unit, GridPosition Position) entry))
        {
            return WorldCommandResult.Fail(WorldCommandError.UnitNotFound);
        }

        UnitDefinition definition = context.Definitions.GetUnit(entry.Unit.DefinitionId);

        if (definition.Leadership is > 0)
        {
            return WorldCommandResult.Fail(WorldCommandError.LeaderDisbandForbidden);
        }

        if (definition.Category == UnitCategory.Guardian)
        {
            return WorldCommandResult.Fail(WorldCommandError.GuardianDisbandForbidden);
        }

        return WorldCommandResult.Success(
            Empty.Value,
            [new WorldUnitDisbandedEvent(Group, UnitId)]);
    }
}
