using Gallean.Common;
using Gallean.Configuration;
using Gallean.Engine;
using Gallean.Tests.Common;

namespace Gallean.Client.Tests;

public sealed class ScenarioConfigurationScreenViewModelTests
{
    private static readonly ScenarioLeader Uther = new(
        new WorldUnit(
            new WorldUnitId("S107UN0001"),
            new UnitDefinitionId("G000UU0019"),
            Hitpoints: 180,
            Experience: 120,
            Level: 12,
            [],
            Name: new LiteralUnitName("Uther"),
            IsTypeLocked: false),
        LeaderEquipment.Empty.With(
            LeaderEquipmentSlot.Banner,
            new WorldItem(new WorldItemId("Exported1"), new ItemDefinitionId("G000IG1001"), Charges: null)),
        WorldInventory.Empty,
        PendingLeaderUpgrades: 1,
        BattlesWon: 34);

    private static readonly ScenarioLeader Elena = new(
        new WorldUnit(
            new WorldUnitId("S108UN0001"),
            new UnitDefinitionId("G000UU0021"),
            Hitpoints: 65,
            Experience: 0,
            Level: 3,
            [],
            Name: new LiteralUnitName("Elena"),
            IsTypeLocked: false),
        LeaderEquipment.Empty,
        WorldInventory.Empty,
        PendingLeaderUpgrades: 0,
        BattlesWon: 1);

    private static readonly ScenarioLeader Dwarf = new(
        new WorldUnit(
            new WorldUnitId("S109UN0001"),
            new UnitDefinitionId("G000UU0036"),
            Hitpoints: 120,
            Experience: 0,
            Level: 20,
            [],
            Name: new LiteralUnitName("Thorin"),
            IsTypeLocked: false),
        LeaderEquipment.Empty,
        WorldInventory.Empty,
        PendingLeaderUpgrades: 0,
        BattlesWon: 90);

    private static IReadOnlyList<string> FormatLeaders(IReadOnlyList<StartingLeaderViewModel> leaders)
    {
        List<string> rows = [];

        foreach (StartingLeaderViewModel leader in leaders)
        {
            rows.Add($"{leader.LeaderId} {leader.Category} {leader.Name}: {leader.Description}");
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
            IsSkirmish: false,
            []);
        (_, IGameTextProvider texts, IGameDefinitionProvider definitions) = new GameLoader(new RandomProvider(0)).LoadCampaigns(Language.English);
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
        Assert.That(viewModel.LeaderName, Is.EqualTo(""));
        Assert.That(
            viewModel.SelectedLordDescription,
            Is.EqualTo("The Warrior Lord's units recover 15% of their lost hit points each day, but spell research is limited to level four."));
        Assert.That(viewModel.PortraitKey, Is.EqualTo(new AssetSpriteKey.LordFace(WorldRace.Human, 0, IsLarge: true)));
        Assert.That(viewModel.Background?.SpriteKey, Is.EqualTo(new AssetSpriteKey.RaceSelectionBackground(WorldRace.Human)));
        Assert.That(
            viewModel.GameConfiguration,
            Is.EqualTo(new GameConfiguration("S107SC0000", new ScenarioConfiguration([new ScenarioPlayerConfiguration(WorldRace.Human, LordCategory.Warrior, new WorldObjectVariant(0), new UnitDefinitionId("G000UU0019"), LeaderName: null)], WorldDifficulty.Average, Leader: null))));

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
            Is.EqualTo(new GameConfiguration("S107SC0000", new ScenarioConfiguration([new ScenarioPlayerConfiguration(WorldRace.Human, LordCategory.Mage, new WorldObjectVariant(3), new UnitDefinitionId("G000UU0021"), LeaderName: null)], WorldDifficulty.Hard, Leader: null))));

        viewModel.SelectedLordCategory = LordCategory.Guildmaster;
        viewModel.SelectedLeader = viewModel.Leaders[1];
        viewModel.EditLeaderNameCommand.Execute(null);

        TextPromptViewModel prompt = Assert.IsOfType<TextPromptViewModel>(gameViewModel.TopScreen);

        Assert.That(prompt.Title, Is.EqualTo("Hero name"));
        Assert.That(prompt.Message, Is.EqualTo("Enter a name for your starting hero."));
        Assert.That(prompt.Text, Is.EqualTo(""));
        Assert.That(prompt.Placeholder, Is.EqualTo("Default name"));
        Assert.That(prompt.MaxLength, Is.EqualTo(20));

        prompt.Text = "  Aldric ";
        prompt.ConfirmCommand.Execute(null);
        viewModel.NextLordFaceCommand.Execute(null);
        viewModel.NextLordFaceCommand.Execute(null);

