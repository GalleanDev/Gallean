namespace Gallean.Engine;

internal sealed class StealItemThiefActionResolver(WorldThiefStealItemAction action) : AbstractThiefActionResolver(action.TargetPosition)
{
    protected override AttemptResult AttemptAction(
        WorldState state,
        IEngineContext context,
        WorldStack thief,
        WorldPlayerId player,
        ThiefTarget target)
    {
        if (target is ThiefStackTarget stackTarget)
        {
            WorldStack stack = stackTarget.Stack;

            WorldItem? item = FindItem(stack.Inventory, action.ItemId);

            if (item is null)
            {
                return AttemptResult.Fail(WorldCommandError.ItemNotFound);
            }

            return AttemptResult.Success(
                WorldThiefActionType.StealItem,
                stack.OwnerId,
                [
                    new WorldInventoryItemTransferredEvent(
                        new WorldUnitGroup.Stack(stack.Id),
                        new WorldUnitGroup.Stack(thief.Id),
                        item)
                ]);
        }
        else if (target is ThiefMerchantSiteTarget merchantTarget)
        {
            WorldMerchantSite merchantSite = merchantTarget.Site;

            if (!merchantSite.Contains(action.ItemId))
            {
                return AttemptResult.Fail(WorldCommandError.ItemNotAvailable);
            }

            WorldItemId stolenItemId = WorldCommandHelpers.GenerateItemId(state);
            WorldItem stolenItem = WorldItemFactory.Create(stolenItemId, context.Definitions.GetItem(action.ItemId), context.Globals);

            return AttemptResult.Success(
                WorldThiefActionType.StealMerchant,
                merchantTarget.OwnerId,
                [new WorldMerchantItemStolenEvent(thief.Id, merchantSite.Id, stolenItem)]);
        }
        else
        {
            return AttemptResult.Fail(WorldCommandError.ThiefTargetInvalid);
        }
    }

    private static WorldItem? FindItem(WorldInventory inventory, ItemDefinitionId itemId)
    {
        foreach (WorldItem item in inventory.Items)
        {
            if (item.DefinitionId == itemId)
            {
                return item;
            }
        }

        return null;
    }
}
