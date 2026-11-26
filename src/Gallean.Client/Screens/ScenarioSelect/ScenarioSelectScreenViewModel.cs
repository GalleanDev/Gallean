using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gallean.Configuration;
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
        IReadOnlyList<GameCampaign> campaigns,
        IReadOnlyList<GameScenario> customScenarios)
    {
        this.assetPreloader = assetPreloader;
        this.texts = texts;
        this.definitions = definitions;
        musicQueue = MusicQueue.Create([new AssetSoundKey.MenuMusic()], isLooping: true);

        Game = gameViewModel;

        List<GameScenario> scenarios = [.. customScenarios];

        foreach (GameCampaign campaign in campaigns)
        {
            if (campaign.IsSkirmish)
            {
                scenarios.AddRange(campaign.Scenarios);
            }
        }

        Scenarios = OrderScenarios(scenarios);
        SelectedScenario = Scenarios.Count > 0 ? Scenarios[0] : null;
        Background = MainMenuBackgroundViewModel.Create();
    }

    [ObservableProperty]
    public partial IReadOnlyList<GameScenario> Scenarios { get; private set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
    [NotifyPropertyChangedFor(nameof(IsCustomScenarioSelected))]
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
    public MainMenuBackgroundViewModel Background { get; }

    public bool IsCustomScenarioSelected => SelectedScenario?.CustomScenarioId is not null;
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

    private static IReadOnlyList<GameScenario> OrderScenarios(IEnumerable<GameScenario> scenarios)
    {
        List<GameScenario> result = [.. scenarios];

        result.Sort(CompareScenarios);

        return result;
    }

    private static int CompareScenarios(GameScenario left, GameScenario right)
    {
        int rv;

        rv = string.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase);
        if (rv != 0)
        {
            return rv;
        }

        rv = string.Compare(left.Id, right.Id, StringComparison.OrdinalIgnoreCase);
        if (rv != 0)
        {
            return rv;
        }

        rv = string.CompareOrdinal(left.CustomScenarioId?.Value, right.CustomScenarioId?.Value);
        if (rv != 0)
        {
            return rv;
        }

        return 0;
    }

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

    [RelayCommand]
    private async Task ImportAsync(IReadOnlyList<ScenarioImportFile>? files)
    {
        if (files is not null)
        {
            foreach (ScenarioImportFile file in files)
            {
                GameScenario? scenario = await Game.TryImportCustomScenarioAsync(file);

                if (scenario is not null)
                {
                    Scenarios = OrderScenarios([.. Scenarios.Where(existing => existing.CustomScenarioId != scenario.CustomScenarioId), scenario]);
                    SelectedScenario = scenario;
                }
            }
        }
    }

    [RelayCommand(CanExecute = nameof(CanDelete))]
    private void Delete()
    {
        if (SelectedScenario is not null && SelectedScenario.CustomScenarioId is not null)
        {
            CustomScenarioId id = SelectedScenario.CustomScenarioId.Value;

            Game.OpenScreen(new ConfirmPromptViewModel(
                TrSpans("Delete custom quest"),
                TrSpans("Delete \"{0}\" permanently?", SelectedScenario.Name),
                Tr("Delete"),
                ConfirmPromptType.Danger,
                () => DeleteAsync(id)));
        }
    }

    private bool CanDelete()
    {
        return IsCustomScenarioSelected;
    }

    private async Task DeleteAsync(CustomScenarioId id)
    {
        if (await Game.TryDeleteCustomScenarioAsync(id))
        {
            Scenarios = [.. Scenarios.Where(scenario => scenario.CustomScenarioId != id)];
            SelectedScenario = Scenarios.Count > 0 ? Scenarios[0] : null;
        }
    }

    protected override bool TryClose()
    {
        Game.ShowMainMenuCommand.Execute(null);

        return true;
    }
}
