using Gallean.Common;

namespace Gallean.Engine;

public sealed record WorldPurchaseItemCommand(
    WorldStackId StackId,
    WorldSiteId SiteId,
    ItemDefinitionId ItemId) : IWorldCommand<Empty>
{
    public WorldCommandResult Execute(WorldState state, IEngineContext context)
    {
        WorldPlayer player = state.CurrentPlayer;

        if (!WorldCommandHelpers.TryGetCurrentPlayerStack(state, StackId, out WorldStack? stack))
        {
            return WorldCommandResult.Fail(WorldCommandError.StackNotFound);
        }

        if (!state.Sites.TryGetValue(SiteId, out WorldSite? site))
        {
            return WorldCommandResult.Fail(WorldCommandError.SiteNotFound);
        }

        if (site is not WorldMerchantSite merchantSite)
        {
            return WorldCommandResult.Fail(WorldCommandError.SiteNotFound);
        }

        if (stack.Position != merchantSite.Entrance)
        {
            return WorldCommandResult.Fail(WorldCommandError.StackNotAtSite);
        }

        MerchantSiteCalculator.PurchaseInfo purchase =
            MerchantSiteCalculator.GetPurchaseInfo(merchantSite, stack, player, ItemId, context);

        if (purchase.Error is { } error)
        {
            return WorldCommandResult.Fail(error);
        }

        WorldItemId purchasedItemId = WorldCommandHelpers.GenerateItemId(state);
        WorldItem purchasedItem = WorldItemFactory.Create(purchasedItemId, context.Definitions.GetItem(ItemId), context.Globals);

        return WorldCommandResult.Success(
            Empty.Value,
            [
                new WorldItemPurchasedEvent(
                    SiteId,
                    StackId,
                    purchasedItem,
                    purchase.Cost)
            ]);
    }
}
