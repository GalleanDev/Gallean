using Gallean.Common;
using Gallean.Configuration;
using Gallean.Engine;
using Gallean.Tests.Common;

namespace Gallean.Client.Tests;

public sealed class GameMenuViewModelTests
{
    private static readonly WorldItem Banner = new(new WorldItemId("S202IM0001"), new ItemDefinitionId("G000IG1001"), Charges: null);

    private static GameModel LoadSeasonedHero(GameLoader gameLoader)
    {
        ScenarioConfiguration configuration = new([new ScenarioPlayerConfiguration(WorldRace.Human, LordCategory.Warrior, new WorldObjectVariant(1), null)], WorldDifficulty.Hard, Leader: null);
        (GameSession session, IGameTextProvider texts) = gameLoader.CreateScenario("S202SC0000", configuration, Language.English);
        GameModel start = new(session, texts, NullLog.Instance, SynchronousTaskScheduler.Instance);
        WorldState state = start.Snapshot.WorldState;
        WorldStack stack = state.Stacks[new WorldStackId("S202KC0000")];
        (WorldUnit leader, _, _) = stack.GetLeader(start.Definitions);
        WorldStack seasoned = stack with
        {
            Units = stack.Units.Replace(leader, leader with { Level = 12, Experience = 300 }),
            Equipment = LeaderEquipment.Empty.With(LeaderEquipmentSlot.Banner, Banner),
            BattlesWon = 34
        };
        (GameSession seasonedSession, IGameTextProvider seasonedTexts) = gameLoader.LoadScenario(
            "S202SC0000",
            state with { Stacks = state.Stacks.SetItem(stack.Id, seasoned) },
            Language.English);

        return new GameModel(seasonedSession, seasonedTexts, NullLog.Instance, SynchronousTaskScheduler.Instance);
    }

    private static GameModel LoadWithoutLeaders(GameLoader gameLoader)
    {
        ScenarioConfiguration configuration = new([new ScenarioPlayerConfiguration(WorldRace.Human, LordCategory.Warrior, new WorldObjectVariant(1), null)], WorldDifficulty.Hard, Leader: null);
        (GameSession session, IGameTextProvider texts) = gameLoader.CreateScenario("S202SC0000", configuration, Language.English);
        GameModel start = new(session, texts, NullLog.Instance, SynchronousTaskScheduler.Instance);
        WorldState state = start.Snapshot.WorldState;
        (GameSession leaderlessSession, IGameTextProvider leaderlessTexts) = gameLoader.LoadScenario(
            "S202SC0000",
            state with { Stacks = state.Stacks.Remove(new WorldStackId("S202KC0000")) },
            Language.English);

        return new GameModel(leaderlessSession, leaderlessTexts, NullLog.Instance, SynchronousTaskScheduler.Instance);
    }

    private static LeaderExportSelectScreenViewModel OpenExport(GameModel gameModel, GameViewModel gameViewModel)
    {
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();
        world.OpenGameMenuCommand.Execute(null);

        GameMenuViewModel menu = Assert.IsOfType<GameMenuViewModel>(gameViewModel.TopScreen);

        Assert.That(menu.ExportLeaderCommand.CanExecute(null), Is.True);

        menu.ExportLeaderCommand.Execute(null);

        return Assert.IsOfType<LeaderExportSelectScreenViewModel>(gameViewModel.TopScreen);
    }

    private static IReadOnlyList<string> FormatLeaders(IReadOnlyList<LeaderExportLeaderViewModel> leaders)
    {
        List<string> rows = [];

        foreach (LeaderExportLeaderViewModel leader in leaders)
        {
            rows.Add($"{leader.StackId} {leader.Name} {leader.LevelText} | {leader.BattlesWonText} | items={leader.ItemIcons.Count}");
        }

        return rows;
    }

    [Test]
    public async Task ExportLeaderTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel gameModel = LoadSeasonedHero(new GameLoader(new RandomProvider(0)));
        LeaderStore store = new(sb.GetPath("leaders"));
        using GameViewModel gameViewModel = new TestGameViewModelBuilder().WithLeaderStore(store).Build();

        LeaderExportSelectScreenViewModel picker = OpenExport(gameModel, gameViewModel);

        Assert.That(FormatLeaders(picker.Leaders), Is.EqualTo([
            "S202KC0000 Edwin Lv. 12 | 34 battles won | items=1"
        ]));
        Assert.That(picker.SelectedLeader, Is.EqualTo(picker.Leaders[0]));
        Assert.That(picker.IsEmpty, Is.False);

        await picker.ExportCommand.ExecuteAsync(null);

