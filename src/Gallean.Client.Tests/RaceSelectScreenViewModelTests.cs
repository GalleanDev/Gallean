using Gallean.Common;
using Gallean.Engine;
using Gallean.Tests.Common;

namespace Gallean.Client.Tests;

public sealed class RaceSelectScreenViewModelTests
{
    private static IReadOnlyList<string> FormatRaces(IReadOnlyList<RaceSelectItemViewModel> races)
    {
        List<string> rows = [];

        foreach (RaceSelectItemViewModel race in races)
        {
            rows.Add($"{race.Race} {race.Name} {race.PlayerType}");
        }

        return rows;
    }

    [Test]
    public void SelectionTest()
    {
        GameScenario scenario = new(
            "S217SC0000",
            "Fardale's Web",
            null,
            "Destroy the web",
            1,
            48,
            4,
            "Strategy First",
            [
                new ScenarioRace(WorldRace.Elf, new GlobalTextId("X000TG0013")),
                new ScenarioRace(WorldRace.Human, new GlobalTextId("X000TG0000"))
            ]);
        (_, IGameTextProvider texts, IGameDefinitionProvider definitions) = new GameLoader(new RandomProvider(0)).LoadCampaigns();
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        RaceSelectScreenViewModel viewModel = new(gameViewModel, NullAssetPreloader.Instance, texts, definitions, scenario);

        Assert.That(FormatRaces(viewModel.Races), Is.EqualTo([
            "Elf Elven Alliance Human",
            "Human Empire Computer"
        ]));
        Assert.That(viewModel.SelectedRace, Is.SameAs(viewModel.Races[0]));
        MenuBackgroundViewModel elvenBackground = Assert.NotNull(viewModel.Background);
        Assert.That(elvenBackground.SpriteKey, Is.EqualTo(new AssetSpriteKey.RaceSelectionBackground(WorldRace.Elf)));

        viewModel.SelectedRace = viewModel.Races[1];

        Assert.That(viewModel.SelectedRace, Is.SameAs(viewModel.Races[1]));
        MenuBackgroundViewModel humanBackground = Assert.NotNull(viewModel.Background);
        Assert.That(humanBackground.SpriteKey, Is.EqualTo(new AssetSpriteKey.RaceSelectionBackground(WorldRace.Human)));
    }

    [Test]
    public void PlayerTypeTest()
    {
        GameScenario scenario = new(
            "S217SC0000",
            "Fardale's Web",
            null,
            "Destroy the web",
            1,
            48,
            4,
            "Strategy First",
            [
                new ScenarioRace(WorldRace.Elf, new GlobalTextId("X000TG0013")),
                new ScenarioRace(WorldRace.Human, new GlobalTextId("X000TG0000"))
            ]);
        (_, IGameTextProvider texts, IGameDefinitionProvider definitions) = new GameLoader(new RandomProvider(0)).LoadCampaigns();
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        RaceSelectScreenViewModel viewModel = new(gameViewModel, NullAssetPreloader.Instance, texts, definitions, scenario);

        viewModel.Races[1].PlayerType = PlayerType.Human;

        Assert.That(FormatRaces(viewModel.Races), Is.EqualTo([
            "Elf Elven Alliance Human",
            "Human Empire Human"
        ]));
        Assert.That(viewModel.NextCommand.CanExecute(null), Is.True);

        viewModel.Races[0].PlayerType = PlayerType.Computer;

        Assert.That(FormatRaces(viewModel.Races), Is.EqualTo([
            "Elf Elven Alliance Computer",
            "Human Empire Human"
        ]));
        Assert.That(viewModel.NextCommand.CanExecute(null), Is.True);

        viewModel.Races[1].PlayerType = PlayerType.Computer;

        Assert.That(FormatRaces(viewModel.Races), Is.EqualTo([
            "Elf Elven Alliance Computer",
            "Human Empire Computer"
        ]));
        Assert.That(viewModel.NextCommand.CanExecute(null), Is.False);
    }

