using Gallean.Common;
using Gallean.Engine;
using Gallean.Tests.Common;

namespace Gallean.Client.Tests;

public sealed class LeaderExportScreenViewModelTests
{
    private static IReadOnlyList<string> FormatLeaders(IReadOnlyList<LeaderExportCandidateViewModel> leaders)
    {
        List<string> rows = [];

        foreach (LeaderExportCandidateViewModel leader in leaders)
        {
            rows.Add($"{leader.StackId} {leader.Name} | {leader.Summary}");
        }

        return rows;
    }

    private static IReadOnlyList<string> FormatItems(IReadOnlyList<LeaderExportItemViewModel> items)
    {
        List<string> rows = [];

        foreach (LeaderExportItemViewModel item in items)
        {
            rows.Add($"{item.Items[0].DefinitionId} {item.Name} {item.AvailableText} {item.SelectedText} add={item.AddCommand.CanExecute(null)} remove={item.RemoveCommand.CanExecute(null)}");
        }

        return rows;
    }

    [Test]
    public async Task ExportTest()
    {
        GameLoader gameLoader = new(new RandomProvider(0));
        ScenarioConfiguration configuration = new([new ScenarioPlayerConfiguration(WorldRace.Human, LordCategory.Warrior, new WorldObjectVariant(1))], WorldDifficulty.Hard, Leader: null);
        (GameSession session, IGameTextProvider texts) = gameLoader.CreateScenario("S107SC0000", configuration);
        GameModel gameModel = new(session, texts, NullLog.Instance, SynchronousTaskScheduler.Instance);
        using GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();

        LeaderExportScreenViewModel screen = new(gameModel, gameViewModel);
        gameViewModel.ShowScreen(screen);

        Assert.That(screen.Instruction, Is.EqualTo("Select a leader and up to 5 items to bring to the next quest."));
        Assert.That(FormatLeaders(screen.Leaders), Is.EqualTo([
            "S107KC0000 Hatton | Level 1 · 0 battles won"
        ]));
        Assert.That(screen.SelectedLeader, Is.EqualTo(screen.Leaders[0]));
        Assert.That(FormatItems(screen.Items), Is.EqualTo([
            "G000IG0001 Life Potion ×1 0 selected add=True remove=False",
            "G000IG0006 Potion of Restoration ×3 0 selected add=True remove=False"
        ]));
        Assert.That(screen.SelectedItemsText.Text, Is.EqualTo("Selected items: 0 / 5"));

        screen.Items[1].AddCommand.Execute(null);
        screen.Items[1].AddCommand.Execute(null);
        screen.Items[1].AddCommand.Execute(null);
        screen.Items[0].AddCommand.Execute(null);

        Assert.That(FormatItems(screen.Items), Is.EqualTo([
            "G000IG0001 Life Potion ×1 1 selected add=False remove=True",
            "G000IG0006 Potion of Restoration ×3 3 selected add=False remove=True"
        ]));
        Assert.That(screen.SelectedItemsText.Text, Is.EqualTo("Selected items: 4 / 5"));

        screen.Items[1].RemoveCommand.Execute(null);

        Assert.That(FormatItems(screen.Items), Is.EqualTo([
            "G000IG0001 Life Potion ×1 1 selected add=False remove=True",
            "G000IG0006 Potion of Restoration ×3 2 selected add=True remove=True"
        ]));

        await screen.ExportCommand.ExecuteAsync(null);

        ConfirmPromptViewModel prompt = Assert.IsOfType<ConfirmPromptViewModel>(gameViewModel.TopScreen);
        Assert.That(prompt.Message, Is.EqualTo(TextSpans.FromText("Continue with only 3 items?")));

        await prompt.ConfirmCommand.ExecuteAsync(null);

        BriefingScreenViewModel briefing = Assert.IsOfType<BriefingScreenViewModel>(gameViewModel.TopScreen);

        await briefing.ContinueCommand.ExecuteAsync(null);

        WorldScreenViewModel world = Assert.IsOfType<WorldScreenViewModel>(gameViewModel.RootScreen);
        GameSnapshot snapshot = world.GameModel.Snapshot;
        WorldPlayer player = snapshot.WorldState.Players[snapshot.ViewPlayerId];

        Assert.That(world.GameModel.ScenarioId, Is.EqualTo("S108SC0000"));
        Assert.That(world.GameModel.Scenario.Definition.NextScenarioId, Is.EqualTo(new ScenarioId("S095SC0000")));
        Assert.That(snapshot.WorldState.Difficulty, Is.EqualTo(WorldDifficulty.Hard));
        Assert.That(player.LordId, Is.EqualTo(new LordDefinitionId("G000LR0001")));
        Assert.That(player.FaceVariant, Is.EqualTo(new WorldObjectVariant(1)));
        Assert.That(snapshot.WorldState.Stacks[new WorldStackId("S108KC0000")], Is.EqualTo(new WorldStack(
            new WorldStackId("S108KC0000"),
            new WorldPlayerId(2),
            new WorldPosition(11, 63),
            WorldStackDirection.SouthWest,
            new UnitGrid<WorldUnit>.Builder()
                .Place(new WorldUnit(new WorldUnitId("S108UN0001"), new UnitDefinitionId("G000UU0019"), 150, 0, 1, [], Name: "Hatton", IsTypeLocked: false), GridPosition.FrontCenter, false)
                .Build(),
            MovementPoints: 20,
            new WorldInventory([
                new WorldItem(new WorldItemId("S107IM0000"), new ItemDefinitionId("G000IG0006"), Charges: null),
                new WorldItem(new WorldItemId("S107IM0001"), new ItemDefinitionId("G000IG0006"), Charges: null),
                new WorldItem(new WorldItemId("S107IM0030"), new ItemDefinitionId("G000IG0001"), Charges: null)
            ]),
            LeaderEquipment.Empty,
            new WorldSubraceId("S108SR0001"),
            AiPriority: 3,
            AiIgnore: false,
            Concealed: false,
            WorldStackOrder.Normal.Instance,
            ExpirationTurn: null,
            PendingLeaderUpgrades: 0,
            BattlesWon: 0,
            SourceTemplateId: null)));
    }

