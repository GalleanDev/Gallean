using System.Collections.Immutable;
using Gallean.Common;
using CommandResult = Gallean.Engine.ICommandResult<Gallean.Engine.WorldCommandError, Gallean.Engine.WorldEvent, Gallean.Engine.WorldGetMerchantInfoCommand.Result>;

namespace Gallean.Engine;

public sealed record WorldGetMerchantInfoCommand(
    WorldStackId StackId,
    WorldSiteId SiteId) : IWorldCommand<WorldGetMerchantInfoCommand.Result>
{
    public sealed record PurchaseItem(
        ItemDefinitionId ItemId,
        int Count,
        WorldResourceSet Cost,
        WorldCommandError? Error);

    public sealed record SellItem(
        WorldItem Item,
        WorldResourceSet Value);

    public sealed record Result(
        ImmutableArray<PurchaseItem> PurchaseItems,
        ImmutableArray<SellItem> SellItems)
    {
        public bool Equals(Result? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return PurchaseItems.SequenceEqual(other.PurchaseItems) &&
                   SellItems.SequenceEqual(other.SellItems);
        }

        public override int GetHashCode()
        {
            HashCode hash = new();

            hash.AddSequence(PurchaseItems);
            hash.AddSequence(SellItems);

            return hash.ToHashCode();
        }
    }

    public CommandResult Execute(WorldState state, IEngineContext context)
    {
        WorldPlayer player = state.CurrentPlayer;

        if (!WorldCommandHelpers.TryGetCurrentPlayerStack(state, StackId, out WorldStack? stack))
        {
            return CommandResult.Fail(WorldCommandError.StackNotFound);
        }

        if (!state.Sites.TryGetValue(SiteId, out WorldSite? site))
        {
            return CommandResult.Fail(WorldCommandError.SiteNotFound);
        }

        if (site is not WorldMerchantSite merchantSite)
        {
            return CommandResult.Fail(WorldCommandError.SiteNotFound);
        }

        if (stack.Position != merchantSite.Entrance)
        {
            return CommandResult.Fail(WorldCommandError.StackNotAtSite);
        }

        return CommandResult.Success(
            new Result(GetPurchaseItems(merchantSite, stack, player, context), GetSellItems(stack, context)),
            []);
    }

    private static ImmutableArray<PurchaseItem> GetPurchaseItems(
        WorldMerchantSite site,
        WorldStack stack,
        WorldPlayer player,
        IEngineContext context)
    {
        ImmutableArray<PurchaseItem>.Builder items = ImmutableArray.CreateBuilder<PurchaseItem>(site.Items.Length);

        foreach (WorldMerchantItem merchantItem in site.Items)
        {
            MerchantSiteCalculator.PurchaseInfo purchase =
                MerchantSiteCalculator.GetPurchaseInfo(site, stack, player, merchantItem.ItemId, context);

            items.Add(new PurchaseItem(merchantItem.ItemId, merchantItem.Count, purchase.Cost, purchase.Error));
        }

        return items.ToImmutable();
    }

    private static ImmutableArray<SellItem> GetSellItems(WorldStack stack, IEngineContext context)
    {
        ImmutableArray<SellItem>.Builder items = ImmutableArray.CreateBuilder<SellItem>(stack.Inventory.Items.Length);

        foreach (WorldItem item in stack.Inventory.Items)
        {
            MerchantSiteCalculator.SellInfo sell = MerchantSiteCalculator.GetSellInfo(item, context);

            items.Add(new SellItem(item, sell.Value));
        }

        return items.ToImmutable();
    }
}
