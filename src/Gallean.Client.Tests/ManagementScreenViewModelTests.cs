using Gallean.Common;
using Gallean.Engine;
using Gallean.Engine.Tests;

namespace Gallean.Client.Tests;

public sealed class ManagementScreenViewModelTests
{
    private sealed class TestAudioPlayer : IAudioPlayer
    {
        public List<string> Actions { get; } = [];
        public float SoundVolume { get; set; }
        public float MusicVolume { get; set; }

        public void PlaySound(AssetSoundKey key)
        {
            Actions.Add($"Sound:{key.CanonicalId}");
        }

        public IMusicPlayback? PlayMusic(MusicQueue queue, MusicPosition position)
        {
            Actions.Add($"Music:{string.Join(',', queue.Tracks.Select(track => track.CanonicalId))}");

            return new RecordingPlayback(string.Join(',', queue.Tracks.Select(track => track.CanonicalId)), Actions);
        }

        public IAudioPlayback? PlayVoice(AssetSoundKey key)
        {
            Actions.Add($"Voice:{key.CanonicalId}");

            return new RecordingPlayback(key.CanonicalId, Actions);
        }

        public IAmbientAudioPlayback? PlayAmbientSound(AssetSoundKey key, float volume, float pan)
        {
            return null;
        }
    }

    private sealed class RecordingPlayback(string name, List<string> actions) : IMusicPlayback
    {
        public MusicPosition Position => MusicPosition.Start;

        public void Stop()
        {
            actions.Add($"Stop:{name}");
        }
    }

    private static IReadOnlyList<string> FormatTabs(IReadOnlyList<ManagementTabViewModel> tabs)
    {
        List<string> rows = [];

        foreach (ManagementTabViewModel tab in tabs)
        {
            rows.Add($"{tab.TabHeader} | {FormatSelection(tab.IsSelected)}");
        }

        return rows;
    }

    private static string FormatSelection(bool isSelected)
    {
        if (isSelected)
        {
            return "selected";
        }
        else
        {
            return "not selected";
        }
    }

    private static IReadOnlyList<string> FormatResources(ResourceSetViewModel resources)
    {
        List<string> rows = [];

        foreach (ResourceSetItemViewModel item in resources.Items)
        {
            rows.Add($"{item.Type} | {item.Value}");
        }

        return rows;
    }

