using Gallean.Common;

namespace Gallean.Engine;

public sealed record WorldTransferInventoryItemCommand(
    WorldUnitGroup Source,
    WorldUnitGroup Target,
    WorldItemId ItemId) : IWorldCommand<Empty>
{
    public WorldCommandResult Execute(WorldState state, IEngineContext context)
    {
        if (ValidateTargets(state) is { } error)
        {
            return WorldCommandResult.Fail(error);
        }

        WorldInventory sourceInventory = WorldUnitGroupCalculator.GetInventory(state, Source);
        if (!sourceInventory.TryGetItem(ItemId, out WorldItem? item))
        {
            return WorldCommandResult.Fail(WorldCommandError.ItemNotFound);
        }

        return WorldCommandResult.Success(
            Empty.Value,
            [new WorldInventoryItemTransferredEvent(Source, Target, item)]);
    }

    private WorldCommandError? ValidateTargets(WorldState state)
    {
        WorldPlan plan = WorldPlan.For(state);

        if (Source is WorldUnitGroup.Stack sourceStackTarget && Target is WorldUnitGroup.Stack targetStackTarget)
        {
            if (!WorldCommandHelpers.TryGetCurrentPlayerStack(state, sourceStackTarget.StackId, out WorldStack? sourceStack))
            {
                return WorldCommandError.StackNotFound;
            }

            if (!WorldCommandHelpers.TryGetCurrentPlayerStack(state, targetStackTarget.StackId, out WorldStack? targetStack))
            {
                return WorldCommandError.StackNotFound;
            }

            if (!sourceStack.Position.IsAdjacentTo(targetStack.Position))
            {
                return WorldCommandError.StackNotAdjacent;
            }

            WorldFortification? sourceFortification = plan.GetFortificationAt(sourceStack.Position);

            if (sourceFortification is not null && FortificationCalculator.IsRioting(sourceFortification))
            {
                return WorldCommandError.CityRioting;
            }

            WorldFortification? targetFortification = plan.GetFortificationAt(targetStack.Position);

            if (targetFortification is not null && FortificationCalculator.IsRioting(targetFortification))
            {
                return WorldCommandError.CityRioting;
            }

            return null;
        }

        WorldUnitGroup.Stack stackTarget;
        WorldUnitGroup.Fortification fortificationTarget;

        switch (Source)
        {
            case WorldUnitGroup.Stack fromStack when Target is WorldUnitGroup.Fortification toFortification:
                stackTarget = fromStack;
                fortificationTarget = toFortification;
                break;
            case WorldUnitGroup.Fortification fromFortification when Target is WorldUnitGroup.Stack toStack:
                stackTarget = toStack;
                fortificationTarget = fromFortification;
                break;
            default:
                return WorldCommandError.StackNotFound;
        }

        if (!WorldCommandHelpers.TryGetCurrentPlayerStack(state, stackTarget.StackId, out WorldStack? stack))
        {
            return WorldCommandError.StackNotFound;
        }

        WorldFortificationId fortificationId = fortificationTarget.FortificationId;

        if (!WorldCommandHelpers.TryGetCurrentPlayerFortification(state, fortificationId, out WorldFortification? fortification))
        {
            return WorldCommandError.StackNotInCity;
        }

        if (plan.GetFortificationAt(stack.Position)?.Id != fortification.Id)
        {
            return WorldCommandError.StackNotInCity;
        }

        if (FortificationCalculator.IsRioting(fortification))
        {
            return WorldCommandError.CityRioting;
        }

        return null;
    }
}