        Assert.That(
            viewModel.SelectedLordDescription,
            Is.EqualTo("The Guildmaster's spell research is limited to level four, but thieves can perform more actions and cities cost 50% less to upgrade."));
        Assert.That(viewModel.LeaderName, Is.EqualTo("Aldric"));
        Assert.That(prompt.IsOpen, Is.False);
        Assert.That(viewModel.PortraitKey, Is.EqualTo(new AssetSpriteKey.LordFace(WorldRace.Human, 1, IsLarge: true)));
        Assert.That(
            viewModel.GameConfiguration,
            Is.EqualTo(new GameConfiguration("S107SC0000", new ScenarioConfiguration([new ScenarioPlayerConfiguration(WorldRace.Human, LordCategory.Guildmaster, new WorldObjectVariant(1), new UnitDefinitionId("G000UU0020"), new LiteralUnitName("Aldric"))], WorldDifficulty.Hard, Leader: null))));
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
            Is.EqualTo(new GameConfiguration("S107SC0000", new ScenarioConfiguration([new ScenarioPlayerConfiguration(WorldRace.Human, LordCategory.Warrior, new WorldObjectVariant(0), new UnitDefinitionId("G000UU0019"), LeaderName: null)], WorldDifficulty.Average, Leader: null))));

        scenarioConfiguration.SelectedLordCategory = LordCategory.Mage;
        scenarioConfiguration.SelectedLeader = scenarioConfiguration.Leaders[1];
        scenarioConfiguration.SelectedDifficulty = WorldDifficulty.VeryHard;
        scenarioConfiguration.LeaderName = "Aldric";
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
        Assert.That(leader, Is.EqualTo(new WorldUnit(new WorldUnitId("S107UN0001"), new UnitDefinitionId("G000UU0020"), Hitpoints: 90, Experience: 0, Level: 1, [UnitModifier.FromFortification(new UnitModifierDefinitionId("g000um6001"))], Name: new LiteralUnitName("Aldric"), IsTypeLocked: false)));
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

    private static IReadOnlyList<string> FormatImportLeaders(IReadOnlyList<LeaderImportLeaderViewModel> leaders)
    {
        List<string> rows = [];

        foreach (LeaderImportLeaderViewModel leader in leaders)
        {
            string availability = leader.IsAvailable ? "available" : "unavailable";

            rows.Add($"{leader.Entry.Id} {leader.Name} {leader.LevelText} | {leader.BattlesWonText} | items={leader.ItemIcons.Count} | {availability}");
        }

        return rows;
    }

    [Test]
    public async Task ImportLeaderTest()
    {
        using Sandbox sb = Sandbox.Create();

        LeaderStore store = new(sb.GetPath("leaders"));

        await store.SaveAsync(Elena, new DateTimeOffset(2001, 2, 3, 4, 5, 6, TimeSpan.Zero), null);
        await store.SaveAsync(Dwarf, new DateTimeOffset(2001, 2, 3, 4, 5, 6, TimeSpan.Zero), null);
        await store.SaveAsync(Uther, new DateTimeOffset(2001, 2, 3, 4, 5, 6, TimeSpan.Zero), null);

        GameScenario scenario = new("S202SC0000", "Quest", null, null, 1, 48, 5, null, IsSkirmish: true, []);
        (_, IGameTextProvider texts, IGameDefinitionProvider definitions) = new GameLoader(new RandomProvider(0)).LoadCampaigns(Language.English);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().WithLeaderStore(store).Build();
        ScenarioConfigurationScreenViewModel viewModel = new(
            gameViewModel,
            texts,
            definitions,
            scenario,
            [new ScenarioRace(WorldRace.Human, new GlobalTextId("X000TG0000"))]);

        gameViewModel.ShowScreen(viewModel);

        Assert.That(viewModel.CanImportLeader, Is.True);

        await viewModel.ImportLeaderCommand.ExecuteAsync(null);

        LeaderImportScreenViewModel picker = Assert.IsOfType<LeaderImportScreenViewModel>(gameViewModel.TopScreen);

        Assert.That(FormatImportLeaders(picker.Leaders), Is.EqualTo([
            "3 Uther Lv. 12 | 34 battles won | items=1 | available",
            "1 Elena Lv. 3 | 1 battle won | items=0 | available",
            "2 Thorin Lv. 20 | 90 battles won | items=0 | unavailable"
        ]));
        Assert.That(picker.SelectedLeader, Is.EqualTo(picker.Leaders[0]));
        Assert.That(picker.IsEmpty, Is.False);
        Assert.That(picker.ImportCommand.CanExecute(null), Is.True);

        picker.SelectedLeader = picker.Leaders[2];

        Assert.That(picker.ImportCommand.CanExecute(null), Is.False);
        Assert.That(picker.DeleteCommand.CanExecute(null), Is.True);

        picker.SelectedLeader = picker.Leaders[0];
        picker.ImportCommand.Execute(null);

        Assert.That(gameViewModel.TopScreen, Is.SameAs(viewModel));
        Assert.That(FormatLeaders(viewModel.Leaders), Is.EqualTo([
            "g000uu0019 Fighter Pegasus Knight: The Pegasus Knight is a noble fighter whose winged steed carries him high above forests and sea.",
            "g000uu0020 Explorer Ranger: Traveling quickly around the realm, the Ranger is often sent on scouting missions by the King.",
            "g000uu0021 Mage Archmage: Master of the arcane, an Archmage is the only Empire leader who can use staffs and scrolls.",
            "G000UU0019 Fighter Uther · Level 12: The Pegasus Knight is a noble fighter whose winged steed carries him high above forests and sea."
        ]));
        Assert.That(viewModel.SelectedLeader, Is.EqualTo(viewModel.Leaders[3]));
        Assert.That(viewModel.GameConfiguration, Is.EqualTo(new GameConfiguration("S202SC0000", new ScenarioConfiguration(
            [new ScenarioPlayerConfiguration(WorldRace.Human, LordCategory.Warrior, new WorldObjectVariant(0), null, LeaderName: null)],
            WorldDifficulty.Average,
            Uther))));

        await viewModel.ImportLeaderCommand.ExecuteAsync(null);

        picker = Assert.IsOfType<LeaderImportScreenViewModel>(gameViewModel.TopScreen);
        picker.SelectedLeader = picker.Leaders[1];
        picker.ImportCommand.Execute(null);

        Assert.That(FormatLeaders(viewModel.Leaders), Is.EqualTo([
            "g000uu0019 Fighter Pegasus Knight: The Pegasus Knight is a noble fighter whose winged steed carries him high above forests and sea.",
            "g000uu0020 Explorer Ranger: Traveling quickly around the realm, the Ranger is often sent on scouting missions by the King.",
            "g000uu0021 Mage Archmage: Master of the arcane, an Archmage is the only Empire leader who can use staffs and scrolls.",
            "G000UU0019 Fighter Uther · Level 12: The Pegasus Knight is a noble fighter whose winged steed carries him high above forests and sea.",
            "G000UU0021 Mage Elena · Level 3: Master of the arcane, an Archmage is the only Empire leader who can use staffs and scrolls."
        ]));
        Assert.That(viewModel.SelectedLeader, Is.EqualTo(viewModel.Leaders[4]));

        await viewModel.ImportLeaderCommand.ExecuteAsync(null);

        picker = Assert.IsOfType<LeaderImportScreenViewModel>(gameViewModel.TopScreen);
        picker.ImportCommand.Execute(null);

        Assert.That(viewModel.Leaders.Count, Is.EqualTo(5));
        Assert.That(viewModel.SelectedLeader, Is.EqualTo(viewModel.Leaders[3]));

        viewModel.SelectedLeader = viewModel.Leaders[1];

        Assert.That(viewModel.GameConfiguration, Is.EqualTo(new GameConfiguration("S202SC0000", new ScenarioConfiguration(
            [new ScenarioPlayerConfiguration(WorldRace.Human, LordCategory.Warrior, new WorldObjectVariant(0), new UnitDefinitionId("g000uu0020"), LeaderName: null)],
            WorldDifficulty.Average,
            Leader: null))));
    }

    [Test]
    public async Task ImportLeaderOrderTest()
    {
        using Sandbox sb = Sandbox.Create();

        LeaderStore store = new(sb.GetPath("leaders"));

        await store.SaveAsync(Uther with { Unit = Uther.Unit with { Name = new LiteralUnitName("Novice") }, BattlesWon = 5 }, new DateTimeOffset(2001, 2, 3, 4, 5, 6, TimeSpan.Zero), null);
        await store.SaveAsync(Uther with { Unit = Uther.Unit with { Name = new LiteralUnitName("Veteran") }, BattlesWon = 60 }, new DateTimeOffset(2001, 2, 3, 4, 5, 6, TimeSpan.Zero), null);
        await store.SaveAsync(Uther, new DateTimeOffset(2001, 2, 3, 4, 5, 6, TimeSpan.Zero), null);

        GameScenario scenario = new("S202SC0000", "Quest", null, null, 1, 48, 5, null, IsSkirmish: true, []);
        (_, IGameTextProvider texts, IGameDefinitionProvider definitions) = new GameLoader(new RandomProvider(0)).LoadCampaigns(Language.English);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().WithLeaderStore(store).Build();
        ScenarioConfigurationScreenViewModel viewModel = new(
            gameViewModel,
            texts,
            definitions,
            scenario,
            [new ScenarioRace(WorldRace.Human, new GlobalTextId("X000TG0000"))]);

        gameViewModel.ShowScreen(viewModel);

        await viewModel.ImportLeaderCommand.ExecuteAsync(null);

        LeaderImportScreenViewModel picker = Assert.IsOfType<LeaderImportScreenViewModel>(gameViewModel.TopScreen);

        Assert.That(FormatImportLeaders(picker.Leaders), Is.EqualTo([
            "2 Veteran Lv. 12 | 60 battles won | items=1 | available",
            "3 Uther Lv. 12 | 34 battles won | items=1 | available",
            "1 Novice Lv. 12 | 5 battles won | items=1 | available"
        ]));
    }

    [Test]
    public void HotseatImportLeaderTest()
    {
        GameScenario scenario = new("S202SC0000", "Quest", null, null, 1, 48, 5, null, IsSkirmish: true, []);
        (_, IGameTextProvider texts, IGameDefinitionProvider definitions) = new GameLoader(new RandomProvider(0)).LoadCampaigns(Language.English);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ScenarioConfigurationScreenViewModel viewModel = new(
            gameViewModel,
            texts,
            definitions,
            scenario,
            [
                new ScenarioRace(WorldRace.Human, new GlobalTextId("X000TG0000")),
                new ScenarioRace(WorldRace.Dwarf, new GlobalTextId("X000TG0001"))
            ]);

        Assert.That(viewModel.CanImportLeader, Is.False);
        Assert.That(viewModel.ImportLeaderCommand.CanExecute(null), Is.False);
    }

    [Test]
    public void SagaImportLeaderTest()
    {
        GameScenario scenario = new("S107SC0000", "Saga", null, null, 1, 48, 5, null, IsSkirmish: false, []);
        (_, IGameTextProvider texts, IGameDefinitionProvider definitions) = new GameLoader(new RandomProvider(0)).LoadCampaigns(Language.English);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ScenarioConfigurationScreenViewModel viewModel = new(
            gameViewModel,
            texts,
            definitions,
            scenario,
            [new ScenarioRace(WorldRace.Human, new GlobalTextId("X000TG0000"))]);

        Assert.That(viewModel.CanImportLeader, Is.False);
        Assert.That(viewModel.ImportLeaderCommand.CanExecute(null), Is.False);
    }

    [Test]
    public async Task DeleteLeaderTest()
    {
        using Sandbox sb = Sandbox.Create();

        LeaderStore store = new(sb.GetPath("leaders"));

        await store.SaveAsync(Uther, new DateTimeOffset(2001, 2, 3, 4, 5, 6, TimeSpan.Zero), null);

        GameScenario scenario = new("S202SC0000", "Quest", null, null, 1, 48, 5, null, IsSkirmish: true, []);
        (_, IGameTextProvider texts, IGameDefinitionProvider definitions) = new GameLoader(new RandomProvider(0)).LoadCampaigns(Language.English);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().WithLeaderStore(store).Build();
        ScenarioConfigurationScreenViewModel viewModel = new(
            gameViewModel,
            texts,
            definitions,
            scenario,
            [new ScenarioRace(WorldRace.Human, new GlobalTextId("X000TG0000"))]);

        gameViewModel.ShowScreen(viewModel);

        await viewModel.ImportLeaderCommand.ExecuteAsync(null);

        LeaderImportScreenViewModel picker = Assert.IsOfType<LeaderImportScreenViewModel>(gameViewModel.TopScreen);

        Assert.That(picker.DeleteCommand.CanExecute(null), Is.True);

        picker.DeleteCommand.Execute(null);

        ConfirmPromptViewModel prompt = Assert.IsOfType<ConfirmPromptViewModel>(gameViewModel.TopScreen);
        Assert.That(prompt.Message, Is.EqualTo(TextSpans.FromText("Delete leader \"Uther\" permanently?")));

        await prompt.ConfirmCommand.ExecuteAsync(null);

        Assert.That(gameViewModel.TopScreen, Is.SameAs(picker));
        Assert.That(await store.EnumerateAsync(), Is.Empty);
        Assert.That(FormatImportLeaders(picker.Leaders), Is.Empty);
        Assert.That(picker.IsEmpty, Is.True);
        Assert.That(picker.SelectedLeader, Is.Null);
        Assert.That(picker.ImportCommand.CanExecute(null), Is.False);
        Assert.That(picker.DeleteCommand.CanExecute(null), Is.False);
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