    [Test]
    public void BackTest()
    {
        GameLoader gameLoader = new(new RandomProvider(0));
        (GameSession session, IGameTextProvider texts) = gameLoader.LoadScenario("S107SC0000", worldState: null);
        GameModel gameModel = new(session, texts, NullLog.Instance, SynchronousTaskScheduler.Instance);
        using GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();

        MainMenuScreenViewModel mainMenu = new(gameViewModel, saves: []);
        gameViewModel.ShowScreen(mainMenu);
        gameViewModel.OpenScreen(new LeaderExportScreenViewModel(gameModel, gameViewModel));
        gameViewModel.EscapeCommand.Execute(null);

        Assert.That(gameViewModel.TopScreen, Is.SameAs(mainMenu));
    }

    [Test]
    public async Task ExportAllItemsTest()
    {
        GameLoader gameLoader = new(new RandomProvider(0));
        ScenarioConfiguration configuration = new([new ScenarioPlayerConfiguration(WorldRace.Human, LordCategory.Warrior, new WorldObjectVariant(1))], WorldDifficulty.Hard, Leader: null);
        (GameSession session, IGameTextProvider texts) = gameLoader.CreateScenario("S107SC0000", configuration);
        GameModel gameModel = new(session, texts, NullLog.Instance, SynchronousTaskScheduler.Instance);
        using GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();

        LeaderExportScreenViewModel screen = new(gameModel, gameViewModel);
        gameViewModel.ShowScreen(screen);

        screen.Items[1].AddCommand.Execute(null);
        screen.Items[1].AddCommand.Execute(null);
        screen.Items[1].AddCommand.Execute(null);
        screen.Items[0].AddCommand.Execute(null);

        Assert.That(screen.SelectedItemsText.Text, Is.EqualTo("Selected items: 4 / 5"));

        await screen.ExportCommand.ExecuteAsync(null);

        Assert.IsOfType<BriefingScreenViewModel>(gameViewModel.TopScreen);
    }
}