        Assert.IsOfType<WorldScreenViewModel>(gameViewModel.TopScreen);
        Assert.That(picker.IsOpen, Is.False);
        Assert.That(gameViewModel.Alert.Current, Is.EqualTo(new Alert(AlertType.Info, IconMoniker.Human, "Leader exported", "Edwin")));
        Assert.That(await store.EnumerateAsync(), Is.EqualTo([
            new LeaderEntry(
                new LeaderId(1),
                new ScenarioLeader(
                    new WorldUnit(
                        new WorldUnitId("S202UN0001"),
                        new UnitDefinitionId("G000UU0019"),
                        Hitpoints: 150,
                        Experience: 300,
                        Level: 12,
                        [],
                        Name: new LiteralUnitName("Edwin"),
                        IsTypeLocked: false),
                    LeaderEquipment.Empty.With(LeaderEquipmentSlot.Banner, new WorldItem(new WorldItemId("Exported1"), new ItemDefinitionId("G000IG1001"), Charges: null)),
                    WorldInventory.Empty,
                    PendingLeaderUpgrades: 0,
                    BattlesWon: 34))
        ]));
    }

    [Test]
    public void NoLeadersTest()
    {
        GameModel gameModel = LoadWithoutLeaders(new GameLoader(new RandomProvider(0)));
        using GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();

        LeaderExportSelectScreenViewModel picker = OpenExport(gameModel, gameViewModel);

        Assert.That(FormatLeaders(picker.Leaders), Is.Empty);
        Assert.That(picker.IsEmpty, Is.True);
        Assert.That(picker.SelectedLeader, Is.Null);
        Assert.That(picker.ExportCommand.CanExecute(null), Is.False);
    }

    [Test]
    public async Task ExportFailureTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameModel gameModel = LoadSeasonedHero(new GameLoader(new RandomProvider(0)));
        LeaderStore store = new(sb.WriteTextFile("blocked", "a file where the directory belongs"));
        using GameViewModel gameViewModel = new TestGameViewModelBuilder().WithLeaderStore(store).Build();

        LeaderExportSelectScreenViewModel picker = OpenExport(gameModel, gameViewModel);

        await picker.ExportCommand.ExecuteAsync(null);

        Assert.IsOfType<ErrorScreenViewModel>(gameViewModel.TopScreen);
        Assert.That(picker.IsOpen, Is.True);
    }

    [Test]
    public async Task ImportIntoQuestTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameLoader gameLoader = new(new RandomProvider(0));
        GameModel gameModel = LoadSeasonedHero(gameLoader);
        LeaderStore store = new(sb.GetPath("leaders"));
        using GameViewModel gameViewModel = new TestGameViewModelBuilder().WithLeaderStore(store).Build();

        LeaderExportSelectScreenViewModel exportPicker = OpenExport(gameModel, gameViewModel);

        await exportPicker.ExportCommand.ExecuteAsync(null);

        (IReadOnlyList<GameCampaign> campaigns, IGameTextProvider menuTexts, IGameDefinitionProvider definitions) = gameLoader.LoadCampaigns(Language.English);
        GameScenario quest = campaigns[0].Scenarios[2];
        ScenarioRace human = quest.PlayableRaces[0];
        ScenarioConfigurationScreenViewModel lordScreen = new(gameViewModel, menuTexts, definitions, quest, [human]);
        gameViewModel.ShowScreen(lordScreen);

        Assert.That(quest.IsSkirmish, Is.True);

        await lordScreen.ImportLeaderCommand.ExecuteAsync(null);

        LeaderImportScreenViewModel importPicker = Assert.IsOfType<LeaderImportScreenViewModel>(gameViewModel.TopScreen);
        importPicker.ImportCommand.Execute(null);

        GameConfiguration gameConfiguration = lordScreen.GameConfiguration;
        ScenarioConfiguration scenarioConfiguration = Assert.NotNull(gameConfiguration.Configuration);
        (GameSession session, IGameTextProvider texts) = gameLoader.CreateScenario(gameConfiguration.ScenarioId, scenarioConfiguration, Language.English);
        GameModel questModel = new(session, texts, NullLog.Instance, SynchronousTaskScheduler.Instance);
        WorldStack stack = questModel.Snapshot.WorldState.Stacks[new WorldStackId("S202KC0000")];
        (WorldUnit leader, _, _) = stack.GetLeader(questModel.Definitions);

        Assert.That(gameConfiguration.ScenarioId, Is.EqualTo("S202SC0000"));
        Assert.That(leader.Name, Is.EqualTo(new LiteralUnitName("Edwin")));
        Assert.That(leader.Level, Is.EqualTo(12));
        Assert.That(leader.Experience, Is.EqualTo(300));
        Assert.That(stack.BattlesWon, Is.EqualTo(34));
        Assert.That(stack.Equipment, Is.EqualTo(LeaderEquipment.Empty.With(
            LeaderEquipmentSlot.Banner,
            new WorldItem(new WorldItemId("Exported1"), new ItemDefinitionId("G000IG1001"), Charges: null))));
    }
}
