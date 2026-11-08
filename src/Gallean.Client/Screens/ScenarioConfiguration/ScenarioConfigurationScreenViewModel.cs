using System.Collections.Immutable;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gallean.Common;
using Gallean.Configuration;
using Gallean.Engine;

namespace Gallean.Client;

internal sealed partial class ScenarioConfigurationScreenViewModel : AbstractScreenViewModel
{
    private const int MaxLeaderNameLength = 20;

    private readonly IGameTextProvider texts;
    private readonly IGameDefinitionProvider definitions;
    private readonly GameScenario scenario;
    private readonly ImmutableArray<ScenarioRace> humanRaces;
    private readonly ScenarioConfiguration configuration;
    private readonly int playerIndex;
    private readonly WorldRace race;

    public ScenarioConfigurationScreenViewModel(
        GameViewModel gameViewModel,
        IGameTextProvider texts,
        IGameDefinitionProvider definitions,
        GameScenario scenario,
        ImmutableArray<ScenarioRace> humanRaces)
        : this(gameViewModel, texts, definitions, scenario, humanRaces, CreateConfiguration(humanRaces), 0)
    {
    }

    private ScenarioConfigurationScreenViewModel(
        GameViewModel gameViewModel,
        IGameTextProvider texts,
        IGameDefinitionProvider definitions,
        GameScenario scenario,
        ImmutableArray<ScenarioRace> humanRaces,
        ScenarioConfiguration configuration,
        int playerIndex)
    {
        this.texts = texts;
        this.definitions = definitions;
        this.scenario = scenario;
        this.humanRaces = humanRaces;
        this.configuration = configuration;
        this.playerIndex = playerIndex;

        ScenarioPlayerConfiguration player = configuration.HumanPlayers[playerIndex];
        GlobalText raceName = texts.GetGlobalText(humanRaces[playerIndex].NameTextId);

        race = player.Race;
        Game = gameViewModel;
        LordCategories = Enum.GetValues<LordCategory>();
        Difficulties = Enum.GetValues<WorldDifficulty>();
        Background = MenuBackgroundViewModel.TryCreateRaceSelection(race);
        PlayerCaption = FormatPlayerCaption(raceName.Text, playerIndex, humanRaces.Length);
        IsLastPlayer = playerIndex == humanRaces.Length - 1;
        CanImportLeader = scenario.IsSkirmish && humanRaces.Length == 1;
        Leaders = BuildLeaders(texts, definitions, race, scenario.SuggestedLevel);
        SelectedLordCategory = player.LordCategory;
        SelectedLeader = Leaders.Count > 0 ? Leaders[0] : null;
        SelectedDifficulty = configuration.Difficulty;
        LordFaceVariant = player.LordFaceVariant.Value;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedLordDescription))]
    [NotifyPropertyChangedFor(nameof(GameConfiguration))]
    public partial LordCategory SelectedLordCategory { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(GameConfiguration))]
    public partial StartingLeaderViewModel? SelectedLeader { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<StartingLeaderViewModel> Leaders { get; private set; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(GameConfiguration))]
    public partial string LeaderName { get; set; } = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(GameConfiguration))]
    public partial WorldDifficulty SelectedDifficulty { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PortraitKey))]
    [NotifyPropertyChangedFor(nameof(GameConfiguration))]
    public partial int LordFaceVariant { get; private set; }

    public GameViewModel Game { get; }
    public IReadOnlyList<LordCategory> LordCategories { get; }
    public IReadOnlyList<WorldDifficulty> Difficulties { get; }
    public MenuBackgroundViewModel? Background { get; }
    public string PlayerCaption { get; }
    public bool IsLastPlayer { get; }
    public bool CanImportLeader { get; }

    public string SelectedLordDescription => LordCategoryFormatter.FormatDescription(SelectedLordCategory);
    public AssetSpriteKey.LordFace PortraitKey => new(race, LordFaceVariant, IsLarge: true);

    public GameConfiguration GameConfiguration => new(scenario.Id, BuildConfiguration());

    public void ImportLeader(LeaderEntry entry)
    {
        List<StartingLeaderViewModel> leaders = [];
        StartingLeaderViewModel? imported = null;

        foreach (StartingLeaderViewModel leader in Leaders)
        {
            if (leader.Entry?.Id == entry.Id)
            {
                imported = leader;
            }

            leaders.Add(leader);
        }

        if (imported is null)
        {
            imported = CreateImportedLeader(entry, texts, definitions);
            leaders.Add(imported);
        }

        Leaders = leaders;
        SelectedLeader = imported;
    }

    private static ScenarioConfiguration CreateConfiguration(ImmutableArray<ScenarioRace> humanRaces)
    {
        ImmutableArray<ScenarioPlayerConfiguration>.Builder humanPlayers = ImmutableArray.CreateBuilder<ScenarioPlayerConfiguration>();

        foreach (ScenarioRace race in humanRaces)
        {
            ScenarioPlayerConfiguration playerConfiguration = new(race.Race, LordCategory.Warrior, new WorldObjectVariant(0), null, LeaderName: null);

            humanPlayers.Add(playerConfiguration);
        }

        return new ScenarioConfiguration(humanPlayers.ToImmutable(), WorldDifficulty.Average, Leader: null);
    }

    private static IReadOnlyList<StartingLeaderViewModel> BuildLeaders(IGameTextProvider texts, IGameDefinitionProvider definitions, WorldRace race, int level)
    {
        WorldRaceDefinition raceDefinition = definitions.GetRace(race);
        List<StartingLeaderViewModel> result = [];

        foreach (UnitDefinitionId leaderId in raceDefinition.Leaders)
        {
            UnitDefinition definition = definitions.GetUnit(leaderId);

            if (definition.LeaderCategory is UnitLeaderCategory.Fighter or UnitLeaderCategory.Explorer or UnitLeaderCategory.Mage)
            {
                GlobalText name = texts.GetGlobalText(definition.NameTextId);
                GlobalText description = texts.GetGlobalText(definition.DescriptionTextId);

                result.Add(new StartingLeaderViewModel(leaderId, definition.LeaderCategory.Value, FormatLeaderName(name.Text, level), description.Text, Entry: null));
            }
        }

        return result;
    }

    private static StartingLeaderViewModel CreateImportedLeader(LeaderEntry entry, IGameTextProvider texts, IGameDefinitionProvider definitions)
    {
        ScenarioLeader leader = entry.Leader;
        UnitDefinition definition = definitions.GetUnit(leader.Unit.DefinitionId);
        GlobalText description = texts.GetGlobalText(definition.DescriptionTextId);
        string name = UnitNameResolver.Resolve(leader.Unit, definitions, texts);

        return new StartingLeaderViewModel(
            leader.Unit.DefinitionId,
            definition.LeaderCategory.GetValueOrDefault(),
            Tr("{0} · Level {1}", name, leader.Unit.Level),
            description.Text,
            entry);
    }

    private static string FormatLeaderName(string name, int level)
    {
        if (level > 1)
        {
            return Tr("{0} · Level {1}", name, level);
        }
        else
        {
            return name;
        }
    }

    private static string FormatPlayerCaption(string raceName, int playerIndex, int playerCount)
    {
        if (playerCount > 1)
        {
            return Tr("Player {0} of {1} · {2}", playerIndex + 1, playerCount, raceName);
        }
        else
        {
            return raceName;
        }
    }

    private ScenarioConfiguration BuildConfiguration()
    {
        UnitDefinitionId? startingLeaderId;
        ScenarioLeader? importedLeader;

        if (SelectedLeader?.Entry is not null)
        {
            startingLeaderId = null;
            importedLeader = SelectedLeader.Entry.Leader;
        }
        else
        {
            startingLeaderId = SelectedLeader?.LeaderId;
            importedLeader = configuration.Leader;
        }

        ScenarioPlayerConfiguration player = new(race, SelectedLordCategory, new WorldObjectVariant(LordFaceVariant), startingLeaderId, BuildLeaderName());

        return configuration with
        {
            HumanPlayers = configuration.HumanPlayers.SetItem(playerIndex, player),
            Difficulty = SelectedDifficulty,
            Leader = importedLeader
        };
    }

    private UnitName? BuildLeaderName()
    {
        string name = LeaderName.Trim();

        if (name.Length > 0)
        {
            return new LiteralUnitName(name);
        }
        else
        {
            return null;
        }
    }

    [RelayCommand]
    private void EditLeaderName()
    {
        Game.OpenScreen(new TextPromptViewModel(
            Tr("Hero name"),
            Tr("Enter a name for your starting hero."),
            LeaderName,
            Tr("Default name"),
            MaxLeaderNameLength,
            name => LeaderName = name.Trim()));
    }

    [RelayCommand]
    private void PreviousLordFace()
    {
        LordFaceVariant = (LordFaceVariant - 1).Mod(AssetSpriteKey.LordFace.GetVariantCount(race));
    }

    [RelayCommand]
    private void NextLordFace()
    {
        LordFaceVariant = (LordFaceVariant + 1).Mod(AssetSpriteKey.LordFace.GetVariantCount(race));
    }

    [RelayCommand]
    private void Next()
    {
        Game.OpenScreen(new ScenarioConfigurationScreenViewModel(Game, texts, definitions, scenario, humanRaces, BuildConfiguration(), playerIndex + 1));
    }

    [RelayCommand(CanExecute = nameof(CanImportLeader))]
    private async Task ImportLeaderAsync()
    {
        IReadOnlyList<LeaderEntry> entries = await Game.EnumerateLeadersAsync();

        Game.OpenScreen(new LeaderImportScreenViewModel(Game, this, texts, definitions, race, entries));
    }

    protected override bool TryClose()
    {
        CloseSelf();

        return true;
    }
}