    [Test]
    public void CapitalTabsTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 3);

        WorldRaceDefinition raceDefinition = new(
            Race: WorldRace.Human,
            NameTextId: new GlobalTextId("Human"),
            Income: WorldResourceSet.Empty,
            NativeTerrain: WorldMapTerrain.Human,
            IsPlayable: true,
            CapitalRegeneration: 35,
            CapitalProtectionModifierId: null,
            Cities: [],
            Leaders: [leaderDefinition.Id],
            Units: [],
            MaleLeaderNames: [],
            FemaleLeaderNames: []);

        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("cap"),
            player.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithRace(raceDefinition)
            .WithCapital(capital)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel screen = ManagementScreenViewModel.CreateForFortification(gameModel, gameViewModel, capital.Id);

        Assert.That(FormatTabs(screen.Tabs), Is.EqualTo([
            "City | selected",
            "Party | not selected",
            "Buildings | not selected",
            "Spellbook | not selected",
            "Diplomacy | not selected"
        ]));
        Assert.That(screen.SelectedTab, Is.SameAs(screen.Tabs[0]));
    }

    [Test]
    public void CityTabsTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(
                new WorldCityDefinitionId(WorldRace.Human, 3),
                WorldResourceSet.Empty) with
            {
                VisionRadius = 5
            };

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("Milonia"),
            cityDefinition,
            player.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(cityDefinition)
            .WithCity(city)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel screen = ManagementScreenViewModel.CreateForFortification(gameModel, gameViewModel, city.Id);

        Assert.That(FormatTabs(screen.Tabs), Is.EqualTo([
            "City | selected",
            "Party | not selected"
        ]));
    }

    [Test]
    public void StackTabsTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 3);

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 100, 0, leaderDefinition.Level, [], null, false);
        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .Build();

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Virgile"),
            player.Id,
            new WorldPosition(4, 4),
            TestHelpers.StackUnitGrid(stackDefinitions, [(leader, GridPosition.FrontBottom)]),
            movementPoints: 20);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel screen = ManagementScreenViewModel.CreateForStack(gameModel, gameViewModel, stack.Id);

        Assert.That(FormatTabs(screen.Tabs), Is.EqualTo(["Party | selected"]));
    }

    [Test]
    public void AdjacentStackTabsTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        WorldPlayer otherPlayer = player with
        {
            Id = new WorldPlayerId(2)
        };

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 3);

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 100, 0, leaderDefinition.Level, [], null, false);
        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .Build();

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Virgile"),
            player.Id,
            new WorldPosition(4, 4),
            TestHelpers.StackUnitGrid(stackDefinitions, [(leader, GridPosition.FrontBottom)]),
            movementPoints: 20);

        WorldStack adjacentStack = stack with
        {
            Id = new WorldStackId("Hugin"),
            Position = new WorldPosition(5, 4),
            Units = TestHelpers.StackUnitGrid(stackDefinitions, [(leader with { Id = new WorldUnitId("huginLeader") }, GridPosition.FrontBottom)])
        };

        WorldStack adjacentEnemyStack = stack with
        {
            Id = new WorldStackId("Munin"),
            OwnerId = otherPlayer.Id,
            Position = new WorldPosition(3, 4),
            Units = TestHelpers.StackUnitGrid(stackDefinitions, [(leader with { Id = new WorldUnitId("muninLeader") }, GridPosition.FrontBottom)])
        };

        WorldStack farStack = stack with
        {
            Id = new WorldStackId("Ratatosk"),
            Position = new WorldPosition(7, 7),
            Units = TestHelpers.StackUnitGrid(stackDefinitions, [(leader with { Id = new WorldUnitId("ratatoskLeader") }, GridPosition.FrontBottom)])
        };

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(otherPlayer.LordId, otherPlayer.Race, LordCategory.Warrior))
            .WithPlayer(otherPlayer)
            .WithDefinition(leaderDefinition)
            .WithStack(stack)
            .WithStack(adjacentStack)
            .WithStack(adjacentEnemyStack)
            .WithStack(farStack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel screen = ManagementScreenViewModel.CreateForStack(gameModel, gameViewModel, stack.Id);

        Assert.That(FormatTabs(screen.Tabs), Is.EqualTo([
            "Party | selected",
            "Exchange | not selected"
        ]));
    }

    [Test]
    public void TrainerTabsTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 3);

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 100, 0, leaderDefinition.Level, [], null, false);
        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .Build();

        WorldTrainerSite site = new(
            Id: new WorldSiteId("trainer"),
            Footprint: new WorldFootprint(new WorldPosition(2, 2), 3, 3),
            Variant: new WorldObjectVariant(0),
            TitleTextId: new ScenarioTextId("Title"),
            GreetingTextId: new ScenarioTextId("Greeting"),
            VisitorIds: [],
            AiPriority: 3);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Virgile"),
            player.Id,
            site.Entrance,
            TestHelpers.StackUnitGrid(stackDefinitions, [(leader, GridPosition.FrontBottom)]),
            movementPoints: 20);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithSite(site)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel screen = ManagementScreenViewModel.CreateForSite(gameModel, gameViewModel, stack.Id);

        Assert.That(FormatTabs(screen.Tabs), Is.EqualTo([
            "Trainer | selected",
            "Party | not selected"
        ]));
    }

    [Test]
    public void TrainerPartyTabsTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 3);

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 100, 0, leaderDefinition.Level, [], null, false);
        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .Build();

        WorldTrainerSite site = new(
            Id: new WorldSiteId("trainer"),
            Footprint: new WorldFootprint(new WorldPosition(2, 2), 3, 3),
            Variant: new WorldObjectVariant(0),
            TitleTextId: new ScenarioTextId("Title"),
            GreetingTextId: new ScenarioTextId("Greeting"),
            VisitorIds: [],
            AiPriority: 3);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Virgile"),
            player.Id,
            site.Entrance,
            TestHelpers.StackUnitGrid(stackDefinitions, [(leader, GridPosition.FrontBottom)]),
            movementPoints: 20);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithSite(site)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel screen = ManagementScreenViewModel.CreateForStack(gameModel, gameViewModel, stack.Id);

        Assert.That(FormatTabs(screen.Tabs), Is.EqualTo([
            "Trainer | not selected",
            "Party | selected"
        ]));
    }

    [Test]
    public void SpellbookTabsTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel screen = ManagementScreenViewModel.CreateForSpellbook(gameModel, gameViewModel);

        Assert.That(FormatTabs(screen.Tabs), Is.EqualTo(["Spellbook | selected"]));
    }

    [Test]
    public void SelectTabTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(
                new WorldCityDefinitionId(WorldRace.Human, 3),
                WorldResourceSet.Empty) with
            {
                VisionRadius = 5
            };

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("Milonia"),
            cityDefinition,
            player.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(cityDefinition)
            .WithCity(city)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel screen = ManagementScreenViewModel.CreateForFortification(gameModel, gameViewModel, city.Id);

        screen.SelectTabCommand.Execute(screen.Tabs[1]);

        Assert.That(FormatTabs(screen.Tabs), Is.EqualTo([
            "City | not selected",
            "Party | selected"
        ]));
        Assert.That(screen.SelectedTab, Is.SameAs(screen.Tabs[1]));
    }

    [Test]
    public void SpellbookSoundTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        TestAudioPlayer audioPlayer = new();
        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().WithAudioPlayer(audioPlayer).Build();
        ManagementScreenViewModel screen = ManagementScreenViewModel.CreateForSpellbook(gameModel, gameViewModel);

        gameViewModel.OpenScreen(screen);
        screen.CloseCommand.Execute(null);

        Assert.That(audioPlayer.Actions, Is.EqualTo([
            "Sound:book-open",
            "Sound:book-close"
        ]));
    }

    [Test]
    public void SpellbookResourcesTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map) with
        {
            Resources = new WorldResourceSet(250, 0, 0, 30, 0, 5)
        };

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel screen = ManagementScreenViewModel.CreateForSpellbook(gameModel, gameViewModel);

        Assert.That(FormatResources(screen.PlayerResources), Is.EqualTo([
            "LifeMana | 55",
            "GroveMana | 5"
        ]));
    }

    [Test]
    public void FortificationResourcesTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map) with
        {
            Resources = new WorldResourceSet(250, 0, 0, 30, 0, 5)
        };

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(
                new WorldCityDefinitionId(WorldRace.Human, 3),
                WorldResourceSet.Empty) with
            {
                VisionRadius = 5
            };

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("Milonia"),
            cityDefinition,
            player.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(cityDefinition)
            .WithCity(city)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel screen = ManagementScreenViewModel.CreateForFortification(gameModel, gameViewModel, city.Id);

        Assert.That(FormatResources(screen.PlayerResources), Is.EqualTo(["Gold | 350"]));
    }

    [Test]
    public void CapitalOpenSoundTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 3);

        WorldRaceDefinition raceDefinition = new(
            Race: WorldRace.Human,
            NameTextId: new GlobalTextId("Human"),
            Income: WorldResourceSet.Empty,
            NativeTerrain: WorldMapTerrain.Human,
            IsPlayable: true,
            CapitalRegeneration: 35,
            CapitalProtectionModifierId: null,
            Cities: [],
            Leaders: [leaderDefinition.Id],
            Units: [],
            MaleLeaderNames: [],
            FemaleLeaderNames: []);

        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("cap"),
            player.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithRace(raceDefinition)
            .WithCapital(capital)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        TestAudioPlayer audioPlayer = new();
        GameViewModel gameViewModel = new TestGameViewModelBuilder().WithAudioPlayer(audioPlayer).Build();
        ManagementScreenViewModel screen = ManagementScreenViewModel.CreateForFortification(gameModel, gameViewModel, capital.Id);

        gameViewModel.OpenScreen(screen);

        Assert.That(audioPlayer.Actions, Is.EqualTo([
            "Sound:interface-open",
            "Music:capital-music/human/0,capital-music/human/1,capital-music/human/2"
        ]));
    }

    [Test]
    public void CityOpenSoundTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(
                new WorldCityDefinitionId(WorldRace.Human, 3),
                WorldResourceSet.Empty) with
            {
                VisionRadius = 5
            };

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("Milonia"),
            cityDefinition,
            player.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(cityDefinition)
            .WithCity(city)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        TestAudioPlayer audioPlayer = new();
        GameViewModel gameViewModel = new TestGameViewModelBuilder().WithAudioPlayer(audioPlayer).Build();
        ManagementScreenViewModel screen = ManagementScreenViewModel.CreateForFortification(gameModel, gameViewModel, city.Id);

        gameViewModel.OpenScreen(screen);

        Assert.That(audioPlayer.Actions, Is.Empty);
    }

    [Test]
    public void CapitalSpellbookResourcesTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map) with
        {
            Resources = new WorldResourceSet(250, 0, 0, 30, 0, 5)
        };

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 3);

        WorldRaceDefinition raceDefinition = new(
            Race: WorldRace.Human,
            NameTextId: new GlobalTextId("Human"),
            Income: WorldResourceSet.Empty,
            NativeTerrain: WorldMapTerrain.Human,
            IsPlayable: true,
            CapitalRegeneration: 35,
            CapitalProtectionModifierId: null,
            Cities: [],
            Leaders: [leaderDefinition.Id],
            Units: [],
            MaleLeaderNames: [],
            FemaleLeaderNames: []);

        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("cap"),
            player.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithRace(raceDefinition)
            .WithCapital(capital)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel screen = ManagementScreenViewModel.CreateForFortification(gameModel, gameViewModel, capital.Id);

        screen.SelectTabCommand.Execute(screen.Tabs[3]);

        Assert.That(FormatResources(screen.PlayerResources), Is.EqualTo([
            "LifeMana | 30",
            "GroveMana | 5"
        ]));

        screen.SelectTabCommand.Execute(screen.Tabs[4]);

        Assert.That(FormatResources(screen.PlayerResources), Is.EqualTo(["Gold | 250"]));
    }

    [Test]
    public void CapitalSpellbookSoundTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 3);

        WorldRaceDefinition raceDefinition = new(
            Race: WorldRace.Human,
            NameTextId: new GlobalTextId("Human"),
            Income: WorldResourceSet.Empty,
            NativeTerrain: WorldMapTerrain.Human,
            IsPlayable: true,
            CapitalRegeneration: 35,
            CapitalProtectionModifierId: null,
            Cities: [],
            Leaders: [leaderDefinition.Id],
            Units: [],
            MaleLeaderNames: [],
            FemaleLeaderNames: []);

        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("cap"),
            player.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithRace(raceDefinition)
            .WithCapital(capital)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        TestAudioPlayer audioPlayer = new();
        GameViewModel gameViewModel = new TestGameViewModelBuilder().WithAudioPlayer(audioPlayer).Build();
        ManagementScreenViewModel screen = ManagementScreenViewModel.CreateForFortification(gameModel, gameViewModel, capital.Id);

        gameViewModel.OpenScreen(screen);
        screen.SelectTabCommand.Execute(screen.Tabs[3]);
        screen.SelectTabCommand.Execute(screen.Tabs[4]);

        Assert.That(audioPlayer.Actions, Is.EqualTo([
            "Sound:interface-open",
            "Music:capital-music/human/0,capital-music/human/1,capital-music/human/2",
            "Sound:book-open",
            "Sound:book-close"
        ]));
    }
}
