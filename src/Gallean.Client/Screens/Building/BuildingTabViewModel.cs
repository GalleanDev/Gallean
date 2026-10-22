using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gallean.Common;
using Gallean.Engine;

namespace Gallean.Client;

internal sealed partial class BuildingTabViewModel : ManagementTabViewModel
{
    private static readonly TimeSpan RestorePanelsDelay = TimeSpan.FromSeconds(4.0);

    private WorldPlayerBuildingDefinitionId? selectedBuildingId;
    private WorldPlayerBuildingDefinitionId? constructedBuildingId;

    private readonly ManagementScreenViewModel screen;
    private readonly CleanupList assetLeases = new();

    public BuildingTabViewModel(GameModel gameModel, GameViewModel gameViewModel, ManagementScreenViewModel screen)
        : base(gameModel, gameViewModel)
    {
        this.screen = screen;

        Game.PropertyChanged += OnGamePropertyChanged;
        Game.Alert.PropertyChanged += OnAlertPropertyChanged;

        RefreshView();
        PreloadAssetsAsync().FileAndForget();
    }

    public override string TabHeader => Tr("Buildings");
    public bool IsInForeground => ReferenceEquals(Game.TopScreen, screen) && Game.Alert.Current is null;

    [ObservableProperty]
    public partial BuildingGraphViewModel Graph { get; private set; } = BuildingGraphViewModel.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ConstructText))]
    [NotifyCanExecuteChangedFor(nameof(ConstructCommand))]
    public partial BuildingNodeViewModel? SelectedBuilding { get; private set; }

    [ObservableProperty]
    public partial CapitalSceneViewModel? CapitalScene { get; private set; }

    [ObservableProperty]
    public partial bool PanelsVisible { get; private set; } = true;

    public string ConstructText => Tr("Build {0}", SelectedBuilding?.Name);

    protected override void OnSnapshotChanged(GameSnapshot current)
    {
        RefreshView();
    }

    protected override void OnSelectionChanged(bool isSelected)
    {
        if (!isSelected && constructedBuildingId is not null)
        {
            constructedBuildingId = null;
            PanelsVisible = true;
            RefreshView();
        }
    }

    protected override void OnDispose()
    {
        Game.PropertyChanged -= OnGamePropertyChanged;
        Game.Alert.PropertyChanged -= OnAlertPropertyChanged;
        assetLeases.Dispose();
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
    private void SelectBuilding(BuildingNodeViewModel building)
    {
        selectedBuildingId = building.BuildingId;

        ApplySelection();
    }

    [RelayCommand]
    private void ClearSelection()
    {
        selectedBuildingId = null;

        ApplySelection();
    }

    [RelayCommand(CanExecute = nameof(CanConstruct))]
    private async Task ConstructAsync()
    {
        if (SelectedBuilding is null)
        {
            return;
        }

        BuildingNodeViewModel building = SelectedBuilding;

        constructedBuildingId = building.BuildingId;

        if (await TryDispatchWorldActionAsync(new WorldConstructBuildingCommand(ViewPlayerId, building.BuildingId)))
        {
            Game.PlaySound(new AssetSoundKey.BuildingConstruct());
            Game.ShowInfo(IconMoniker.Capital, Tr("Building constructed"), building.Name);
            selectedBuildingId = null;
            PanelsVisible = false;
            RestorePanelsAsync(building.BuildingId).FileAndForget();
        }

        RefreshView();
    }

    [RelayCommand]
    private void ShowPanels()
    {
        PanelsVisible = true;
    }

    private async Task PreloadAssetsAsync()
    {
        assetLeases.Add(await Game.AssetPreloader.PreloadAsync(Graph.GetPreloadItems()));
    }

    private async Task RestorePanelsAsync(WorldPlayerBuildingDefinitionId buildingId)
    {
        await Task.Delay(RestorePanelsDelay);

        if (constructedBuildingId == buildingId)
        {
            PanelsVisible = true;
        }
    }

    private bool CanConstruct()
    {
        return SelectedBuilding is not null && SelectedBuilding.IsConstructible;
    }

    private void RefreshView()
    {
        WorldPlayer player = WorldState.Players[ViewPlayerId];
        WorldGetBuildingsInfoCommand.Result info = ResolveBuildingsInfo();
        LordDefinition lord = GameModel.Definitions.GetLord(player.LordId);

        BuildingGraphBuilder graphBuilder = new(
            GameModel.Definitions,
            GameModel.Texts,
            GameModel.Stats,
            lord,
            player.Buildings,
            player.Resources,
            info);

        Graph = graphBuilder.Build();
        CapitalScene = CapitalSceneViewModel.Create(player.Race, player.Buildings.Acquired, constructedBuildingId);

        ApplySelection();
    }

    private void ApplySelection()
    {
        BuildingNodeViewModel? selected = FindNode(selectedBuildingId);

        foreach (BuildingNodeViewModel node in Graph.Nodes)
        {
            node.IsSelected = ReferenceEquals(node, selected);
            node.IsSelectionLocked = selected is not null && selected.LockedIds.Contains(node.BuildingId);
        }

        SelectedBuilding = selected;
        selectedBuildingId = selected?.BuildingId;
    }

    private BuildingNodeViewModel? FindNode(WorldPlayerBuildingDefinitionId? buildingId)
    {
        foreach (BuildingNodeViewModel node in Graph.Nodes)
        {
            if (node.BuildingId == buildingId)
            {
                return node;
            }
        }

        return null;
    }

    private WorldGetBuildingsInfoCommand.Result ResolveBuildingsInfo()
    {
        ICommandResult<WorldCommandError, WorldEvent, WorldGetBuildingsInfoCommand.Result> result =
            GameModel.World.DryRunCommand(new WorldGetBuildingsInfoCommand(ViewPlayerId));

        if (result.Failed)
        {
            return new WorldGetBuildingsInfoCommand.Result([]);
        }
        else
        {
            return result.Data;
        }
    }
}
