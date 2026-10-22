using Gallean.Common;

namespace Gallean.Engine;

public sealed record WorldSellItemCommand(
    WorldStackId StackId,
    WorldSiteId SiteId,
    WorldItemId ItemId) : IWorldCommand<Empty>
{
    public WorldCommandResult Execute(WorldState state, IEngineContext context)
    {
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

        if (!stack.Inventory.TryGetItem(ItemId, out WorldItem? item))
        {
            return WorldCommandResult.Fail(WorldCommandError.ItemNotFound);
        }

        MerchantSiteCalculator.SellInfo sell = MerchantSiteCalculator.GetSellInfo(item, context);

        if (sell.Error is { } error)
        {
            return WorldCommandResult.Fail(error);
        }

        return WorldCommandResult.Success(
            Empty.Value, [new WorldItemSoldEvent(SiteId, StackId, item, sell.Value)]);
    }
}
