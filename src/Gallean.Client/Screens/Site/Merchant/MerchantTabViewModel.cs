using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gallean.Engine;

namespace Gallean.Client;

internal sealed partial class MerchantTabViewModel : ManagementTabViewModel, IDragDropProvider, IDragDropHost
{
    private readonly ManagementScreenViewModel screen;
    private readonly WorldSiteId siteId;
    private readonly WorldStackId stackId;
    private ItemDefinitionId? selectedPurchaseItemId;
    private WorldItemId? selectedSellItemId;

    public MerchantTabViewModel(
        GameModel gameModel,
        GameViewModel gameViewModel,
        ManagementScreenViewModel screen,
        WorldSiteId siteId,
        WorldStackId stackId)
        : base(gameModel, gameViewModel)
    {
        this.screen = screen;
        this.siteId = siteId;
        this.stackId = stackId;

        DragDrop = new DragDropViewModel(gameModel, this);

        RefreshView();

        RegisterSnapshotGatedCommands(TradeCommand);
    }

    public override string TabHeader => Tr("Merchant");
    public override AssetSpriteKey.SiteImage SiteImageKey { get; } = new(AssetAnimationKey.SiteType.Merchant);

    public DragDropViewModel DragDrop { get; }
    public DropZoneViewModel BuyZone { get; } = new();
    public DropZoneViewModel SellZone { get; } = new();

    public string OfferHint => Tr("Select an item to buy or one of your items to sell.");

    [ObservableProperty]
    public partial string SiteTitle { get; private set; } = string.Empty;

    [ObservableProperty]
    public partial string SiteGreeting { get; private set; } = string.Empty;

