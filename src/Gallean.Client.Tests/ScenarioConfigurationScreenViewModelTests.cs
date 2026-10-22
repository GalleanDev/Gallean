using Gallean.Common;
using Gallean.Engine;
using Gallean.Tests.Common;

namespace Gallean.Client.Tests;

public sealed class ScenarioConfigurationScreenViewModelTests
{
    private static IReadOnlyList<string> FormatLeaders(IReadOnlyList<StartingLeaderViewModel> leaders)
    {
        List<string> rows = [];

        foreach (StartingLeaderViewModel leader in leaders)
        {
            rows.Add($"{leader.Leader.Unit.DefinitionId} {leader.Category} {leader.Name}: {leader.Description}");
        }

        return rows;
    }

    [Test]
    public void SelectionTest()
    {
        GameScenario scenario = new(
            "S107SC0000",
            "The Necromancer's Spirit",
            null,
            "Destroy the cult",
            1,
            48,
            5,
            "Strategy First",
            []);
        (_, IGameTextProvider texts, IGameDefinitionProvider definitions) = new GameLoader(new RandomProvider(0)).LoadCampaigns();
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ScenarioConfigurationScreenViewModel viewModel = new(
            gameViewModel,
            texts,
            definitions,
            scenario,
            [new ScenarioRace(WorldRace.Human, new GlobalTextId("X000TG0000"))]);

        Assert.That(viewModel.PlayerCaption, Is.EqualTo("Empire"));
        Assert.That(viewModel.IsLastPlayer, Is.True);
        Assert.That(viewModel.LordCategories, Is.EqualTo([
            LordCategory.Warrior,
            LordCategory.Mage,
            LordCategory.Guildmaster
        ]));
        Assert.That(FormatLeaders(viewModel.Leaders), Is.EqualTo([
            "g000uu0019 Fighter Pegasus Knight: The Pegasus Knight is a noble fighter whose winged steed carries him high above forests and sea.",
            "g000uu0020 Explorer Ranger: Traveling quickly around the realm, the Ranger is often sent on scouting missions by the King.",
            "g000uu0021 Mage Archmage: Master of the arcane, an Archmage is the only Empire leader who can use staffs and scrolls."
        ]));
        Assert.That(viewModel.Difficulties, Is.EqualTo([
            WorldDifficulty.Easy,
            WorldDifficulty.Average,
            WorldDifficulty.Hard,
            WorldDifficulty.VeryHard
        ]));
        Assert.That(viewModel.SelectedLordCategory, Is.EqualTo(LordCategory.Warrior));
        Assert.That(viewModel.SelectedLeader, Is.SameAs(viewModel.Leaders[0]));
        Assert.That(viewModel.SelectedDifficulty, Is.EqualTo(WorldDifficulty.Average));
        Assert.That(
            viewModel.SelectedLordDescription,
            Is.EqualTo("The Warrior Lord's units recover 15% of their lost hit points each day, but spell research is limited to level four."));
        Assert.That(viewModel.PortraitKey, Is.EqualTo(new AssetSpriteKey.LordFace(WorldRace.Human, 0, IsLarge: true)));
        Assert.That(viewModel.Background?.SpriteKey, Is.EqualTo(new AssetSpriteKey.RaceSelectionBackground(WorldRace.Human)));
        Assert.That(
            viewModel.GameConfiguration,
            Is.EqualTo(new GameConfiguration("S107SC0000", new ScenarioConfiguration([new ScenarioPlayerConfiguration(WorldRace.Human, LordCategory.Warrior, new WorldObjectVariant(0))], WorldDifficulty.Average, new ScenarioLeader(new WorldUnit(UnitDetailsViewModel.PreviewUnitId, new UnitDefinitionId("G000UU0019"), Hitpoints: 150, Experience: 0, Level: 1, [], null, false), LeaderEquipment.Empty, WorldInventory.Empty, PendingLeaderUpgrades: 0, BattlesWon: 0)))));

        viewModel.SelectedLordCategory = LordCategory.Mage;
        viewModel.SelectedLeader = viewModel.Leaders[2];
        viewModel.SelectedDifficulty = WorldDifficulty.Hard;
        viewModel.PreviousLordFaceCommand.Execute(null);

        Assert.That(
            viewModel.SelectedLordDescription,
            Is.EqualTo("The Mage Lord can research every spell at half cost and cast spells twice per day."));
        Assert.That(viewModel.PortraitKey, Is.EqualTo(new AssetSpriteKey.LordFace(WorldRace.Human, 3, IsLarge: true)));
        Assert.That(
            viewModel.GameConfiguration,
            Is.EqualTo(new GameConfiguration("S107SC0000", new ScenarioConfiguration([new ScenarioPlayerConfiguration(WorldRace.Human, LordCategory.Mage, new WorldObjectVariant(3))], WorldDifficulty.Hard, new ScenarioLeader(new WorldUnit(UnitDetailsViewModel.PreviewUnitId, new UnitDefinitionId("G000UU0021"), Hitpoints: 65, Experience: 0, Level: 1, [], null, false), LeaderEquipment.Empty, WorldInventory.Empty, PendingLeaderUpgrades: 0, BattlesWon: 0)))));

        viewModel.SelectedLordCategory = LordCategory.Guildmaster;
        viewModel.SelectedLeader = viewModel.Leaders[1];
        viewModel.NextLordFaceCommand.Execute(null);
        viewModel.NextLordFaceCommand.Execute(null);

        Assert.That(
            viewModel.SelectedLordDescription,
            Is.EqualTo("The Guildmaster's spell research is limited to level four, but thieves can perform more actions and cities cost 50% less to upgrade."));
        Assert.That(viewModel.PortraitKey, Is.EqualTo(new AssetSpriteKey.LordFace(WorldRace.Human, 1, IsLarge: true)));
        Assert.That(
            viewModel.GameConfiguration,
            Is.EqualTo(new GameConfiguration("S107SC0000", new ScenarioConfiguration([new ScenarioPlayerConfiguration(WorldRace.Human, LordCategory.Guildmaster, new WorldObjectVariant(1))], WorldDifficulty.Hard, new ScenarioLeader(new WorldUnit(UnitDetailsViewModel.PreviewUnitId, new UnitDefinitionId("G000UU0020"), Hitpoints: 90, Experience: 0, Level: 1, [], null, false), LeaderEquipment.Empty, WorldInventory.Empty, PendingLeaderUpgrades: 0, BattlesWon: 0)))));
    }

