using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gallean.Engine;

namespace Gallean.Client;

internal sealed partial class MageTabViewModel
    : ManagementTabViewModel, IUnitGridSelectionProvider, IDragDropProvider, IDragDropHost
{
    private readonly ManagementScreenViewModel screen;
    private readonly WorldSiteId siteId;
    private readonly WorldStackId stackId;
    private readonly UnitGridSelectionViewModel unitSelection;
    private WorldSpellDefinitionId? selectedSpellId;

    public MageTabViewModel(GameModel gameModel, GameViewModel gameViewModel, ManagementScreenViewModel screen, WorldSiteId siteId, WorldStackId stackId)
        : base(gameModel, gameViewModel)
    {
        this.screen = screen;
        this.siteId = siteId;
        this.stackId = stackId;

        unitSelection = new UnitGridSelectionViewModel(gameModel, this);
        Stack = new SiteStackViewModel(gameModel, unitSelection);
        DragDrop = new DragDropViewModel(gameModel, this);

        RefreshView();

        RegisterSnapshotGatedCommands(LearnCommand);
    }

    public override string TabHeader => Tr("Mage tower");
    public override AssetSpriteKey.SiteImage SiteImageKey { get; } = new(AssetAnimationKey.SiteType.Mage);

    public SiteStackViewModel Stack { get; }
    public DragDropViewModel DragDrop { get; }

    public string OfferHint => Tr("Select a spell to learn it.");

    [ObservableProperty]
    public partial string SiteTitle { get; private set; } = string.Empty;

    [ObservableProperty]
    public partial string SiteGreeting { get; private set; } = string.Empty;

    [ObservableProperty]
    public partial IReadOnlyList<MageSpellItemViewModel> Spells { get; private set; } = [];

    [ObservableProperty]
    public partial SiteOfferViewModel? Offer { get; private set; }

    public void SelectSpell(MageSpellItemViewModel spell)
    {
        if (IsBusy)
        {
            return;
        }

        if (selectedSpellId == spell.SpellId)
        {
            selectedSpellId = null;
        }
        else
        {
            selectedSpellId = spell.SpellId;
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

    async Task IDragDropProvider.DropAsync(IDragSourceReference source, IDragTargetReference target)
    {
        if (source is UnitGridSlotReference sourceSlot && target is UnitGridSlotReference targetSlot)
        {
            await unitSelection.DropUnitAsync(sourceSlot, targetSlot);
        }
    }

    void IDragDropProvider.OnDragChanged()
    {
        RefreshView();
    }

    [RelayCommand(CanExecute = nameof(CanLearn))]
    private async Task LearnAsync()
    {
        if (selectedSpellId is { } spellId)
        {
            if (await TryDispatchWorldActionAsync(new WorldPurchaseSpellCommand(stackId, siteId, spellId)))
            {
                WorldSpellDefinition definition = GameModel.Definitions.GetSpell(spellId);

                Game.PlaySound(new AssetSoundKey.SitePurchase());
                Game.ShowInfo(IconMoniker.Spellbook, Tr("Spell learned"), GameModel.Texts.GetGlobalText(definition.NameTextId).Text);
                selectedSpellId = null;
            }

            RefreshView();
        }
    }

    private bool CanLearn()
    {
        if (IsBusy)
        {
            return false;
        }

        return Offer is { Status.IsAvailable: true };
    }

    protected override void OnSnapshotChanged(GameSnapshot current)
    {
        RefreshView();
    }

    private void RefreshView()
    {
        if (WorldState.Sites.GetValueOrDefault(siteId) is not WorldMageSite site)
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

        WorldGetMageInfoCommand.Result info = ResolveMageInfo();

        if (selectedSpellId is { } spellId && FindSpell(info, spellId) is null)
        {
            selectedSpellId = null;
        }

        SiteTitle = GameModel.Texts.GetScenarioText(site.TitleTextId);
        SiteGreeting = GameModel.Texts.GetScenarioText(site.GreetingTextId);
        Stack.Update(stack, showsPurchaseHints: false, BuildUnitHighlight(stack));

        Spells = BuildSpells(info);
        Offer = BuildOffer(info);

        LearnCommand.NotifyCanExecuteChanged();
    }

    private IReadOnlySet<GridPosition> BuildUnitHighlight(WorldStack stack)
    {
        HashSet<GridPosition> positions = [];

        if (DragDrop.Dragged is not UnitGridSlotReference source)
        {
            return positions;
        }

        WorldUnit? unit = stack.Units[source.Position];

        if (unit is null)
        {
            return positions;
        }

        foreach (GridPosition position in GridPosition.All)
        {
            WorldRepositionUnitCommand command = new(new WorldUnitGroup.Stack(stack.Id), unit.Id, position);
            WorldCommandResult result = GameModel.World.DryRunCommand(command);

            if (position != source.Position && !result.Failed)
            {
                positions.Add(position);
            }
        }

        return positions;
    }

    private WorldGetMageInfoCommand.Result ResolveMageInfo()
    {
        ICommandResult<WorldCommandError, WorldEvent, WorldGetMageInfoCommand.Result> result =
            GameModel.World.DryRunCommand(new WorldGetMageInfoCommand(stackId, siteId));

        if (result.Failed)
        {
            return new WorldGetMageInfoCommand.Result([]);
        }
        else
        {
            return result.Data;
        }
    }

    private static WorldGetMageInfoCommand.Spell? FindSpell(WorldGetMageInfoCommand.Result info, WorldSpellDefinitionId spellId)
    {
        foreach (WorldGetMageInfoCommand.Spell spell in info.Spells)
        {
            if (spell.SpellId == spellId)
            {
                return spell;
            }
        }

        return null;
    }

    private static string? FormatUnavailability(WorldGetMageInfoCommand.Spell spell)
    {
        if (spell.Error is { } error)
        {
            return WorldCommandErrorFormatter.Format(error);
        }
        else
        {
            return null;
        }
    }

    private IReadOnlyList<MageSpellItemViewModel> BuildSpells(WorldGetMageInfoCommand.Result info)
    {
        WorldResourceSet resources = WorldState.Players[ViewPlayerId].Resources;
        List<MageSpellItemViewModel> result = [];

        foreach (WorldGetMageInfoCommand.Spell spell in info.Spells)
        {
            if (spell.Error == WorldCommandError.SpellAlreadyKnown)
            {
                continue;
            }
            else
            {
                WorldSpellDefinition definition = GameModel.Definitions.GetSpell(spell.SpellId);

                string name = GameModel.Texts.GetGlobalText(definition.NameTextId).Text;
                string description = GameModel.Texts.GetGlobalText(definition.DescriptionTextId).Text;
                string levelText = Tr("Level {0}", definition.Level);
                string? unavailableReason;

                if (spell.Error is { } error && error != WorldCommandError.InsufficientResources)
                {
                    unavailableReason = WorldCommandErrorFormatter.Format(error);
                }
                else
                {
                    unavailableReason = null;
                }

                SpellDetailsViewModel details = SpellDetailsViewModel.Create(spell.SpellId, GameModel.Definitions, GameModel.Texts);

                MageSpellItemViewModel item = new(
                    this,
                    spell.SpellId,
                    name,
                    description,
                    levelText,
                    new OfferStatusViewModel(new OfferPriceViewModel(spell.Cost, resources), unavailableReason),
                    details)
                {
                    IsSelected = selectedSpellId == spell.SpellId
                };

                result.Add(item);
            }
        }

        return result;
    }

    private SiteOfferViewModel? BuildOffer(WorldGetMageInfoCommand.Result info)
    {
        if (selectedSpellId is { } spellId && FindSpell(info, spellId) is { } spell)
        {
            WorldSpellDefinition definition = GameModel.Definitions.GetSpell(spellId);

            return new SiteOfferViewModel(
                SpellIconResolver.Resolve(GameModel.Definitions, spellId),
                SpellDetailsViewModel.Create(spellId, GameModel.Definitions, GameModel.Texts),
                GameModel.Texts.GetGlobalText(definition.NameTextId).Text,
                TrSpans("Level {0:*}", definition.Level),
                GameModel.Texts.GetGlobalText(definition.DescriptionTextId).Text,
                Tr("Learn"),
                new OfferStatusViewModel(new OfferPriceViewModel(spell.Cost, WorldState.Players[ViewPlayerId].Resources), FormatUnavailability(spell)),
                LearnCommand);
        }
        else
        {
            return null;
        }
    }
}
