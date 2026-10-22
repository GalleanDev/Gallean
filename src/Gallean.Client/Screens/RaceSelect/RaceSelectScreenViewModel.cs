using System.Collections.Immutable;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gallean.Common;
using Gallean.Engine;

namespace Gallean.Client;

internal sealed partial class RaceSelectScreenViewModel : AbstractScreenViewModel
{
    private readonly GameViewModel gameViewModel;
    private readonly IAssetPreloader assetPreloader;
    private readonly IGameTextProvider texts;
    private readonly IGameDefinitionProvider definitions;
    private readonly GameScenario scenario;
    private readonly CleanupList assetLeases = new();

    public RaceSelectScreenViewModel(
        GameViewModel gameViewModel,
        IAssetPreloader assetPreloader,
        IGameTextProvider texts,
        IGameDefinitionProvider definitions,
        GameScenario scenario)
    {
        this.gameViewModel = gameViewModel;
        this.assetPreloader = assetPreloader;
        this.texts = texts;
        this.definitions = definitions;
        this.scenario = scenario;

        List<RaceSelectItemViewModel> races = [];

        foreach (ScenarioRace race in scenario.PlayableRaces)
        {
            GlobalText name = texts.GetGlobalText(race.NameTextId);
            PlayerType playerType = races.Count > 0 ? PlayerType.Computer : PlayerType.Human;

            races.Add(new RaceSelectItemViewModel(this, race, name.Text, playerType));
        }

        Races = races;
        SelectedRace = Races.Count > 0 ? Races[0] : null;
    }

    [ObservableProperty]
    public partial RaceSelectItemViewModel? SelectedRace { get; set; }

    [ObservableProperty]
    public partial MenuBackgroundViewModel? Background { get; private set; }

    public IReadOnlyList<RaceSelectItemViewModel> Races { get; }

    public void OnPlayerTypeChanged()
    {
        NextCommand.NotifyCanExecuteChanged();
    }

    public override void Dispose()
    {
        assetLeases.Dispose();
    }

    protected override void OnOpened()
    {
        PreloadBackgroundsAsync().FileAndForget();
    }

    private async Task PreloadBackgroundsAsync()
    {
        List<AssetPreloadItem> items = [];

        foreach (RaceSelectItemViewModel race in Races)
        {
            MenuBackgroundViewModel? viewModel = MenuBackgroundViewModel.TryCreateRaceSelection(race.Race);

            if (viewModel is not null)
            {
                items.AddRange(viewModel.GetPreloadItems());
            }
        }

        assetLeases.Add(await assetPreloader.PreloadAsync(items));
    }

    partial void OnSelectedRaceChanged(RaceSelectItemViewModel? value)
    {
        if (value is null)
        {
            Background = null;
        }
        else
        {
            MenuBackgroundViewModel? viewModel = MenuBackgroundViewModel.TryCreateRaceSelection(value.Race);

            if (Background?.SpriteKey != viewModel?.SpriteKey)
            {
                Background = viewModel;
            }
        }
    }

    [RelayCommand(CanExecute = nameof(CanNext))]
    private void Next()
    {
        ImmutableArray<ScenarioRace>.Builder humanRaces = ImmutableArray.CreateBuilder<ScenarioRace>();

        foreach (RaceSelectItemViewModel race in Races)
        {
            if (race.IsHuman)
            {
                humanRaces.Add(race.ScenarioRace);
            }
        }

        ScenarioConfigurationScreenViewModel configurationScreen = new(gameViewModel, texts, definitions, scenario, humanRaces.ToImmutable());
        gameViewModel.OpenScreen(configurationScreen);
    }

    private bool CanNext()
    {
        foreach (RaceSelectItemViewModel race in Races)
        {
            if (race.IsHuman)
            {
                return true;
            }
        }

        return false;
    }

    protected override bool TryClose()
    {
        CloseSelf();

        return true;
    }
}