    [Test]
    public async Task StartGameTest()
    {
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();

        await gameViewModel.ShowCampaignSelectCommand.ExecuteAsync(null);

        CampaignSelectScreenViewModel campaignSelect = Assert.IsOfType<CampaignSelectScreenViewModel>(gameViewModel.RootScreen);

        campaignSelect.SelectedCampaign = campaignSelect.Campaigns[1];

        campaignSelect.StartCommand.Execute(null);

        ScenarioConfigurationScreenViewModel scenarioConfiguration = Assert.IsOfType<ScenarioConfigurationScreenViewModel>(gameViewModel.TopScreen);

        Assert.That(gameViewModel.RootScreen, Is.SameAs(campaignSelect));
        Assert.That(
            scenarioConfiguration.GameConfiguration,
            Is.EqualTo(new GameConfiguration("S107SC0000", new ScenarioConfiguration([new ScenarioPlayerConfiguration(WorldRace.Human, LordCategory.Warrior, new WorldObjectVariant(0))], WorldDifficulty.Average, new ScenarioLeader(new WorldUnit(UnitDetailsViewModel.PreviewUnitId, new UnitDefinitionId("G000UU0019"), Hitpoints: 150, Experience: 0, Level: 1, [], null, false), LeaderEquipment.Empty, WorldInventory.Empty, PendingLeaderUpgrades: 0, BattlesWon: 0)))));

        scenarioConfiguration.SelectedLordCategory = LordCategory.Mage;
        scenarioConfiguration.SelectedLeader = scenarioConfiguration.Leaders[1];
        scenarioConfiguration.SelectedDifficulty = WorldDifficulty.VeryHard;
        scenarioConfiguration.NextLordFaceCommand.Execute(null);
        scenarioConfiguration.NextLordFaceCommand.Execute(null);
        await gameViewModel.StartGameCommand.ExecuteAsync(scenarioConfiguration.GameConfiguration);

        BriefingScreenViewModel briefing = Assert.IsOfType<BriefingScreenViewModel>(gameViewModel.TopScreen);

        await briefing.ContinueCommand.ExecuteAsync(null);

        WorldScreenViewModel world = Assert.IsOfType<WorldScreenViewModel>(gameViewModel.RootScreen);
        GameSnapshot snapshot = world.GameModel.Snapshot;
        WorldPlayer player = snapshot.WorldState.Players[snapshot.ViewPlayerId];
        WorldStack stack = snapshot.WorldState.Stacks[new WorldStackId("S107KC0000")];
        (WorldUnit leader, _, _) = stack.GetLeader(world.GameModel.Definitions);

        Assert.That(snapshot.WorldState.Difficulty, Is.EqualTo(WorldDifficulty.VeryHard));
        Assert.That(player.LordId, Is.EqualTo(new LordDefinitionId("G000LR0003")));
        Assert.That(player.FaceVariant, Is.EqualTo(new WorldObjectVariant(2)));
        Assert.That(player.Resources, Is.EqualTo(new WorldResourceSet(175, 0, 0, 25, 0, 0)));
        Assert.That(leader, Is.EqualTo(new WorldUnit(new WorldUnitId("S107UN0001"), new UnitDefinitionId("G000UU0020"), Hitpoints: 90, Experience: 0, Level: 1, [], Name: "Winfrid", IsTypeLocked: false)));
        Assert.That(campaignSelect.IsOpen, Is.False);
        Assert.That(scenarioConfiguration.IsOpen, Is.False);
    }

