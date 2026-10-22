using System.Collections.Immutable;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gallean.Common;
using Gallean.Engine;

namespace Gallean.Client;

internal sealed partial class ScenarioConfigurationScreenViewModel : AbstractScreenViewModel
{
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

        if (humanRaces.Length == 1)
        {
            Leaders = BuildLeaders(texts, definitions, race);
        }
        else
        {
            Leaders = [];
        }

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
    [NotifyPropertyChangedFor(nameof(GameConfiguration))]
    public partial WorldDifficulty SelectedDifficulty { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PortraitKey))]
    [NotifyPropertyChangedFor(nameof(GameConfiguration))]
    public partial int LordFaceVariant { get; private set; }

    public GameViewModel Game { get; }
    public IReadOnlyList<LordCategory> LordCategories { get; }
    public IReadOnlyList<StartingLeaderViewModel> Leaders { get; }
    public IReadOnlyList<WorldDifficulty> Difficulties { get; }
    public MenuBackgroundViewModel? Background { get; }
    public string PlayerCaption { get; }
    public bool IsLastPlayer { get; }

    public string SelectedLordDescription => LordCategoryFormatter.FormatDescription(SelectedLordCategory);
    public AssetSpriteKey.LordFace PortraitKey => new(race, LordFaceVariant, IsLarge: true);
    public GameConfiguration GameConfiguration => new(scenario.Id, BuildConfiguration());

    private static ScenarioConfiguration CreateConfiguration(ImmutableArray<ScenarioRace> humanRaces)
    {
        ImmutableArray<ScenarioPlayerConfiguration>.Builder humanPlayers = ImmutableArray.CreateBuilder<ScenarioPlayerConfiguration>();

        foreach (ScenarioRace race in humanRaces)
        {
            ScenarioPlayerConfiguration playerConfiguration = new(race.Race, LordCategory.Warrior, new WorldObjectVariant(0));

            humanPlayers.Add(playerConfiguration);
        }

        return new ScenarioConfiguration(humanPlayers.ToImmutable(), WorldDifficulty.Average, Leader: null);
    }

    private static IReadOnlyList<StartingLeaderViewModel> BuildLeaders(IGameTextProvider texts, IGameDefinitionProvider definitions, WorldRace race)
    {
        WorldRaceDefinition raceDefinition = definitions.GetRace(race);
        List<StartingLeaderViewModel> result = [];

        foreach (UnitDefinitionId leaderId in raceDefinition.Leaders)
        {
            UnitDefinition definition = definitions.GetUnit(leaderId);

            if (definition.LeaderCategory is UnitLeaderCategory.Fighter or UnitLeaderCategory.Explorer or UnitLeaderCategory.Mage)
            {
                WorldUnit unit = new(UnitDetailsViewModel.PreviewUnitId, leaderId, definition.MaxHitpoints, 0, definition.Level, [], null, false);
                ScenarioLeader leader = new(unit, LeaderEquipment.Empty, WorldInventory.Empty, PendingLeaderUpgrades: 0, BattlesWon: 0);
                GlobalText name = texts.GetGlobalText(definition.NameTextId);
                GlobalText description = texts.GetGlobalText(definition.DescriptionTextId);

                result.Add(new StartingLeaderViewModel(leader, definition.LeaderCategory.Value, name.Text, description.Text));
            }
        }

        return result;
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
        ScenarioPlayerConfiguration player = new(race, SelectedLordCategory, new WorldObjectVariant(LordFaceVariant));

        return configuration with
        {
            HumanPlayers = configuration.HumanPlayers.SetItem(playerIndex, player),
            Difficulty = SelectedDifficulty,
            Leader = SelectedLeader?.Leader
        };
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

    protected override bool TryClose()
    {
        CloseSelf();

        return true;
    }
}
