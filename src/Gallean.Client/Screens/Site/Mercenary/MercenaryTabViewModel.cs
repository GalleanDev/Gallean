using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gallean.Common;
using Gallean.Engine;

namespace Gallean.Client;

internal sealed partial class MercenaryTabViewModel
    : ManagementTabViewModel, IUnitGridSelectionProvider, IDragDropProvider, IDragDropHost
{
    private readonly ManagementScreenViewModel screen;
    private readonly WorldSiteId siteId;
    private readonly WorldStackId stackId;
    private readonly UnitGridSelectionViewModel unitSelection;
    private MercenaryReference? selectedMercenary;

    public MercenaryTabViewModel(GameModel gameModel, GameViewModel gameViewModel, ManagementScreenViewModel screen, WorldSiteId siteId, WorldStackId stackId)
        : base(gameModel, gameViewModel)
    {
        this.screen = screen;
        this.siteId = siteId;
        this.stackId = stackId;

        unitSelection = new UnitGridSelectionViewModel(gameModel, this);
        Stack = new SiteStackViewModel(gameModel, unitSelection);
        DragDrop = new DragDropViewModel(gameModel, this);

        RefreshView();

        RegisterSnapshotGatedCommands(HireCommand);
    }

    public override string TabHeader => Tr("Mercenaries");
    public override AssetSpriteKey.SiteImage SiteImageKey { get; } = new(AssetAnimationKey.SiteType.Mercenary);

    public SiteStackViewModel Stack { get; }
    public DragDropViewModel DragDrop { get; }

    public string OfferHint => Tr("Select a mercenary to hire.");

    [ObservableProperty]
    public partial string SiteTitle { get; private set; } = string.Empty;

    [ObservableProperty]
    public partial string SiteGreeting { get; private set; } = string.Empty;

    [ObservableProperty]
    public partial IReadOnlyList<MercenaryItemViewModel> Mercenaries { get; private set; } = [];

    [ObservableProperty]
    public partial SiteOfferViewModel? Offer { get; private set; }

    public void SelectMercenary(MercenaryItemViewModel mercenary)
    {
        if (IsBusy)
        {
            return;
        }

        if (selectedMercenary == mercenary.Reference)
        {
            selectedMercenary = null;
        }
        else
        {
            selectedMercenary = mercenary.Reference;
        }

        RefreshView();
    }

    [RelayCommand(CanExecute = nameof(CanHire))]
    private async Task HireAsync()
    {
        if (selectedMercenary is { } mercenary && FindHirePosition(mercenary) is { } position)
        {
            await HireAtAsync(mercenary, position);
        }
    }

    private bool CanHire()
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
            case (UnitGridSlotReference sourceSlot, UnitGridSlotReference targetSlot):
                await unitSelection.DropUnitAsync(sourceSlot, targetSlot);
                break;

            case (MercenaryReference mercenary, UnitGridSlotReference targetSlot):
                await HireAtAsync(mercenary, targetSlot.Position);
                break;
        }
    }

    void IDragDropProvider.OnDragChanged()
    {
        RefreshView();
    }

    private async Task HireAtAsync(MercenaryReference mercenary, GridPosition position)
    {
        if (await TryDispatchWorldActionAsync(new WorldPurchaseMercenaryCommand(stackId, siteId, mercenary.UnitDefinitionId, mercenary.Level, position)))
        {
            Game.PlaySound(new AssetSoundKey.SitePurchase());
        }

        RefreshView();
    }

    async Task<bool> IUnitGridSelectionProvider.MoveUnitAsync(UnitGridSlotReference source, UnitGridSlotReference target)
    {
        if (!WorldState.Stacks.TryGetValue(stackId, out WorldStack? stack))
        {
            return false;
        }

        WorldUnit? unit = stack.Units[source.Position];

        if (unit is null)
        {
            return false;
        }

        return await TryDispatchWorldActionAsync(
            new WorldRepositionUnitCommand(new WorldUnitGroup.Stack(stackId), unit.Id, target.Position));
    }

    void IUnitGridSelectionProvider.OnUnitSelectionChanged()
    {
        RefreshView();
    }

    void IUnitGridSelectionProvider.OnUnitMoved()
    {
        RefreshView();
    }

    protected override void OnSnapshotChanged(GameSnapshot current)
    {
        RefreshView();
    }

    private void RefreshView()
    {
        if (WorldState.Sites.GetValueOrDefault(siteId) is not WorldMercenarySite site)
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

        WorldGetMercenaryInfoCommand.Result info = ResolveMercenaryInfo();

        if (selectedMercenary is { } mercenary && FindMercenary(info, mercenary) is null)
        {
            selectedMercenary = null;
        }

        SiteTitle = GameModel.Texts.GetScenarioText(site.TitleTextId);
        SiteGreeting = GameModel.Texts.GetScenarioText(site.GreetingTextId);
        Stack.Update(stack, showsPurchaseHints: true, BuildUnitHighlight(stack));

        Mercenaries = BuildMercenaries(info);
        Offer = BuildOffer(info);

        HireCommand.NotifyCanExecuteChanged();
    }

    private IReadOnlySet<GridPosition> BuildUnitHighlight(WorldStack stack)
    {
        HashSet<GridPosition> positions = [];
        IDragSourceReference? placed = DragDrop.Dragged;

        foreach (GridPosition position in GridPosition.All)
        {
            IWorldCommand<Empty>? command = BuildDropCommand(stack, placed, position);

            if (command is null)
            {
                continue;
            }

            WorldCommandResult result = GameModel.World.DryRunCommand(command);

            if (!result.Failed)
            {
                positions.Add(position);
            }
        }

        return positions;
    }

    private IWorldCommand<Empty>? BuildDropCommand(WorldStack stack, IDragSourceReference? placed, GridPosition position)
    {
        return placed switch
        {
            UnitGridSlotReference source when source.Position != position =>
                BuildRepositionCommand(stack, source.Position, position),
            MercenaryReference mercenary =>
                new WorldPurchaseMercenaryCommand(stack.Id, siteId, mercenary.UnitDefinitionId, mercenary.Level, position),
            _ => null
        };
    }

    private static WorldRepositionUnitCommand? BuildRepositionCommand(WorldStack stack, GridPosition source, GridPosition target)
    {
        WorldUnit? unit = stack.Units[source];

        if (unit is null)
        {
            return null;
        }

        return new WorldRepositionUnitCommand(new WorldUnitGroup.Stack(stack.Id), unit.Id, target);
    }

    private GridPosition? FindHirePosition(MercenaryReference mercenary)
    {
        foreach (GridPosition position in GridPosition.All)
        {
            WorldPurchaseMercenaryCommand command = new(stackId, siteId, mercenary.UnitDefinitionId, mercenary.Level, position);
            WorldCommandResult result = GameModel.World.DryRunCommand(command);

            if (!result.Failed)
            {
                return position;
            }
        }

        return null;
    }

    private WorldGetMercenaryInfoCommand.Result ResolveMercenaryInfo()
    {
        ICommandResult<WorldCommandError, WorldEvent, WorldGetMercenaryInfoCommand.Result> result =
            GameModel.World.DryRunCommand(new WorldGetMercenaryInfoCommand(stackId, siteId));

        if (result.Failed)
        {
            return new WorldGetMercenaryInfoCommand.Result([]);
        }
        else
        {
            return result.Data;
        }
    }

    private static WorldGetMercenaryInfoCommand.Mercenary? FindMercenary(
        WorldGetMercenaryInfoCommand.Result info,
        MercenaryReference reference)
    {
        foreach (WorldGetMercenaryInfoCommand.Mercenary mercenary in info.Mercenaries)
        {
            if (mercenary.DefinitionId == reference.UnitDefinitionId && mercenary.Level == reference.Level)
            {
                return mercenary;
            }
        }

        return null;
    }

    private IReadOnlyList<MercenaryItemViewModel> BuildMercenaries(WorldGetMercenaryInfoCommand.Result info)
    {
        WorldResourceSet resources = WorldState.Players[ViewPlayerId].Resources;
        List<MercenaryItemViewModel> result = [];

        foreach (WorldGetMercenaryInfoCommand.Mercenary mercenary in info.Mercenaries)
        {
            UnitDefinition definition = GameModel.Definitions.GetUnit(mercenary.DefinitionId);

            string name = GameModel.Texts.GetGlobalText(definition.NameTextId).Text;
            string description = GameModel.Texts.GetGlobalText(definition.DescriptionTextId).Text;
            string levelText = Tr("Level {0}", mercenary.Level);
            string? unavailableReason;

            if (mercenary.Error is { } error && error != WorldCommandError.InsufficientResources)
            {
                unavailableReason = WorldCommandErrorFormatter.Format(error);
            }
            else
            {
                unavailableReason = null;
            }

            UnitDetailsViewModel details = BuildMercenaryDetails(mercenary);

            MercenaryItemViewModel item = new(
                this,
                mercenary.DefinitionId,
                mercenary.Level,
                definition.IsLarge,
                name,
                description,
                levelText,
                new OfferStatusViewModel(new OfferPriceViewModel(mercenary.Cost, resources), unavailableReason),
                details)
            {
                IsSelected = selectedMercenary == new MercenaryReference(mercenary.DefinitionId, mercenary.Level)
            };

            result.Add(item);
        }

        return result;
    }

    private UnitDetailsViewModel BuildMercenaryDetails(WorldGetMercenaryInfoCommand.Mercenary mercenary)
    {
        WorldUnit unit = new(
            UnitDetailsViewModel.PreviewUnitId,
            mercenary.DefinitionId,
            Hitpoints: 0,
            Experience: 0,
            mercenary.Level,
            [],
            null,
            false);

        UnitStats unitStats = GameModel.Stats.GetWorldUnitStats(unit);

        return UnitDetailsViewModel.CreateFromWorld(
            unit with { Hitpoints = unitStats.MaxHitpoints },
            null,
            GameModel.Definitions,
            GameModel.Texts,
            GameModel.Stats);
    }

    private SiteOfferViewModel? BuildOffer(WorldGetMercenaryInfoCommand.Result info)
    {
        if (selectedMercenary is { } reference && FindMercenary(info, reference) is { } mercenary)
        {
            UnitDefinition definition = GameModel.Definitions.GetUnit(mercenary.DefinitionId);
            string? unavailableReason;

            if (mercenary.Error is { } error)
            {
                unavailableReason = WorldCommandErrorFormatter.Format(error);
            }
            else
            {
                unavailableReason = null;
            }

            return new SiteOfferViewModel(
                new IconViewModel.Unit(new AssetSpriteKey.UnitPortrait(mercenary.DefinitionId)),
                BuildMercenaryDetails(mercenary),
                GameModel.Texts.GetGlobalText(definition.NameTextId).Text,
                TrSpans("Level {0:*}", mercenary.Level),
                GameModel.Texts.GetGlobalText(definition.DescriptionTextId).Text,
                Tr("Hire"),
                new OfferStatusViewModel(new OfferPriceViewModel(mercenary.Cost, WorldState.Players[ViewPlayerId].Resources), unavailableReason),
                HireCommand);
        }
        else
        {
            return null;
        }
    }
}
