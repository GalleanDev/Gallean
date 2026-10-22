using Gallean.Common;

namespace Gallean.Engine;

public sealed record WorldTransferUnitCommand(
    WorldUnitGroup Source,
    WorldUnitGroup Target,
    WorldUnitId UnitId,
    GridPosition TargetPosition) : IWorldCommand<Empty>
{
    public WorldCommandResult Execute(WorldState state, IEngineContext context)
    {
        WorldPlan plan = WorldPlan.For(state);

        switch (Source, Target)
        {
            case (WorldUnitGroup.Stack sourceGroup, WorldUnitGroup.Stack targetGroup):
                {
                    if (!WorldCommandHelpers.TryGetCurrentPlayerStack(state, sourceGroup.StackId, out WorldStack? sourceStack))
                    {
                        return WorldCommandResult.Fail(WorldCommandError.StackNotFound);
                    }

                    if (!WorldCommandHelpers.TryGetCurrentPlayerStack(state, targetGroup.StackId, out WorldStack? targetStack))
                    {
                        return WorldCommandResult.Fail(WorldCommandError.StackNotFound);
                    }

                    if (!sourceStack.Position.IsAdjacentTo(targetStack.Position))
                    {
                        return WorldCommandResult.Fail(WorldCommandError.StackNotAdjacent);
                    }

                    WorldFortification? sourceFortification = plan.GetFortificationAt(sourceStack.Position);

                    if (sourceFortification is not null && FortificationCalculator.IsRioting(sourceFortification))
                    {
                        return WorldCommandResult.Fail(WorldCommandError.CityRioting);
                    }

                    WorldFortification? targetFortification = plan.GetFortificationAt(targetStack.Position);

                    if (targetFortification is not null && FortificationCalculator.IsRioting(targetFortification))
                    {
                        return WorldCommandResult.Fail(WorldCommandError.CityRioting);
                    }

                    break;
                }

            case (WorldUnitGroup.Stack visitingGroup, WorldUnitGroup.Fortification garrisonGroup):
                {
                    if (!WorldCommandHelpers.TryGetCurrentPlayerStack(state, visitingGroup.StackId, out WorldStack? visitingStack))
                    {
                        return WorldCommandResult.Fail(WorldCommandError.StackNotFound);
                    }

                    WorldFortificationId fortificationId = garrisonGroup.FortificationId;

                    if (!WorldCommandHelpers.TryGetCurrentPlayerFortification(state, fortificationId, out WorldFortification? fortification))
                    {
                        return WorldCommandResult.Fail(WorldCommandError.GarrisonNotFound);
                    }

                    if (plan.GetFortificationAt(visitingStack.Position)?.Id != fortification.Id)
                    {
                        return WorldCommandResult.Fail(WorldCommandError.GarrisonNotFound);
                    }

                    if (FortificationCalculator.IsRioting(fortification))
                    {
                        return WorldCommandResult.Fail(WorldCommandError.CityRioting);
                    }

                    break;
                }

            case (WorldUnitGroup.Fortification garrisonGroup, WorldUnitGroup.Stack visitingGroup):
                {
                    if (!WorldCommandHelpers.TryGetCurrentPlayerStack(state, visitingGroup.StackId, out WorldStack? visitingStack))
                    {
                        return WorldCommandResult.Fail(WorldCommandError.StackNotFound);
                    }

                    WorldFortificationId fortificationId = garrisonGroup.FortificationId;

                    if (!WorldCommandHelpers.TryGetCurrentPlayerFortification(state, fortificationId, out WorldFortification? fortification))
                    {
                        return WorldCommandResult.Fail(WorldCommandError.GarrisonNotFound);
                    }

                    if (plan.GetFortificationAt(visitingStack.Position)?.Id != fortification.Id)
                    {
                        return WorldCommandResult.Fail(WorldCommandError.GarrisonNotFound);
                    }

                    if (FortificationCalculator.IsRioting(fortification))
                    {
                        return WorldCommandResult.Fail(WorldCommandError.CityRioting);
                    }

                    break;
                }

            case (WorldUnitGroup.Fortification, WorldUnitGroup.Fortification):
                return WorldCommandResult.Fail(WorldCommandError.StackNotFound);

            default:
                return WorldCommandResult.Fail(WorldCommandError.StackNotFound);
        }

        if (!WorldUnitGroupCalculator.TryTransferUnit(
            state,
            context,
            Source,
            Target,
            UnitId,
            TargetPosition,
            out _,
            out WorldCommandError? error))
        {
            return WorldCommandResult.Fail(error.Value);
        }

        return WorldCommandResult.Success(
            Empty.Value,
            [new WorldUnitTransferredEvent(Source, Target, UnitId, TargetPosition)]);
    }
}