    [ObservableProperty]
    public partial IReadOnlyList<MerchantPurchaseItemViewModel> PurchaseItems { get; private set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<MerchantSellItemViewModel> SellItems { get; private set; } = [];

    [ObservableProperty]
    public partial SiteOfferViewModel? Offer { get; private set; }

    public void SelectPurchaseItem(MerchantPurchaseItemViewModel item)
    {
        if (IsBusy)
        {
            return;
        }

        if (selectedPurchaseItemId == item.ItemId)
        {
            selectedPurchaseItemId = null;
        }
        else
        {
            selectedPurchaseItemId = item.ItemId;
            selectedSellItemId = null;
        }

        RefreshView();
    }

    public void SelectSellItem(MerchantSellItemViewModel item)
    {
        if (IsBusy)
        {
            return;
        }

        if (selectedSellItemId == item.ItemId)
        {
            selectedSellItemId = null;
        }
        else
        {
            selectedSellItemId = item.ItemId;
            selectedPurchaseItemId = null;
        }

        RefreshView();
    }

    [RelayCommand(CanExecute = nameof(CanTrade))]
    private async Task TradeAsync()
    {
        if (selectedPurchaseItemId is { } purchaseItemId)
        {
            await BuyAsync(purchaseItemId);
        }
        else if (selectedSellItemId is { } sellItemId)
        {
            await SellAsync(sellItemId);
        }
    }

    private bool CanTrade()
    {
        if (IsBusy)
        {
            return false;
        }

        return Offer is { Status.IsAvailable: true };
    }

    async Task IDragDropProvider.DropAsync(IDragSourceReference source, IDragTargetReference target)
    {
        switch (source, target)
        {
            case (MerchantPurchaseItemReference purchaseItem, DropZoneReference zone) when zone.Zone == BuyZone:
                await BuyAsync(purchaseItem.ItemId);
                break;

            case (MerchantSellItemReference sellItem, DropZoneReference zone) when zone.Zone == SellZone:
                await SellAsync(sellItem.ItemId);
                break;
        }
    }

    void IDragDropProvider.OnDragChanged()
    {
        RefreshView();
    }

    private async Task BuyAsync(ItemDefinitionId itemId)
    {
        if (await TryDispatchWorldActionAsync(new WorldPurchaseItemCommand(stackId, siteId, itemId)))
        {
            Game.PlaySound(new AssetSoundKey.SitePurchase());
        }

        RefreshView();
    }

    private async Task SellAsync(WorldItemId itemId)
    {
        if (await TryDispatchWorldActionAsync(new WorldSellItemCommand(stackId, siteId, itemId)))
        {
            selectedSellItemId = null;
        }

        RefreshView();
    }

    protected override void OnSnapshotChanged(GameSnapshot current)
    {
        RefreshView();
    }

    private void RefreshView()
    {
        if (WorldState.Sites.GetValueOrDefault(siteId) is not WorldMerchantSite site)
        {
            screen.Close();
            return;
        }

        if (!WorldState.Stacks.TryGetValue(stackId, out WorldStack? stack) ||
            stack.OwnerId != ViewPlayerId ||
            stack.Position != site.Entrance)
        {
            screen.Close();
            return;
        }

        WorldGetMerchantInfoCommand.Result info = ResolveMerchantInfo();

        if (selectedPurchaseItemId is { } purchaseItemId && FindPurchaseItem(info, purchaseItemId) is null)
        {
            selectedPurchaseItemId = null;
        }

        if (selectedSellItemId is { } sellItemId && FindSellItem(info, sellItemId) is null)
        {
            selectedSellItemId = null;
        }

        SiteTitle = GameModel.Texts.GetScenarioText(site.TitleTextId);
        SiteGreeting = GameModel.Texts.GetScenarioText(site.GreetingTextId);

        PurchaseItems = BuildPurchaseItems(info);
        SellItems = BuildSellItems(info);
        Offer = BuildOffer(info);

        UpdateBuyZone(info, DragDrop.Dragged);
        UpdateSellZone(info, DragDrop.Dragged);

        TradeCommand.NotifyCanExecuteChanged();
    }

    private WorldGetMerchantInfoCommand.Result ResolveMerchantInfo()
    {
        ICommandResult<WorldCommandError, WorldEvent, WorldGetMerchantInfoCommand.Result> result =
            GameModel.World.DryRunCommand(new WorldGetMerchantInfoCommand(stackId, siteId));

        if (result.Failed)
        {
            return new WorldGetMerchantInfoCommand.Result([], []);
        }
        else
        {
            return result.Data;
        }
    }

    private static WorldGetMerchantInfoCommand.PurchaseItem? FindPurchaseItem(
        WorldGetMerchantInfoCommand.Result info,
        ItemDefinitionId itemId)
    {
        foreach (WorldGetMerchantInfoCommand.PurchaseItem purchaseItem in info.PurchaseItems)
        {
            if (purchaseItem.ItemId == itemId)
            {
                return purchaseItem;
            }
        }

        return null;
    }

    private static WorldGetMerchantInfoCommand.SellItem? FindSellItem(
        WorldGetMerchantInfoCommand.Result info,
        WorldItemId itemId)
    {
        foreach (WorldGetMerchantInfoCommand.SellItem sellItem in info.SellItems)
        {
            if (sellItem.Item.Id == itemId)
            {
                return sellItem;
            }
        }

        return null;
    }

    private string? FormatSellUnavailability(WorldItem item)
    {
        WorldCommandResult result = GameModel.World.DryRunCommand(new WorldSellItemCommand(stackId, siteId, item.Id));

        if (result.Failed)
        {
            return WorldCommandErrorFormatter.Format(result.Error.Value);
        }
        else
        {
            return null;
        }
    }

    private static string? FormatPurchaseUnavailability(WorldGetMerchantInfoCommand.PurchaseItem purchaseItem)
    {
        if (purchaseItem.Error is { } error)
        {
            return WorldCommandErrorFormatter.Format(error);
        }
        else
        {
            return null;
        }
    }

    private IReadOnlyList<MerchantPurchaseItemViewModel> BuildPurchaseItems(WorldGetMerchantInfoCommand.Result info)
    {
        WorldResourceSet resources = WorldState.Players[ViewPlayerId].Resources;
        List<MerchantPurchaseItemViewModel> result = [];

        foreach (WorldGetMerchantInfoCommand.PurchaseItem purchaseItem in info.PurchaseItems)
        {
            string name = ItemFormatter.FormatName(GameModel.Definitions, GameModel.Texts, purchaseItem.ItemId, charges: null);
            string description = ItemFormatter.FormatDescription(GameModel.Definitions, GameModel.Texts, purchaseItem.ItemId);
            TextSpans countText = TrSpans("In stock: {0:*}", purchaseItem.Count);
            AssetSpriteKey.ItemIcon icon = ItemIconResolver.Resolve(GameModel.Definitions, purchaseItem.ItemId);
            string? unavailableReason;

            if (purchaseItem.Error is { } error && error != WorldCommandError.InsufficientResources)
            {
                unavailableReason = WorldCommandErrorFormatter.Format(error);
            }
            else
            {
                unavailableReason = null;
            }

            MerchantPurchaseItemViewModel item = new(
                this,
                purchaseItem.ItemId,
                icon,
                name,
                description,
                countText,
                new OfferStatusViewModel(new OfferPriceViewModel(purchaseItem.Cost, resources), unavailableReason),
                ItemDetailsViewModel.Create(purchaseItem.ItemId, charges: null, GameModel.Definitions, GameModel.Texts))
            {
                IsSelected = selectedPurchaseItemId == purchaseItem.ItemId
            };

            result.Add(item);
        }

        return result;
    }

    private IReadOnlyList<MerchantSellItemViewModel> BuildSellItems(WorldGetMerchantInfoCommand.Result info)
    {
        List<MerchantSellItemViewModel> result = [];

        foreach (WorldGetMerchantInfoCommand.SellItem sellItem in info.SellItems)
        {
            WorldItem worldItem = sellItem.Item;
            string name = ItemFormatter.FormatName(GameModel.Definitions, GameModel.Texts, worldItem.DefinitionId, worldItem.Charges);
            string description = ItemFormatter.FormatDescription(GameModel.Definitions, GameModel.Texts, worldItem.DefinitionId);
            AssetSpriteKey.ItemIcon icon = ItemIconResolver.Resolve(GameModel.Definitions, worldItem.DefinitionId);

            ItemDetailsViewModel details = ItemDetailsViewModel.Create(
                worldItem.DefinitionId,
                worldItem.Charges,
                GameModel.Definitions,
                GameModel.Texts);

            MerchantSellItemViewModel item = new(
                this,
                worldItem.Id,
                icon,
                name,
                description,
                sellItem.Value,
                FormatSellUnavailability(worldItem),
                details)
            {
                IsSelected = selectedSellItemId == worldItem.Id
            };

            result.Add(item);
        }

        return result;
    }

    private SiteOfferViewModel? BuildOffer(WorldGetMerchantInfoCommand.Result info)
    {
        if (selectedPurchaseItemId is { } purchaseItemId && FindPurchaseItem(info, purchaseItemId) is { } purchaseItem)
        {
            return new SiteOfferViewModel(
                new IconViewModel.Item(ItemIconResolver.Resolve(GameModel.Definitions, purchaseItemId)),
                ItemDetailsViewModel.Create(purchaseItemId, charges: null, GameModel.Definitions, GameModel.Texts),
                ItemFormatter.FormatName(GameModel.Definitions, GameModel.Texts, purchaseItemId, charges: null),
                TrSpans("In stock: {0:*}", purchaseItem.Count),
                ItemFormatter.FormatDescription(GameModel.Definitions, GameModel.Texts, purchaseItemId),
                Tr("Buy"),
                new OfferStatusViewModel(new OfferPriceViewModel(purchaseItem.Cost, WorldState.Players[ViewPlayerId].Resources), FormatPurchaseUnavailability(purchaseItem)),
                TradeCommand);
        }
        else if (selectedSellItemId is { } sellItemId && FindSellItem(info, sellItemId) is { } sellItem)
        {
            WorldItem worldItem = sellItem.Item;

            return new SiteOfferViewModel(
                new IconViewModel.Item(ItemIconResolver.Resolve(GameModel.Definitions, worldItem.DefinitionId)),
                ItemDetailsViewModel.Create(worldItem.DefinitionId, worldItem.Charges, GameModel.Definitions, GameModel.Texts),
                ItemFormatter.FormatName(GameModel.Definitions, GameModel.Texts, worldItem.DefinitionId, worldItem.Charges),
                TrSpans("From your inventory"),
                ItemFormatter.FormatDescription(GameModel.Definitions, GameModel.Texts, worldItem.DefinitionId),
                Tr("Sell"),
                new OfferStatusViewModel(new OfferPriceViewModel(sellItem.Value), FormatSellUnavailability(worldItem)),
                TradeCommand);
        }
        else
        {
            return null;
        }
    }

    private void UpdateBuyZone(WorldGetMerchantInfoCommand.Result info, IDragSourceReference? dragged)
    {
        WorldGetMerchantInfoCommand.PurchaseItem? purchaseItem = null;

        if (dragged is MerchantPurchaseItemReference reference)
        {
            purchaseItem = FindPurchaseItem(info, reference.ItemId);
        }

        if (purchaseItem is not null)
        {
            string name = ItemFormatter.FormatName(GameModel.Definitions, GameModel.Texts, purchaseItem.ItemId, charges: null);

            BuyZone.Show(new MerchantTradeDropViewModel(Tr("Buy {0}", name), new ResourceSetViewModel(purchaseItem.Cost, ResourceSetProjection.All)));
        }
        else
        {
            BuyZone.Hide();
        }
    }

    private void UpdateSellZone(WorldGetMerchantInfoCommand.Result info, IDragSourceReference? dragged)
    {
        WorldGetMerchantInfoCommand.SellItem? sellItem = null;

        if (dragged is MerchantSellItemReference reference)
        {
            sellItem = FindSellItem(info, reference.ItemId);
        }

        if (sellItem is not null)
        {
            WorldItem item = sellItem.Item;
            string name = ItemFormatter.FormatName(GameModel.Definitions, GameModel.Texts, item.DefinitionId, item.Charges);

            SellZone.Show(new MerchantTradeDropViewModel(Tr("Sell {0}", name), new ResourceSetViewModel(sellItem.Value, ResourceSetProjection.All)));
        }
        else
        {
            SellZone.Hide();
        }
    }
}
