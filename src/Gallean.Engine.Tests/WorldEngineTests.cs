using System.Collections.Immutable;

namespace Gallean.Engine.Tests;

public sealed class WorldEngineTests
{
    [Test]
    public void GoldenTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        UnitDefinition unitDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("dummy", maxHitpoints: 10, initiative: 50, damage: 5),
            movementPoints: 6, leadership: 6);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            new WorldPlayerId(1),
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("unit"), unitDefinition),
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(unitDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(),
        [
            new WorldTurnStartingEvent(new WorldPlayerId(1)),
            new WorldStackMovementRestoredEvent(new WorldStackId("Stack1"), 6),
            new WorldTurnStartedEvent(new WorldPlayerId(1)),
            new WorldTurnResourcesGeneratedEvent(new WorldPlayerId(1), new WorldResourceSet(100, 25, 0, 0, 0, 0))
        ]);

        engine.CheckCommandError(
            new WorldMoveStackCommand(new WorldStackId("InvalidStack"), new WorldPosition(0, 1)),
            WorldCommandError.StackNotFound
        );

        engine.CheckCommand(
            new WorldMoveStackCommand(new WorldStackId("Stack1"), new WorldPosition(0, 1)),
            [
                new WorldStackMovingEvent(new WorldStackId("Stack1"), new WorldPosition(0, 0), new WorldPosition(0, 1), WorldStackDirection.SouthWest, 4, [], null),
                new WorldStackMovedEvent(new WorldStackId("Stack1"), new WorldPosition(0, 0), new WorldPosition(0, 1), WorldStackDirection.SouthWest, 4, [], null)
            ]
        );

        Assert.That(engine.State, Is.EqualTo(new WorldState(
            Map: TestHelpers.SimpleMap(3, 3),
            TurnInfo: new WorldTurnInfo(1, new WorldPlayerId(1)),
            Difficulty: WorldDifficulty.Average,
            Players: engine.State.Players.SetItem(player.Id, player with
            {
                Resources = new WorldResourceSet(100, 25, 0, 0, 0, 0)
            }),
            Relations: ImmutableSortedDictionary<WorldRelationId, WorldRelation>.Empty,
            Stacks: engine.State.Stacks.SetItem(stack.Id, stack with
            {
                Position = new WorldPosition(0, 1),
                Direction = WorldStackDirection.SouthWest,
                MovementPoints = 4
            }),
            Fortifications: ImmutableSortedDictionary<WorldFortificationId, WorldFortification>.Empty,
            ResourceNodes: ImmutableSortedDictionary<WorldResourceNodeId, WorldResourceNode>.Empty,
            Rods: ImmutableSortedDictionary<WorldRodId, WorldRod>.Empty,
            Bags: ImmutableSortedDictionary<WorldBagId, WorldBag>.Empty,
            Tombs: ImmutableSortedDictionary<WorldTombId, WorldTomb>.Empty,
            Landmarks: ImmutableSortedDictionary<WorldLandmarkId, WorldLandmark>.Empty,
            Mountains: ImmutableSortedDictionary<WorldMountainId, WorldMountain>.Empty,
            Sites: ImmutableSortedDictionary<WorldSiteId, WorldSite>.Empty,
            Subraces: ImmutableSortedDictionary<WorldSubraceId, WorldSubrace>.Empty,
            ScenarioState: WorldScenarioState.Empty,
            AiMemories: ImmutableSortedDictionary<WorldPlayerId, WorldAiMemory>.Empty)
        ));
    }

    [Test]
    public void DryRunTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        UnitDefinition unitDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("dummy", maxHitpoints: 10, initiative: 50, damage: 5),
            movementPoints: 6, leadership: 6);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            new WorldPlayerId(1),
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("unit"), unitDefinition),
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(unitDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(),
        [
            new WorldTurnStartingEvent(new WorldPlayerId(1)),
            new WorldStackMovementRestoredEvent(new WorldStackId("Stack1"), 6),
            new WorldTurnStartedEvent(new WorldPlayerId(1)),
            new WorldTurnResourcesGeneratedEvent(new WorldPlayerId(1), new WorldResourceSet(100, 25, 0, 0, 0, 0))
        ]);

        WorldState previousState = engine.State;

        engine.CheckCommand(
            new WorldMoveStackCommand(new WorldStackId("Stack1"), new WorldPosition(0, 1)),
            [
                new WorldStackMovingEvent(new WorldStackId("Stack1"), new WorldPosition(0, 0), new WorldPosition(0, 1), WorldStackDirection.SouthWest, 4, [], null),
                new WorldStackMovedEvent(new WorldStackId("Stack1"), new WorldPosition(0, 0), new WorldPosition(0, 1), WorldStackDirection.SouthWest, 4, [], null)
            ],
            dryRun: true
        );

        Assert.That(engine.State, Is.EqualTo(previousState));
    }

    [Test]
    public void CaptureNodeWithExpansionTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^^",
            "^^^^^^^",
            "^^.....",
            "^^.U...",
            "^^.....",
            "^^.....",
            "^^....."
        ]);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);
        WorldPlayer player3 = TestHelpers.SimplePlayer(new WorldPlayerId(3), WorldRace.Neutral, map);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 10, leadership: 6);
        WorldStack stack1 = TestHelpers.SimpleStack(
            new WorldStackId("stack1"),
            player1.Id,
            new WorldPosition(2, 2),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 10);

        WorldStack stack2 = TestHelpers.SimpleStack(
            new WorldStackId("stack2"),
            player2.Id,
            new WorldPosition(6, 6),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader2"), leader),
            movementPoints: 10);

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty);
        WorldCity city1 = TestHelpers.SimpleCity(
            new WorldFortificationId("city1"),
            cityDefinition,
            player1.Id,
            new WorldPosition(3, 3),
            UnitGrid<WorldUnit>.Empty);

        WorldResourceNode node1 = new(
            Id: new WorldResourceNodeId("node1"),
            Position: new WorldPosition(5, 3),
            ResourceType: WorldResourceType.Gold,
            AiPriority: 3
        );

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithGlobals(new GlobalVariables { CityTerrainTilesPerTurnLevel1 = 4 })
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithDefinition(TestHelpers.SimpleLord(player3.LordId, player3.Race, LordCategory.Warrior))
            .WithPlayer(player3)
            .WithDefinition(cityDefinition)
            .WithStack(stack1)
            .WithStack(stack2)
            .WithCity(city1)
            .WithResourceNode(node1)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player1.Id),
            new WorldStackMovementRestoredEvent(stack1.Id, 10),
            new WorldTurnStartedEvent(player1.Id),
            new WorldTurnResourcesGeneratedEvent(player1.Id, new WorldResourceSet(100, 25, 0, 0, 0, 0))
        ]);

        engine.CheckCommand(new WorldEndTurnCommand(), [
            new WorldTurnEndedEvent(player1.Id),
            new WorldTurnStartingEvent(player2.Id),
            new WorldStackMovementRestoredEvent(stack2.Id, 10),
            new WorldTurnStartedEvent(player2.Id),
            new WorldTurnResourcesGeneratedEvent(player2.Id, new WorldResourceSet(100, 0, 0, 0, 25, 0))
        ]);

        engine.CheckCommand(new WorldEndTurnCommand(), [
            new WorldTurnEndedEvent(player2.Id),
            new WorldTurnStartingEvent(player3.Id),
            new WorldTurnStartedEvent(player3.Id),
            new WorldTurnResourcesGeneratedEvent(player3.Id, WorldResourceSet.Empty)
        ]);

        engine.CheckCommand(new WorldEndTurnCommand(), [
            new WorldTurnEndedEvent(player3.Id),
            new WorldRoundEndedEvent(),
            new WorldRoundStartedEvent(),
            new WorldTurnStartingEvent(player1.Id),
            new WorldStackMovementRestoredEvent(stack1.Id, 10),
            new WorldTurnStartedEvent(player1.Id),
            new WorldTurnResourcesGeneratedEvent(player1.Id, new WorldResourceSet(150, 25, 0, 0, 0, 0))
        ]);

        Assert.That(TestHelpers.SerializeMap(engine.State.Map), Is.EqualTo([
            "^^^^^^^",
            "^^^^^^^",
            "^^.UUUU",
            "^^UUUUU",
            "^^U.UUU",
            "^^.....",
            "^^....."
        ]));

        engine.CheckCommand(new WorldEndTurnCommand(), [
            new WorldTurnEndedEvent(player1.Id),
            new WorldTurnStartingEvent(player2.Id),
            new WorldStackMovementRestoredEvent(stack2.Id, 10),
            new WorldTurnStartedEvent(player2.Id),
            new WorldTurnResourcesGeneratedEvent(player2.Id, new WorldResourceSet(100, 0, 0, 0, 25, 0))
        ]);

        engine.CheckCommand(new WorldEndTurnCommand(), [
            new WorldTurnEndedEvent(player2.Id),
            new WorldTurnStartingEvent(player3.Id),
            new WorldTurnStartedEvent(player3.Id),
            new WorldTurnResourcesGeneratedEvent(player3.Id, WorldResourceSet.Empty)
        ]);

        engine.CheckCommand(new WorldEndTurnCommand(), [
            new WorldTurnEndedEvent(player3.Id),
            new WorldRoundEndedEvent(),
            new WorldRoundStartedEvent(),
            new WorldTurnStartingEvent(player1.Id),
            new WorldStackMovementRestoredEvent(stack1.Id, 10),
            new WorldTurnStartedEvent(player1.Id),
            new WorldTurnResourcesGeneratedEvent(player1.Id, new WorldResourceSet(150, 25, 0, 0, 0, 0))
        ]);

        Assert.That(TestHelpers.SerializeMap(engine.State.Map), Is.EqualTo([
            "^^^^^^^",
            "^^^^^^^",
            "^^.UUUU",
            "^^UUUUU",
            "^^UUUUU",
            "^^UU...",
            "^^U...."
        ]));

        WorldPlayer expectedPlayer1 = player1 with
        {
            Resources = new WorldResourceSet(400, 75, 0, 0, 0, 0)
        };

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Players = engine.State.Players.SetItem(expectedPlayer1.Id, expectedPlayer1)
            }));
    }

    [Test]
    public void ExpandCityTerritoryTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^",
            "^.....",
            "^.....",
            "^..U..",
            "^.....",
            "^....."
        ]);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty);
        WorldCity city1 = TestHelpers.SimpleCity(
            new WorldFortificationId("City1"),
            cityDefinition,
            player1.Id,
            new WorldPosition(3, 3),
            UnitGrid<WorldUnit>.Empty);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithGlobals(new GlobalVariables { CityTerrainTilesPerTurnLevel1 = 12 })
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithCity(city1)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player1.Id),
            new WorldTurnStartedEvent(player1.Id),
            new WorldTurnResourcesGeneratedEvent(player1.Id, new WorldResourceSet(100, 25, 0, 0, 0, 0))
        ]);

        Assert.That(TestHelpers.SerializeMap(engine.State.Map), Is.EqualTo([
            "^^^^^^",
            "^.....",
            "^.....",
            "^..U..",
            "^.....",
            "^....."
        ]));

        engine.CheckCommand(new WorldEndTurnCommand(), [
            new WorldTurnEndedEvent(player1.Id),
            new WorldRoundEndedEvent(),
            new WorldRoundStartedEvent(),
            new WorldTurnStartingEvent(player1.Id),
            new WorldTurnStartedEvent(player1.Id),
            new WorldTurnResourcesGeneratedEvent(player1.Id, new WorldResourceSet(100, 25, 0, 0, 0, 0))
        ]);

        Assert.That(TestHelpers.SerializeMap(engine.State.Map), Is.EqualTo([
            "^^^^^^",
            "^UUU..",
            "^U.UU.",
            "^UUU..",
            "^UUU..",
            "^.U..."
        ]));

        engine.CheckCommand(new WorldEndTurnCommand(), [
            new WorldTurnEndedEvent(player1.Id),
            new WorldRoundEndedEvent(),
            new WorldRoundStartedEvent(),
            new WorldTurnStartingEvent(player1.Id),
            new WorldTurnStartedEvent(player1.Id),
            new WorldTurnResourcesGeneratedEvent(player1.Id, new WorldResourceSet(100, 25, 0, 0, 0, 0))
        ]);

        Assert.That(TestHelpers.SerializeMap(engine.State.Map), Is.EqualTo([
            "^^^^^^",
            "^UUUUU",
            "^U.UUU",
            "^UUUUU",
            "^UUUUU",
            "^UUUUU"
        ]));
    }

    [Test]
    public void ExpandRodTerritoryTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^",
            "^.....",
            "^.....",
            "^..U..",
            "^.....",
            "^....."
        ]);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldRod rod1 = new(
            Id: new WorldRodId("Rod1"),
            OwnerId: player1.Id,
            Position: new WorldPosition(3, 3)
        );

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithRod(rod1)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player1.Id),
            new WorldTurnStartedEvent(player1.Id),
            new WorldTurnResourcesGeneratedEvent(player1.Id, new WorldResourceSet(100, 25, 0, 0, 0, 0))
        ]);

        engine.CheckCommand(new WorldEndTurnCommand(), [
            new WorldTurnEndedEvent(player1.Id),
            new WorldRoundEndedEvent(),
            new WorldRoundStartedEvent(),
            new WorldTurnStartingEvent(player1.Id),
            new WorldTurnStartedEvent(player1.Id),
            new WorldTurnResourcesGeneratedEvent(player1.Id, new WorldResourceSet(100, 25, 0, 0, 0, 0))
        ]);

        Assert.That(TestHelpers.SerializeMap(engine.State.Map), Is.EqualTo([
            "^^^^^^",
            "^.....",
            "^..U..",
            "^..U..",
            "^..U..",
            "^....."
        ]));
    }

    [Test]
    public void CapturedNodesGenerateIncomeTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 1, 'U');

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer neutralPlayer = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Neutral, map);

        WorldResourceNode goldNode = new(
            Id: new WorldResourceNodeId("gold"),
            Position: new WorldPosition(0, 0),
            ResourceType: WorldResourceType.Gold,
            AiPriority: 3
        );

        WorldResourceNode manaNode = new(
            Id: new WorldResourceNodeId("mana"),
            Position: new WorldPosition(1, 0),
            ResourceType: WorldResourceType.DeathMana,
            AiPriority: 3
        );

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(neutralPlayer.LordId, neutralPlayer.Race, LordCategory.Warrior))
            .WithPlayer(neutralPlayer)
            .WithResourceNode(goldNode)
            .WithResourceNode(manaNode)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(150, 75, 0, 0, 0, 0))
        ]);
    }

    [Test]
    public void NeutralTerrainNodeTest()
    {
        WorldMap map = TestHelpers.SimpleMap(1, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer neutralPlayer = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Neutral, map);

        WorldResourceNode node = new(
            Id: new WorldResourceNodeId("node"),
            Position: new WorldPosition(0, 0),
            ResourceType: WorldResourceType.Gold,
            AiPriority: 3
        );

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(neutralPlayer.LordId, neutralPlayer.Race, LordCategory.Warrior))
            .WithPlayer(neutralPlayer)
            .WithResourceNode(node)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 25, 0, 0, 0, 0))
        ]);
    }

    [Test]
    public void ResumeGameTest()
    {
        WorldMap map = TestHelpers.SimpleMap(1, 1);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Human, map);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithTurnInfo(new WorldTurnInfo(4, player2.Id))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .Build();

        WorldState initialState = engine.State;

        engine.CheckCommand(new WorldResumeGameCommand(), [
            new WorldGameResumedEvent(new WorldPlayerId(2))
        ]);

        Assert.That(engine.State, Is.SameAs(initialState));
    }
}
