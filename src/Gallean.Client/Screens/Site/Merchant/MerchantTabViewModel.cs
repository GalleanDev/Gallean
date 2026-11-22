using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gallean.Engine;

namespace Gallean.Client;

internal sealed partial class MerchantTabViewModel : ManagementTabViewModel, IDragDropProvider, IDragDropHost
{
    private readonly WorldSiteId siteId;
    private readonly WorldStackId stackId;
    private ItemDefinitionId? selectedPurchaseItemId;
    private MerchantSellItemReference? selectedSellItem;

    public MerchantTabViewModel(
        GameModel gameModel,
        GameViewModel gameViewModel,
        ManagementScreenViewModel screen,
        WorldSiteId siteId,
        WorldStackId stackId)
        : base(gameModel, gameViewModel)
    {
        Screen = screen;
        this.siteId = siteId;
        this.stackId = stackId;

        DragDrop = new DragDropViewModel(gameModel, this);

        RefreshView();

        RegisterSnapshotGatedCommands(TradeCommand, SellAllCommand, SellValuablesCommand);
    }

    public ManagementScreenViewModel Screen { get; }
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
            selectedSellItem = null;
        }

        RefreshView();
    }

    public void SelectSellItem(MerchantSellItemViewModel item)
    {
        if (IsBusy)
        {
            return;
        }

        if (selectedSellItem == item.Reference)
        {
            selectedSellItem = null;
        }
        else
        {
            selectedSellItem = item.Reference;
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
        else if (selectedSellItem is { } sellItem)
        {
            await SellAsync(sellItem);
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

    [RelayCommand(CanExecute = nameof(CanSellAll))]
    private void SellAll()
    {
        WorldResourceSet value = GetValue(EnumerateSellableItems());

        Game.OpenScreen(new ConfirmPromptViewModel(
            TrSpans("Sell all items"),
            TrSpans("The merchant offers {0:*} for {1:b}.", WorldResourceFormatter.Format(value), Tr("all your items")),
            Tr("Sell all"),
            ConfirmPromptType.Danger,
            () => SellItemsAsync(EnumerateSellableItems())));
    }

    private bool CanSellAll()
    {
        if (IsBusy)
        {
            return false;
        }

        List<WorldGetMerchantInfoCommand.SellItem> sellItems = [.. EnumerateSellableItems()];

        return sellItems.Count > 0;
    }

    [RelayCommand(CanExecute = nameof(CanSellValuables))]
    private void SellValuables()
    {
        WorldResourceSet value = GetValue(EnumerateSellableValuables());

        Game.OpenScreen(new ConfirmPromptViewModel(
            TrSpans("Sell all valuables"),
            TrSpans("The merchant offers {0:*} for {1:b}.", WorldResourceFormatter.Format(value), Tr("all your valuables")),
            Tr("Sell"),
            ConfirmPromptType.Success,
            () => SellItemsAsync(EnumerateSellableValuables())));
    }

    private bool CanSellValuables()
    {
        if (IsBusy)
        {
            return false;
        }

        List<WorldGetMerchantInfoCommand.SellItem> sellItems = [.. EnumerateSellableValuables()];

        return sellItems.Count > 0;
    }

    async Task IDragDropProvider.DropAsync(IDragSourceReference source, IDragTargetReference target)
    {
        switch (source, target)
        {
            case (MerchantPurchaseItemReference purchaseItem, DropZoneReference zone) when zone.Zone == BuyZone:
                await BuyAsync(purchaseItem.ItemId);
                break;

            case (MerchantSellItemReference sellItem, DropZoneReference zone) when zone.Zone == SellZone:
                await SellAsync(sellItem);
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

    private async Task SellAsync(MerchantSellItemReference reference)
    {
        WorldGetMerchantInfoCommand.SellItem? sellItem = FindSellItem(ResolveMerchantInfo(), reference);

        if (sellItem is not null && await TryDispatchWorldActionAsync(new WorldSellItemCommand(stackId, siteId, sellItem.Item.Id)))
        {
            Game.PlaySound(new AssetSoundKey.SitePurchase());
        }

        RefreshView();
    }

    private async Task SellItemsAsync(IEnumerable<WorldGetMerchantInfoCommand.SellItem> sellItems)
    {
        foreach (WorldGetMerchantInfoCommand.SellItem sellItem in sellItems)
        {
            await TryDispatchWorldActionAsync(new WorldSellItemCommand(stackId, siteId, sellItem.Item.Id));
        }

        Game.PlaySound(new AssetSoundKey.SitePurchase());
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
            Screen.Close();
            return;
        }

        if (!WorldState.Stacks.TryGetValue(stackId, out WorldStack? stack) ||
            stack.OwnerId != ViewPlayerId ||
            !site.IsEntranceAt(stack.Position))
        {
            Screen.Close();
            return;
        }

        WorldGetMerchantInfoCommand.Result info = ResolveMerchantInfo();

        if (FindPurchaseItem(info, selectedPurchaseItemId) is null)
        {
            selectedPurchaseItemId = null;
        }

        if (FindSellItem(info, selectedSellItem) is null)
        {
            selectedSellItem = null;
        }

        SiteTitle = GameModel.Texts.GetScenarioText(site.TitleTextId);
        SiteGreeting = GameModel.Texts.GetScenarioText(site.GreetingTextId);

        PurchaseItems = BuildPurchaseItems(info);
        SellItems = BuildSellItems(info);
        Offer = BuildOffer(info);

        UpdateBuyZone(info, DragDrop.Dragged);
        UpdateSellZone(info, DragDrop.Dragged);

        TradeCommand.NotifyCanExecuteChanged();
        SellAllCommand.NotifyCanExecuteChanged();
        SellValuablesCommand.NotifyCanExecuteChanged();
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

    private IEnumerable<WorldGetMerchantInfoCommand.SellItem> EnumerateSellableItems()
    {
        WorldGetMerchantInfoCommand.Result info = ResolveMerchantInfo();

        foreach (WorldGetMerchantInfoCommand.SellItem sellItem in info.SellItems)
        {
            if (CanSell(sellItem.Item))
            {
                yield return sellItem;
            }
        }
    }

    private IEnumerable<WorldGetMerchantInfoCommand.SellItem> EnumerateSellableValuables()
    {
        WorldGetMerchantInfoCommand.Result info = ResolveMerchantInfo();

        foreach (WorldGetMerchantInfoCommand.SellItem sellItem in info.SellItems)
        {
            if (IsValuable(sellItem.Item) && CanSell(sellItem.Item))
            {
                yield return sellItem;
            }
        }
    }

    private bool CanSell(WorldItem item)
    {
        WorldSellItemCommand command = new(stackId, siteId, item.Id);
        WorldCommandResult commandResult = GameModel.World.DryRunCommand(command);

        return !commandResult.Failed;
    }

    private bool IsValuable(WorldItem item)
    {
        ItemDefinition definition = GameModel.Definitions.GetItem(item.DefinitionId);

        return definition.Category switch
        {
            ItemCategory.Armor => false,
            ItemCategory.Jewel => false,
            ItemCategory.Weapon => false,
            ItemCategory.Banner => false,
            ItemCategory.PotionTemporaryBoost => false,
            ItemCategory.PotionHeal => false,
            ItemCategory.PotionRevive => false,
            ItemCategory.PotionPermanentBoost => false,
            ItemCategory.Scroll => false,
            ItemCategory.Wand => false,
            ItemCategory.Valuable => true,
            ItemCategory.Orb => false,
            ItemCategory.Talisman => false,
            ItemCategory.TravelItem => false,
            ItemCategory.Special => false
        };
    }

    private static WorldResourceSet GetValue(IEnumerable<WorldGetMerchantInfoCommand.SellItem> sellItems)
    {
        WorldResourceSet value = WorldResourceSet.Empty;

        foreach (WorldGetMerchantInfoCommand.SellItem sellItem in sellItems)
        {
            value = new WorldResourceSet(
                value.Gold + sellItem.Value.Gold,
                value.DeathMana + sellItem.Value.DeathMana,
                value.RuneMana + sellItem.Value.RuneMana,
                value.LifeMana + sellItem.Value.LifeMana,
                value.InfernalMana + sellItem.Value.InfernalMana,
                value.GroveMana + sellItem.Value.GroveMana);
        }

        return value;
    }

    private static WorldGetMerchantInfoCommand.PurchaseItem? FindPurchaseItem(
        WorldGetMerchantInfoCommand.Result info,
        ItemDefinitionId? itemId)
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
        MerchantSellItemReference? reference)
    {
        WorldGetMerchantInfoCommand.SellItem? result = null;

        foreach (WorldGetMerchantInfoCommand.SellItem sellItem in info.SellItems)
        {
            if (new MerchantSellItemReference(sellItem.Item.DefinitionId, sellItem.Item.Charges) == reference)
            {
                result = sellItem;
            }
        }

        return result;
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
        IEnumerable<WorldGetMerchantInfoCommand.PurchaseItem> purchaseItems = info.PurchaseItems.OrderBy(purchaseItem => purchaseItem.Cost, OfferPriceComparer.Descending);

        foreach (WorldGetMerchantInfoCommand.PurchaseItem purchaseItem in purchaseItems)
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

            ItemDetailsViewModel details = ItemDetailsViewModel.Create(
                purchaseItem.ItemId,
                charges: null,
                resources,
                GameModel.Definitions,
                GameModel.Texts);

            MerchantPurchaseItemViewModel item = new(
                this,
                purchaseItem.ItemId,
                icon,
                name,
                description,
                countText,
                new OfferStatusViewModel(new OfferPriceViewModel(purchaseItem.Cost, resources), unavailableReason),
                details)
            {
                IsSelected = selectedPurchaseItemId == purchaseItem.ItemId
            };

            result.Add(item);
        }

        return result;
    }

    private IReadOnlyList<MerchantSellItemViewModel> BuildSellItems(WorldGetMerchantInfoCommand.Result info)
    {
        WorldResourceSet resources = WorldState.Players[ViewPlayerId].Resources;
        List<MerchantSellItemViewModel> result = [];
        IEnumerable<WorldGetMerchantInfoCommand.SellItem> sellItems = info.SellItems.OrderBy(sellItem => sellItem.Value, OfferPriceComparer.Descending);

        foreach (ItemGroup group in ItemGroup.Build([.. sellItems.Select(sellItem => sellItem.Item)]))
        {
            MerchantSellItemReference reference = new(group.DefinitionId, group.Charges);
            WorldGetMerchantInfoCommand.SellItem? sellItem = FindSellItem(info, reference);

            if (sellItem is not null)
            {
                WorldItem worldItem = sellItem.Item;
                string name = ItemFormatter.FormatName(GameModel.Definitions, GameModel.Texts, worldItem.DefinitionId, worldItem.Charges);
                string description = ItemFormatter.FormatDescription(GameModel.Definitions, GameModel.Texts, worldItem.DefinitionId);
                AssetSpriteKey.ItemIcon icon = ItemIconResolver.Resolve(GameModel.Definitions, worldItem.DefinitionId);
                ItemDefinition definition = GameModel.Definitions.GetItem(worldItem.DefinitionId);
                InventoryItemType itemType = InventoryItemTypeResolver.Resolve(definition.Category);

                ItemDetailsViewModel details = ItemDetailsViewModel.Create(
                    worldItem.DefinitionId,
                    worldItem.Charges,
                    resources,
                    GameModel.Definitions,
                    GameModel.Texts);

                MerchantSellItemViewModel item = new(
                    this,
                    reference.ItemId,
                    reference.Charges,
                    group.Items.Length,
                    itemType,
                    icon,
                    name,
                    description,
                    sellItem.Value,
                    FormatSellUnavailability(worldItem),
                    details)
                {
                    IsSelected = selectedSellItem == reference
                };

                result.Add(item);
            }
        }

        return result;
    }

    private SiteOfferViewModel? BuildOffer(WorldGetMerchantInfoCommand.Result info)
    {
        WorldResourceSet resources = WorldState.Players[ViewPlayerId].Resources;
        WorldGetMerchantInfoCommand.PurchaseItem? purchaseItem = FindPurchaseItem(info, selectedPurchaseItemId);
        WorldGetMerchantInfoCommand.SellItem? sellItem = FindSellItem(info, selectedSellItem);

        if (purchaseItem is not null)
        {
            ItemDetailsViewModel details = ItemDetailsViewModel.Create(
                purchaseItem.ItemId,
                charges: null,
                resources,
                GameModel.Definitions,
                GameModel.Texts);

            return new SiteOfferViewModel(
                new IconViewModel.Item(ItemIconResolver.Resolve(GameModel.Definitions, purchaseItem.ItemId)),
                details,
                ItemFormatter.FormatName(GameModel.Definitions, GameModel.Texts, purchaseItem.ItemId, charges: null),
                TrSpans("In stock: {0:*}", purchaseItem.Count),
                ItemFormatter.FormatDescription(GameModel.Definitions, GameModel.Texts, purchaseItem.ItemId),
                Tr("Buy"),
                new OfferStatusViewModel(new OfferPriceViewModel(purchaseItem.Cost, resources), FormatPurchaseUnavailability(purchaseItem)),
                TradeCommand);
        }
        else if (sellItem is not null)
        {
            WorldItem worldItem = sellItem.Item;

            ItemDetailsViewModel details = ItemDetailsViewModel.Create(
                worldItem.DefinitionId,
                worldItem.Charges,
                resources,
                GameModel.Definitions,
                GameModel.Texts);

            return new SiteOfferViewModel(
                new IconViewModel.Item(ItemIconResolver.Resolve(GameModel.Definitions, worldItem.DefinitionId)),
                details,
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
            sellItem = FindSellItem(info, reference);
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
