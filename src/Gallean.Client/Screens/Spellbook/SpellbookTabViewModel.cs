using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gallean.Engine;

namespace Gallean.Client;

internal sealed partial class SpellbookTabViewModel : ManagementTabViewModel
{
    private WorldSpellDefinitionId? selectedSpellId;

    public SpellbookTabViewModel(GameModel gameModel, GameViewModel gameViewModel, ManagementScreenViewModel screen)
        : base(gameModel, gameViewModel)
    {
        Screen = screen;

        Game.PropertyChanged += OnGamePropertyChanged;
        Game.Alert.PropertyChanged += OnAlertPropertyChanged;

        RefreshView();
    }

    private static string ScenarioLockedText => Tr("Not available in this quest");

    public ManagementScreenViewModel Screen { get; }

    public override string TabHeader => Tr("Spellbook");
    public override AssetSoundKey CloseSound => new AssetSoundKey.BookClose();
    public bool IsInForeground => ReferenceEquals(Game.TopScreen, Screen) && Game.Alert.Current is null;

    [ObservableProperty]
    public partial IReadOnlyList<SpellbookLevelViewModel> Levels { get; private set; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ActivateCommand))]
    [NotifyCanExecuteChangedFor(nameof(CastSelectedCommand))]
    public partial SpellbookItemViewModel? SelectedSpell { get; private set; }

    [ObservableProperty]
    public partial SpellbookBuildingViewModel? RequiredBuilding { get; private set; }

    [ObservableProperty]
    public partial string ResearchedCountText { get; private set; } = string.Empty;

    public async Task ConstructAsync(SpellbookBuildingViewModel building)
    {
        if (await TryDispatchWorldActionAsync(new WorldConstructBuildingCommand(ViewPlayerId, building.BuildingId)))
        {
            Game.PlaySound(new AssetSoundKey.BuildingConstruct());
            Game.ShowInfo(IconMoniker.Capital, Tr("Building constructed"), building.Name);
        }

        RefreshView();
    }

    protected override void OnSnapshotChanged(GameSnapshot current)
    {
        RefreshView();
    }

    protected override void OnDispose()
    {
        Game.PropertyChanged -= OnGamePropertyChanged;
        Game.Alert.PropertyChanged -= OnAlertPropertyChanged;
    }

    private void OnGamePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(GameViewModel.TopScreen))
        {
            OnPropertyChanged(nameof(IsInForeground));
        }
    }

    private void OnAlertPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(AlertViewModel.Current))
        {
            OnPropertyChanged(nameof(IsInForeground));
        }
    }

    [RelayCommand]
    private void SelectSpell(SpellbookItemViewModel spell)
    {
        selectedSpellId = spell.SpellId;

        ApplySelection();
    }

    [RelayCommand]
    private void ClearSelection()
    {
        selectedSpellId = null;

        ApplySelection();
    }

    [RelayCommand(CanExecute = nameof(CanActivate))]
    private async Task ActivateAsync()
    {
        if (SelectedSpell is not null)
        {
            SpellbookItemViewModel item = SelectedSpell;

            Task action = item.Type switch
            {
                SpellbookItemType.Known => CastAsync(item),
                SpellbookItemType.Researchable => ResearchAsync(item),
                SpellbookItemType.Locked => Task.CompletedTask
            };

            await action;
        }
    }

    private bool CanActivate()
    {
        return SelectedSpell is not null && SelectedSpell.Status.IsAvailable;
    }

    [RelayCommand(CanExecute = nameof(CanCastSelected))]
    private async Task CastSelectedAsync()
    {
        if (SelectedSpell is not null)
        {
            await CastAsync(SelectedSpell);
        }
    }

    private bool CanCastSelected()
    {
        return SelectedSpell is { Type: SpellbookItemType.Known, Status.IsAvailable: true };
    }

    private async Task CastAsync(SpellbookItemViewModel item)
    {
        if (item.IsTargeted)
        {
            BeginCast(item);
        }
        else
        {
            await CastGlobalSpellAsync(item);
        }
    }

    private void BeginCast(SpellbookItemViewModel item)
    {
        if (Game.RootScreen is WorldScreenViewModel world)
        {
            Screen.Close();
            world.SelectAction(new WorldCursorAction.CastSpell(item.SpellId));
        }
    }

    private async Task CastGlobalSpellAsync(SpellbookItemViewModel item)
    {
        if (await TryDispatchWorldActionAsync(new WorldCastSpellCommand(item.SpellId, Target: null)))
        {
            Screen.Close();
        }
        else
        {
            RefreshView();
        }
    }

    private async Task ResearchAsync(SpellbookItemViewModel item)
    {
        if (await TryDispatchWorldActionAsync(new WorldResearchSpellCommand(ViewPlayerId, item.SpellId)))
        {
            selectedSpellId = null;

            Game.PlaySound(new AssetSoundKey.SpellResearch());
        }

        RefreshView();
    }

    private void RefreshView()
    {
        WorldGetSpellbookInfoCommand.Result info = ResolveSpellbookInfo();
        List<SpellbookItemViewModel> items = [.. BuildKnown(info), .. BuildResearchable(info), .. BuildLocked(info)];

        Levels = BuildLevels(items);
        RequiredBuilding = BuildRequiredBuilding(info);
        ResearchedCountText = Tr("{0} of {1} spells researched", info.Acquired.Length, items.Count);

        ApplySelection();
    }

    private void ApplySelection()
    {
        SpellbookItemViewModel? selected = FindItem(selectedSpellId);

        foreach (SpellbookLevelViewModel level in Levels)
        {
            foreach (SpellbookItemViewModel item in level.Spells)
            {
                item.IsSelected = ReferenceEquals(item, selected);
            }
        }

        SelectedSpell = selected;
        selectedSpellId = selected?.SpellId;
    }

    private SpellbookItemViewModel? FindItem(WorldSpellDefinitionId? spellId)
    {
        foreach (SpellbookLevelViewModel level in Levels)
        {
            foreach (SpellbookItemViewModel item in level.Spells)
            {
                if (item.SpellId == spellId)
                {
                    return item;
                }
            }
        }

        return null;
    }

    private static IReadOnlyList<SpellbookLevelViewModel> BuildLevels(IReadOnlyList<SpellbookItemViewModel> items)
    {
        SortedDictionary<int, List<SpellbookItemViewModel>> spellsByLevel = [];

        foreach (SpellbookItemViewModel item in items)
        {
            if (!spellsByLevel.TryGetValue(item.Level, out List<SpellbookItemViewModel>? spells))
            {
                spells = [];
                spellsByLevel.Add(item.Level, spells);
            }

            spells.Add(item);
        }

        List<SpellbookLevelViewModel> levels = [];

        foreach (KeyValuePair<int, List<SpellbookItemViewModel>> entry in spellsByLevel)
        {
            entry.Value.Sort(CompareItems);

            levels.Add(new SpellbookLevelViewModel(Tr("Level {0}", entry.Key), entry.Value));
        }

        return levels;
    }

    private static int CompareItems(SpellbookItemViewModel first, SpellbookItemViewModel second)
    {
        return StringComparer.CurrentCulture.Compare(first.Details.Name, second.Details.Name);
    }

    private SpellbookBuildingViewModel? BuildRequiredBuilding(WorldGetSpellbookInfoCommand.Result info)
    {
        if (info.RequiredBuildingId is { } buildingId)
        {
            WorldPlayerBuildingDefinition building = GameModel.Definitions.GetPlayerBuilding(buildingId);

            return new SpellbookBuildingViewModel(
                this,
                buildingId,
                GameModel.Texts.GetGlobalText(building.NameTextId).Text,
                new OfferStatusViewModel(
                    new OfferPriceViewModel(building.Cost, WorldState.Players[ViewPlayerId].Resources),
                    FormatError(info.RequiredBuildingError)));
        }
        else
        {
            return null;
        }
    }

    private IEnumerable<SpellbookItemViewModel> BuildKnown(WorldGetSpellbookInfoCommand.Result info)
    {
        WorldResourceSet resources = WorldState.Players[ViewPlayerId].Resources;

        foreach (WorldGetSpellbookInfoCommand.Cast cast in info.Acquired)
        {
            OfferStatusViewModel status = new(new OfferPriceViewModel(cast.Cost, resources), FormatError(cast.Error));

            yield return BuildItem(cast.SpellId, SpellbookItemType.Known, status);
        }
    }

    private IEnumerable<SpellbookItemViewModel> BuildResearchable(WorldGetSpellbookInfoCommand.Result info)
    {
        WorldResourceSet resources = WorldState.Players[ViewPlayerId].Resources;

        foreach (WorldGetSpellbookInfoCommand.Research research in info.Available)
        {
            OfferStatusViewModel status = new(new OfferPriceViewModel(research.Cost, resources), FormatError(research.Error));

            yield return BuildItem(research.SpellId, SpellbookItemType.Researchable, status);
        }
    }

    private IEnumerable<SpellbookItemViewModel> BuildLocked(WorldGetSpellbookInfoCommand.Result info)
    {
        foreach (WorldSpellDefinitionId spellId in info.Locked)
        {
            yield return BuildItem(spellId, SpellbookItemType.Locked, new OfferStatusViewModel(new OfferPriceViewModel(WorldResourceSet.Empty), ScenarioLockedText));
        }
    }

    private SpellbookItemViewModel BuildItem(
        WorldSpellDefinitionId spellId,
        SpellbookItemType type,
        OfferStatusViewModel status)
    {
        WorldSpellDefinition spell = GameModel.Definitions.GetSpell(spellId);

        return new SpellbookItemViewModel(
            spellId,
            type,
            spell.Level,
            spell.AreaRadius is not null,
            status,
            SpellDetailsViewModel.Create(spellId, GameModel.Definitions, GameModel.Texts));
    }

    private WorldGetSpellbookInfoCommand.Result ResolveSpellbookInfo()
    {
        ICommandResult<WorldCommandError, WorldEvent, WorldGetSpellbookInfoCommand.Result> result =
            GameModel.World.DryRunCommand(new WorldGetSpellbookInfoCommand(ViewPlayerId));

        if (result.Failed)
        {
            return new WorldGetSpellbookInfoCommand.Result([], [], [], RequiredBuildingId: null, RequiredBuildingError: null);
        }
        else
        {
            return result.Data;
        }
    }

    private static string? FormatError(WorldCommandError? error)
    {
        if (error is { } value)
        {
            return WorldCommandErrorFormatter.Format(value);
        }
        else
        {
            return null;
        }
    }
}
