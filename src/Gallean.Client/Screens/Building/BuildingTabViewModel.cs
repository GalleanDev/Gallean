using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gallean.Common;
using Gallean.Engine;

namespace Gallean.Client;

internal sealed partial class BuildingTabViewModel : ManagementTabViewModel
{
    private static readonly TimeSpan RestoreUiDelay = TimeSpan.FromSeconds(4.0);

    private WorldPlayerBuildingDefinitionId? selectedBuildingId;
    private WorldPlayerBuildingDefinitionId? constructedBuildingId;

    private readonly CleanupList assetLeases = new();

    public BuildingTabViewModel(GameModel gameModel, GameViewModel gameViewModel, ManagementScreenViewModel screen)
        : base(gameModel, gameViewModel)
    {
        Screen = screen;

        Game.PropertyChanged += OnGamePropertyChanged;
        Game.Alert.PropertyChanged += OnAlertPropertyChanged;

        RefreshView();
        PreloadAssetsAsync().FileAndForget();
    }

    public override string TabHeader => Tr("Buildings");
    public ManagementScreenViewModel Screen { get; }
    public bool IsInForeground => ReferenceEquals(Game.TopScreen, Screen) && Game.Alert.Current is null;

    [ObservableProperty]
    public partial BuildingGraphViewModel Graph { get; private set; } = BuildingGraphViewModel.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ConstructText))]
    [NotifyCanExecuteChangedFor(nameof(ConstructCommand))]
    public partial BuildingNodeViewModel? SelectedBuilding { get; private set; }

    [ObservableProperty]
    public partial CapitalSceneViewModel? CapitalScene { get; private set; }

    [ObservableProperty]
    public partial bool IsUiVisible { get; private set; } = true;

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
            IsUiVisible = true;
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
        if (SelectedBuilding is null || SelectedBuilding.BuildingId is null)
        {
            return;
        }

        BuildingNodeViewModel building = SelectedBuilding;
        WorldPlayerBuildingDefinitionId buildingId = SelectedBuilding.BuildingId.Value;

        constructedBuildingId = buildingId;

        if (await TryDispatchWorldActionAsync(new WorldConstructBuildingCommand(ViewPlayerId, buildingId)))
        {
            Game.PlaySound(new AssetSoundKey.BuildingConstruct());
            Game.ShowInfo(IconMoniker.Capital, Tr("Building constructed"), building.Name);
            selectedBuildingId = null;
            IsUiVisible = false;
            RestoreUiAsync(buildingId).FileAndForget();
        }

        RefreshView();
    }

    [RelayCommand]
    private void ShowUi()
    {
        IsUiVisible = true;
    }

    [RelayCommand]
    private void ToggleUi()
    {
        IsUiVisible = !IsUiVisible;
    }

    private async Task PreloadAssetsAsync()
    {
        assetLeases.Add(await Game.AssetPreloader.PreloadAsync(Graph.GetPreloadItems()));
    }

    private async Task RestoreUiAsync(WorldPlayerBuildingDefinitionId buildingId)
    {
        await Task.Delay(RestoreUiDelay);

        if (constructedBuildingId == buildingId)
        {
            IsUiVisible = true;
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
        CapitalScene = CapitalSceneViewModel.Create(
            Game.AssetMetadata,
            player.Race,
            player.Buildings.Acquired,
            constructedBuildingId,
            GameModel.Definitions,
            GameModel.Texts);

        ApplySelection();
    }

    private void ApplySelection()
    {
        BuildingNodeViewModel? selected = FindNode(selectedBuildingId);

        foreach (BuildingNodeViewModel node in Graph.Nodes)
        {
            node.IsSelected = ReferenceEquals(node, selected);
            node.IsSelectionLocked = selected is not null && node.BuildingId is not null && selected.LockedIds.Contains(node.BuildingId.Value);
        }

        SelectedBuilding = selected;
        selectedBuildingId = selected?.BuildingId;
    }

    private BuildingNodeViewModel? FindNode(WorldPlayerBuildingDefinitionId? buildingId)
    {
        foreach (BuildingNodeViewModel node in Graph.Nodes)
        {
            if (node.BuildingId is not null && node.BuildingId == buildingId)
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
