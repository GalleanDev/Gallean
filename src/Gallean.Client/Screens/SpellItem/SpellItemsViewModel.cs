using System.Collections.Immutable;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gallean.Engine;

namespace Gallean.Client;

internal sealed partial class SpellItemsViewModel : AbstractGameScreenViewModel
{
    private readonly ManagementScreenViewModel screen;
    private readonly WorldStackId stackId;
    private WorldItemId? selectedItemId;

    public SpellItemsViewModel(GameModel gameModel, GameViewModel gameViewModel, ManagementScreenViewModel screen, WorldStackId stackId)
        : base(gameModel, gameViewModel)
    {
        this.screen = screen;
        this.stackId = stackId;

        RefreshView();
    }

    public override AssetSoundKey OpenSound => new AssetSoundKey.BookOpen();
    public override AssetSoundKey CloseSound => new AssetSoundKey.BookClose();

    [ObservableProperty]
    public partial IReadOnlyList<SpellItemGroupViewModel> Groups { get; private set; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(UseSelectedCommand))]
    public partial SpellItemViewModel? SelectedItem { get; private set; }

    protected override bool TryClose()
    {
        CloseSelf();

        return true;
    }

    protected override void OnSnapshotChanged(GameSnapshot current)
    {
        RefreshView();
    }

    [RelayCommand]
    private void SelectItem(SpellItemViewModel item)
    {
        selectedItemId = item.ItemId;

        ApplySelection();
    }

    [RelayCommand]
    private void ClearSelection()
    {
        selectedItemId = null;

        ApplySelection();
    }

    [RelayCommand(CanExecute = nameof(CanUseSelected))]
    private async Task UseSelectedAsync()
    {
        if (SelectedItem is not null)
        {
            SpellItemViewModel item = SelectedItem;
            WorldUseItemCommand command = new(new WorldUnitGroup.Stack(stackId), item.ItemId, Target: null);
            WorldCommandResult commandResult = GameModel.World.DryRunCommand(command);

            if (commandResult.Error is WorldCommandError.SpellTargetInvalid)
            {
                if (Game.RootScreen is WorldScreenViewModel world)
                {
                    screen.Close();
                    world.SelectAction(new WorldCursorAction.UseItem(stackId, item.ItemId));
                }
            }
            else if (await TryDispatchWorldActionAsync(command))
            {
                Game.PlaySound(ItemSoundResolver.ResolveWorld(item.Category));
                screen.Close();
            }
            else
            {
                RefreshView();
            }
        }
    }

    private bool CanUseSelected()
    {
        return SelectedItem is not null && SelectedItem.Status.IsAvailable;
    }

    private void RefreshView()
    {
        if (WorldState.Stacks.TryGetValue(stackId, out WorldStack? stack))
        {
            Groups = BuildGroups(stack);
        }
        else
        {
            Groups = [];
        }

        ApplySelection();
    }

    private void ApplySelection()
    {
        SpellItemViewModel? selected = null;

        foreach (SpellItemGroupViewModel group in Groups)
        {
            foreach (SpellItemViewModel item in group.Items)
            {
                item.IsSelected = item.ItemId == selectedItemId;

                if (item.IsSelected)
                {
                    selected = item;
                }
            }
        }

        SelectedItem = selected;
        selectedItemId = selected?.ItemId;
    }

    private IReadOnlyList<SpellItemGroupViewModel> BuildGroups(WorldStack stack)
    {
        WorldResourceSet resources = WorldState.Players[ViewPlayerId].Resources;
        ImmutableArray<ItemGroup> itemGroups = ItemGroup.Build([.. stack.Equipment.Items.Values, .. stack.Inventory.Items]);
        List<SpellItemViewModel> scrolls = [];
        List<SpellItemViewModel> staffs = [];

        foreach (ItemGroup itemGroup in itemGroups)
        {
            ItemDefinition definition = GameModel.Definitions.GetItem(itemGroup.DefinitionId);

            if (definition.Effect is SpellItemEffect)
            {
                WorldItem item = itemGroup.Items[^1];

                ItemDetailsViewModel details = ItemDetailsViewModel.Create(
                    item.DefinitionId,
                    item.Charges,
                    resources,
                    GameModel.Definitions,
                    GameModel.Texts);

                SpellItemViewModel viewModel = new(
                    item.Id,
                    definition.Category,
                    InventoryItemTypeResolver.Resolve(definition.Category),
                    itemGroup.Items.Length,
                    BuildStatus(stack, item, details),
                    details);

                if (definition.Category == ItemCategory.Scroll)
                {
                    scrolls.Add(viewModel);
                }
                else
                {
                    staffs.Add(viewModel);
                }
            }
        }

        List<SpellItemGroupViewModel> groups = [];

        if (staffs.Count > 0)
        {
            groups.Add(new SpellItemGroupViewModel(Tr("Staffs"), staffs));
        }

        if (scrolls.Count > 0)
        {
            groups.Add(new SpellItemGroupViewModel(Tr("Scrolls"), scrolls));
        }

        return groups;
    }

    private OfferStatusViewModel BuildStatus(WorldStack stack, WorldItem item, ItemDetailsViewModel details)
    {
        string? unavailableReason = ResolveUnavailableReason(stack, item);

        if (details.CastingCost is not null)
        {
            return new OfferStatusViewModel(details.CastingCost, unavailableReason);
        }
        else
        {
            return new OfferStatusViewModel(unavailableReason);
        }
    }

    private string? ResolveUnavailableReason(WorldStack stack, WorldItem item)
    {
        WorldUseItemCommand command = new(new WorldUnitGroup.Stack(stack.Id), item.Id, Target: null);
        WorldCommandResult commandResult = GameModel.World.DryRunCommand(command);
        WorldCommandError? error = commandResult.Error;

        if (error is null or WorldCommandError.SpellTargetInvalid)
        {
            return null;
        }
        else
        {
            return WorldCommandErrorFormatter.Format(error.Value);
        }
    }
}
