using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Gallean.Engine;

internal static class InventoryTransferCalculator
{
    public static bool TryTransferItem(
        WorldState state,
        WorldUnitGroup source,
        WorldUnitGroup target,
        WorldItemId itemId,
        [NotNullWhen(true)] out WorldItem? item,
        [NotNullWhen(false)] out WorldCommandError? error)
    {
        item = null;

        if (!TryGetSourceInventory(state, source, target, out WorldInventory? inventory, out error))
        {
            return false;
        }

        if (!inventory.TryGetItem(itemId, out item))
        {
            error = WorldCommandError.ItemNotFound;
            return false;
        }

        error = null;
        return true;
    }

    public static bool TryTransferInventory(
        WorldState state,
        WorldUnitGroup source,
        WorldUnitGroup target,
        out ImmutableArray<WorldItem> items,
        [NotNullWhen(false)] out WorldCommandError? error)
    {
        items = [];

        if (!TryGetSourceInventory(state, source, target, out WorldInventory? inventory, out error))
        {
            return false;
        }

        if (inventory.Items.IsEmpty)
        {
            error = WorldCommandError.ItemNotFound;
            return false;
        }

        items = inventory.Items;
        error = null;
        return true;
    }

    private static bool TryGetSourceInventory(
        WorldState state,
        WorldUnitGroup source,
        WorldUnitGroup target,
        [NotNullWhen(true)] out WorldInventory? inventory,
        [NotNullWhen(false)] out WorldCommandError? error)
    {
        inventory = null;

        WorldPlan plan = WorldPlan.For(state);

        if (source is WorldUnitGroup.Stack sourceStackTarget && target is WorldUnitGroup.Stack targetStackTarget)
        {
            if (!WorldCommandHelpers.TryGetCurrentPlayerStack(state, sourceStackTarget.StackId, out WorldStack? sourceStack))
            {
                error = WorldCommandError.StackNotFound;
                return false;
            }

            if (!WorldCommandHelpers.TryGetCurrentPlayerStack(state, targetStackTarget.StackId, out WorldStack? targetStack))
            {
                error = WorldCommandError.StackNotFound;
                return false;
            }

            if (!sourceStack.Position.IsAdjacentTo(targetStack.Position))
            {
                error = WorldCommandError.StackNotAdjacent;
                return false;
            }

            WorldFortification? sourceFortification = plan.GetFortificationAt(sourceStack.Position);

            if (sourceFortification is not null && FortificationCalculator.IsRioting(sourceFortification))
            {
                error = WorldCommandError.CityRioting;
                return false;
            }

            WorldFortification? targetFortification = plan.GetFortificationAt(targetStack.Position);

            if (targetFortification is not null && FortificationCalculator.IsRioting(targetFortification))
            {
                error = WorldCommandError.CityRioting;
                return false;
            }

            inventory = sourceStack.Inventory;
            error = null;
            return true;
        }

        WorldUnitGroup.Stack stackTarget;
        WorldUnitGroup.Fortification fortificationTarget;

        switch (source)
        {
            case WorldUnitGroup.Stack fromStack when target is WorldUnitGroup.Fortification toFortification:
                stackTarget = fromStack;
                fortificationTarget = toFortification;
                break;
            case WorldUnitGroup.Fortification fromFortification when target is WorldUnitGroup.Stack toStack:
                stackTarget = toStack;
                fortificationTarget = fromFortification;
                break;
            default:
                error = WorldCommandError.StackNotFound;
                return false;
        }

        if (!WorldCommandHelpers.TryGetCurrentPlayerStack(state, stackTarget.StackId, out WorldStack? stack))
        {
            error = WorldCommandError.StackNotFound;
            return false;
        }

        WorldFortificationId fortificationId = fortificationTarget.FortificationId;

        if (!WorldCommandHelpers.TryGetCurrentPlayerFortification(state, fortificationId, out WorldFortification? fortification))
        {
            error = WorldCommandError.StackNotInCity;
            return false;
        }

        if (plan.GetFortificationAt(stack.Position)?.Id != fortification.Id)
        {
            error = WorldCommandError.StackNotInCity;
            return false;
        }

        if (FortificationCalculator.IsRioting(fortification))
        {
            error = WorldCommandError.CityRioting;
            return false;
        }

        inventory = WorldUnitGroupCalculator.GetInventory(state, source);
        error = null;
        return true;
    }
}
