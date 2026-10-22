using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gallean.Engine;

namespace Gallean.Client;

internal sealed partial class ScenarioSelectScreenViewModel : AbstractScreenViewModel
{
    private readonly IAssetPreloader assetPreloader;
    private readonly IGameTextProvider texts;
    private readonly IGameDefinitionProvider definitions;
    private readonly MusicQueue musicQueue;

    public ScenarioSelectScreenViewModel(
        GameViewModel gameViewModel,
        IAssetPreloader assetPreloader,
        IGameTextProvider texts,
        IGameDefinitionProvider definitions,
        IReadOnlyList<GameCampaign> campaigns)
    {
        this.assetPreloader = assetPreloader;
        this.texts = texts;
        this.definitions = definitions;
        musicQueue = MusicQueue.Create([new AssetSoundKey.MenuMusic()], isLooping: true);

        Game = gameViewModel;

        List<GameScenario> scenarios = [];

        foreach (GameCampaign campaign in campaigns)
        {
            if (campaign.IsSkirmish)
            {
                scenarios.AddRange(campaign.Scenarios);
            }
        }

        IOrderedEnumerable<GameScenario> orderedScenarios = scenarios
            .OrderBy(scenario => scenario.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(scenario => scenario.Id, StringComparer.OrdinalIgnoreCase);

        Scenarios = [.. orderedScenarios];
        SelectedScenario = Scenarios.Count > 0 ? Scenarios[0] : null;
        Background = MainMenuBackgroundViewModel.Create();
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCommand))]
    [NotifyPropertyChangedFor(nameof(SelectedScenarioName))]
    [NotifyPropertyChangedFor(nameof(SelectedScenarioDescription))]
    [NotifyPropertyChangedFor(nameof(SelectedScenarioObjective))]
    [NotifyPropertyChangedFor(nameof(SelectedScenarioLevel))]
    [NotifyPropertyChangedFor(nameof(SelectedScenarioMapSize))]
    [NotifyPropertyChangedFor(nameof(SelectedScenarioCityCount))]
    [NotifyPropertyChangedFor(nameof(SelectedScenarioRaces))]
    [NotifyPropertyChangedFor(nameof(SelectedScenarioCreator))]
    public partial GameScenario? SelectedScenario { get; set; }

    public GameViewModel Game { get; }
    public IReadOnlyList<GameScenario> Scenarios { get; }
    public MainMenuBackgroundViewModel Background { get; }

    public string? SelectedScenarioName => SelectedScenario?.Name;
    public string? SelectedScenarioDescription => SelectedScenario?.Description;
    public string? SelectedScenarioObjective => FormatObjective(SelectedScenario);
    public string? SelectedScenarioLevel => SelectedScenario?.SuggestedLevel.ToString();
    public string? SelectedScenarioMapSize => FormatMapSize(SelectedScenario);
    public string? SelectedScenarioCityCount => SelectedScenario?.CityCount.ToString();
    public string? SelectedScenarioRaces => FormatRaces(SelectedScenario, texts);
    public string? SelectedScenarioCreator => SelectedScenario?.Creator;

    public override ScreenMusic Music => ScreenMusic.Tracks(musicQueue);
    public override AssetSoundKey OpenSound => new AssetSoundKey.InterfaceOpen();

    private static string? FormatRaces(GameScenario? scenario, IGameTextProvider texts)
    {
        if (scenario is not null)
        {
            List<string> names = [];

            foreach (ScenarioRace race in scenario.PlayableRaces)
            {
                GlobalText name = texts.GetGlobalText(race.NameTextId);

                names.Add(name.Text);
            }

            return string.Join(", ", names);
        }
        else
        {
            return null;
        }
    }

    private static string? FormatObjective(GameScenario? scenario)
    {
        if (scenario?.Objective is not null)
        {
            return ScenarioFormatter.FormatObjective(scenario.Objective);
        }
        else
        {
            return null;
        }
    }

    private static string? FormatMapSize(GameScenario? scenario)
    {
        if (scenario is not null)
        {
            return $"{scenario.MapSize} × {scenario.MapSize}";
        }
        else
        {
            return null;
        }
    }

    [RelayCommand(CanExecute = nameof(CanStart))]
    private void Start()
    {
        if (SelectedScenario is null)
        {
            return;
        }

        Game.OpenScreen(new RaceSelectScreenViewModel(Game, assetPreloader, texts, definitions, SelectedScenario));
    }

    private bool CanStart()
    {
        return SelectedScenario is not null;
    }

    protected override bool TryClose()
    {
        Game.ShowMainMenuCommand.Execute(null);

        return true;
    }
}