    [Test]
    public async Task HotseatStartGameTest()
    {
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();

        await gameViewModel.ShowScenarioSelectCommand.ExecuteAsync(null);

        ScenarioSelectScreenViewModel scenarioSelect = Assert.IsOfType<ScenarioSelectScreenViewModel>(gameViewModel.RootScreen);

        scenarioSelect.StartCommand.Execute(null);

        RaceSelectScreenViewModel raceSelect = Assert.IsOfType<RaceSelectScreenViewModel>(gameViewModel.TopScreen);

        raceSelect.Races[1].PlayerType = PlayerType.Human;
        raceSelect.NextCommand.Execute(null);

        ScenarioConfigurationScreenViewModel firstPlayer = Assert.IsOfType<ScenarioConfigurationScreenViewModel>(gameViewModel.TopScreen);

        Assert.That(firstPlayer.Leaders, Is.Empty);

        firstPlayer.SelectedLordCategory = LordCategory.Mage;
        firstPlayer.SelectedDifficulty = WorldDifficulty.Hard;
        firstPlayer.NextLordFaceCommand.Execute(null);
        firstPlayer.NextCommand.Execute(null);

        ScenarioConfigurationScreenViewModel secondPlayer = Assert.IsOfType<ScenarioConfigurationScreenViewModel>(gameViewModel.TopScreen);

        secondPlayer.SelectedLordCategory = LordCategory.Guildmaster;
        secondPlayer.NextLordFaceCommand.Execute(null);
        secondPlayer.NextLordFaceCommand.Execute(null);
        await gameViewModel.StartGameCommand.ExecuteAsync(secondPlayer.GameConfiguration);

        BriefingScreenViewModel briefing = Assert.IsOfType<BriefingScreenViewModel>(gameViewModel.TopScreen);

        await briefing.ContinueCommand.ExecuteAsync(null);

        WorldScreenViewModel world = Assert.IsOfType<WorldScreenViewModel>(gameViewModel.RootScreen);
        GameSnapshot snapshot = world.GameModel.Snapshot;
        WorldPlayer undead = snapshot.WorldState.Players[new WorldPlayerId(2)];
        WorldPlayer elf = snapshot.WorldState.Players[new WorldPlayerId(3)];

        Assert.That(snapshot.ViewPlayerId, Is.EqualTo(new WorldPlayerId(2)));
        Assert.That(snapshot.WorldState.TurnInfo.CurrentPlayerId, Is.EqualTo(new WorldPlayerId(2)));
        Assert.That(snapshot.WorldState.Difficulty, Is.EqualTo(WorldDifficulty.Hard));
        Assert.That(undead.IsAi, Is.False);
        Assert.That(undead.LordId, Is.EqualTo(new LordDefinitionId("g000LR0012")));
        Assert.That(undead.FaceVariant, Is.EqualTo(new WorldObjectVariant(1)));
        Assert.That(elf.IsAi, Is.False);
        Assert.That(elf.LordId, Is.EqualTo(new LordDefinitionId("g000LR0017")));
        Assert.That(elf.FaceVariant, Is.EqualTo(new WorldObjectVariant(2)));
        Assert.That(scenarioSelect.IsOpen, Is.False);
        Assert.That(raceSelect.IsOpen, Is.False);
        Assert.That(firstPlayer.IsOpen, Is.False);
        Assert.That(secondPlayer.IsOpen, Is.False);
    }

    [Test]
    public async Task BackTest()
    {
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();

        await gameViewModel.ShowCampaignSelectCommand.ExecuteAsync(null);

        CampaignSelectScreenViewModel campaignSelect = Assert.IsOfType<CampaignSelectScreenViewModel>(gameViewModel.RootScreen);

        campaignSelect.StartCommand.Execute(null);

        ScenarioConfigurationScreenViewModel scenarioConfiguration = Assert.IsOfType<ScenarioConfigurationScreenViewModel>(gameViewModel.TopScreen);

        gameViewModel.EscapeCommand.Execute(null);

        Assert.That(gameViewModel.TopScreen, Is.SameAs(campaignSelect));
        Assert.That(scenarioConfiguration.IsOpen, Is.False);
    }
}