    [Test]
    public async Task NextTest()
    {
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();

        await gameViewModel.ShowScenarioSelectCommand.ExecuteAsync(null);

        ScenarioSelectScreenViewModel scenarioSelect = Assert.IsOfType<ScenarioSelectScreenViewModel>(gameViewModel.RootScreen);

        scenarioSelect.StartCommand.Execute(null);

        RaceSelectScreenViewModel raceSelect = Assert.IsOfType<RaceSelectScreenViewModel>(gameViewModel.TopScreen);

        raceSelect.Races[0].PlayerType = PlayerType.Computer;
        raceSelect.Races[1].PlayerType = PlayerType.Human;
        raceSelect.NextCommand.Execute(null);

        ScenarioConfigurationScreenViewModel scenarioConfiguration = Assert.IsOfType<ScenarioConfigurationScreenViewModel>(gameViewModel.TopScreen);

        Assert.That(gameViewModel.RootScreen, Is.SameAs(scenarioSelect));
        Assert.That(scenarioConfiguration.PlayerCaption, Is.EqualTo("Elven Alliance"));
        Assert.That(scenarioConfiguration.IsLastPlayer, Is.True);
        Assert.That(
            scenarioConfiguration.GameConfiguration,
            Is.EqualTo(new GameConfiguration("S200SC0000", new ScenarioConfiguration([new ScenarioPlayerConfiguration(WorldRace.Elf, LordCategory.Warrior, new WorldObjectVariant(0))], WorldDifficulty.Average, new ScenarioLeader(new WorldUnit(UnitDetailsViewModel.PreviewUnitId, new UnitDefinitionId("G000UU8009"), Hitpoints: 135, Experience: 0, Level: 1, [], null, false), LeaderEquipment.Empty, WorldInventory.Empty, PendingLeaderUpgrades: 0, BattlesWon: 0)))));
        Assert.That(scenarioConfiguration.PortraitKey, Is.EqualTo(new AssetSpriteKey.LordFace(WorldRace.Elf, 0, IsLarge: true)));
    }

    [Test]
    public async Task HotseatNextTest()
    {
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();

        await gameViewModel.ShowScenarioSelectCommand.ExecuteAsync(null);

        ScenarioSelectScreenViewModel scenarioSelect = Assert.IsOfType<ScenarioSelectScreenViewModel>(gameViewModel.RootScreen);

        scenarioSelect.StartCommand.Execute(null);

        RaceSelectScreenViewModel raceSelect = Assert.IsOfType<RaceSelectScreenViewModel>(gameViewModel.TopScreen);

        raceSelect.Races[1].PlayerType = PlayerType.Human;
        raceSelect.NextCommand.Execute(null);

        ScenarioConfigurationScreenViewModel firstPlayer = Assert.IsOfType<ScenarioConfigurationScreenViewModel>(gameViewModel.TopScreen);

        Assert.That(firstPlayer.PlayerCaption, Is.EqualTo("Player 1 of 2 · Undead Hordes"));
        Assert.That(firstPlayer.IsLastPlayer, Is.False);
        Assert.That(firstPlayer.PortraitKey, Is.EqualTo(new AssetSpriteKey.LordFace(WorldRace.Undead, 0, IsLarge: true)));

        firstPlayer.SelectedLordCategory = LordCategory.Mage;
        firstPlayer.SelectedDifficulty = WorldDifficulty.Hard;
        firstPlayer.NextLordFaceCommand.Execute(null);
        firstPlayer.NextCommand.Execute(null);

        ScenarioConfigurationScreenViewModel secondPlayer = Assert.IsOfType<ScenarioConfigurationScreenViewModel>(gameViewModel.TopScreen);

        Assert.That(firstPlayer.IsOpen, Is.True);
        Assert.That(secondPlayer.PlayerCaption, Is.EqualTo("Player 2 of 2 · Elven Alliance"));
        Assert.That(secondPlayer.IsLastPlayer, Is.True);
        Assert.That(secondPlayer.SelectedDifficulty, Is.EqualTo(WorldDifficulty.Hard));
        Assert.That(secondPlayer.PortraitKey, Is.EqualTo(new AssetSpriteKey.LordFace(WorldRace.Elf, 0, IsLarge: true)));

        secondPlayer.SelectedLordCategory = LordCategory.Guildmaster;

        Assert.That(
            secondPlayer.GameConfiguration,
            Is.EqualTo(new GameConfiguration(
                "S200SC0000",
                new ScenarioConfiguration(
                    [
                        new ScenarioPlayerConfiguration(WorldRace.Undead, LordCategory.Mage, new WorldObjectVariant(1)),
                        new ScenarioPlayerConfiguration(WorldRace.Elf, LordCategory.Guildmaster, new WorldObjectVariant(0))
                    ],
                    WorldDifficulty.Hard,
                    Leader: null))));

        gameViewModel.EscapeCommand.Execute(null);

        Assert.That(gameViewModel.TopScreen, Is.SameAs(firstPlayer));
        Assert.That(secondPlayer.IsOpen, Is.False);
    }

    [Test]
    public async Task BackTest()
    {
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();

        await gameViewModel.ShowScenarioSelectCommand.ExecuteAsync(null);

        ScenarioSelectScreenViewModel scenarioSelect = Assert.IsOfType<ScenarioSelectScreenViewModel>(gameViewModel.RootScreen);

        scenarioSelect.StartCommand.Execute(null);

        RaceSelectScreenViewModel raceSelect = Assert.IsOfType<RaceSelectScreenViewModel>(gameViewModel.TopScreen);

        gameViewModel.EscapeCommand.Execute(null);

        Assert.That(gameViewModel.TopScreen, Is.SameAs(scenarioSelect));
        Assert.That(raceSelect.IsOpen, Is.False);
    }
}
