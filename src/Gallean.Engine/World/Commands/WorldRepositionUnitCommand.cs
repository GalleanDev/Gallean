using Gallean.Common;

namespace Gallean.Engine;

public sealed record WorldRepositionUnitCommand(
    WorldUnitGroup Group,
    WorldUnitId UnitId,
    GridPosition TargetPosition) : IWorldCommand<Empty>
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

        if (!WorldUnitGroupCalculator.TryRepositionUnit(state, context, Group, UnitId, TargetPosition, out _, out WorldCommandError? error))
        {
            return WorldCommandResult.Fail(error.Value);
        }

        return WorldCommandResult.Success(
            Empty.Value,
            [new WorldUnitRepositionedEvent(Group, UnitId, TargetPosition)]);
    }
}
