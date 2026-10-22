namespace Gallean.Engine.Tests;

public sealed class WorldEngineIncomeTests
{
    [Test]
    public void BaseTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0)
        };

        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map) with
        {
            Resources = new WorldResourceSet(0, 0, 10, 0, 0, 0)
        };

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 4, leadership: 6);
        WorldStack stack1 = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player1.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 4);

        WorldStack stack2 = TestHelpers.SimpleStack(
            new WorldStackId("Stack2"),
            player2.Id,
            new WorldPosition(2, 2),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader2"), leader),
            movementPoints: 4);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(stack1)
            .WithStack(stack2)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player1.Id),
            new WorldStackMovementRestoredEvent(stack1.Id, 4),
            new WorldTurnStartedEvent(player1.Id),
            new WorldTurnResourcesGeneratedEvent(player1.Id, new WorldResourceSet(100, 25, 0, 0, 0, 0))
        ]);

        engine.CheckCommand(new WorldEndTurnCommand(), [
            new WorldTurnEndedEvent(player1.Id),
            new WorldTurnStartingEvent(player2.Id),
            new WorldStackMovementRestoredEvent(stack2.Id, 4),
            new WorldTurnStartedEvent(player2.Id),
            new WorldTurnResourcesGeneratedEvent(player2.Id, new WorldResourceSet(100, 0, 0, 0, 25, 0))
        ]);

        engine.CheckCommand(new WorldEndTurnCommand(), [
            new WorldTurnEndedEvent(player2.Id),
            new WorldRoundEndedEvent(),
            new WorldRoundStartedEvent(),
            new WorldTurnStartingEvent(player1.Id),
            new WorldStackMovementRestoredEvent(stack1.Id, 4),
            new WorldTurnStartedEvent(player1.Id),
            new WorldTurnResourcesGeneratedEvent(player1.Id, new WorldResourceSet(100, 25, 0, 0, 0, 0))
        ]);

        WorldPlayer expectedPlayer1 = player1 with
        {
            Resources = new WorldResourceSet(300, 50, 0, 0, 0, 0)
        };
        WorldPlayer expectedPlayer2 = player2 with
        {
            Resources = new WorldResourceSet(100, 0, 10, 0, 25, 0)
        };

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Players = engine.State.Players
                    .SetItem(expectedPlayer1.Id, expectedPlayer1)
                    .SetItem(expectedPlayer2.Id, expectedPlayer2)
            }));
    }

    [Test]
    public void ResourceNodeTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            ".U."
        ]);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0)
        };

        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 5, leadership: 6);
        WorldStack stack1 = TestHelpers.SimpleStack(
            new WorldStackId("stack1"),
            player1.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 5);

        WorldStack stack2 = TestHelpers.SimpleStack(
            new WorldStackId("stack2"),
            player2.Id,
            new WorldPosition(2, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader2"), leader),
            movementPoints: 5);

        WorldResourceNode node = new(
            Id: new WorldResourceNodeId("gold_mine"),
            OwnerId: player1.Id,
            Position: new WorldPosition(1, 0),
            ResourceType: WorldResourceType.Gold,
            AiPriority: 3
        );

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(stack1)
            .WithStack(stack2)
            .WithResourceNode(node)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player1.Id),
            new WorldStackMovementRestoredEvent(stack1.Id, 5),
            new WorldTurnStartedEvent(player1.Id),
            new WorldTurnResourcesGeneratedEvent(player1.Id, new WorldResourceSet(150, 25, 0, 0, 0, 0))
        ]);

        engine.CheckCommand(new WorldEndTurnCommand(), [
            new WorldTurnEndedEvent(player1.Id),
            new WorldTurnStartingEvent(player2.Id),
            new WorldStackMovementRestoredEvent(stack2.Id, 5),
            new WorldTurnStartedEvent(player2.Id),
            new WorldTurnResourcesGeneratedEvent(player2.Id, new WorldResourceSet(100, 0, 0, 0, 25, 0))
        ]);

        engine.CheckCommand(new WorldEndTurnCommand(), [
            new WorldTurnEndedEvent(player2.Id),
            new WorldRoundEndedEvent(),
            new WorldRoundStartedEvent(),
            new WorldTurnStartingEvent(player1.Id),
            new WorldStackMovementRestoredEvent(stack1.Id, 5),
            new WorldTurnStartedEvent(player1.Id),
            new WorldTurnResourcesGeneratedEvent(player1.Id, new WorldResourceSet(150, 25, 0, 0, 0, 0))
        ]);

        WorldPlayer expectedPlayer1 = player1 with
        {
            Resources = new WorldResourceSet(400, 50, 0, 0, 0, 0)
        };

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Players = engine.State.Players.SetItem(expectedPlayer1.Id, expectedPlayer1)
            }));
    }

    [Test]
    public void EasyTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer ai = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map) with
        {
            IsAi = true
        };

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 4, leadership: 6);

        WorldStack humanStack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            human.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 4);

        WorldStack aiStack = TestHelpers.SimpleStack(
            new WorldStackId("Stack2"),
            ai.Id,
            new WorldPosition(2, 2),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader2"), leader),
            movementPoints: 4);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDifficulty(WorldDifficulty.Easy)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithDefinition(TestHelpers.SimpleLord(ai.LordId, ai.Race, LordCategory.Warrior))
            .WithPlayer(ai)
            .WithStack(humanStack)
            .WithStack(aiStack)
            .Build();

        engine.CheckCommand(new WorldEndTurnCommand(), [
            new WorldTurnEndedEvent(new WorldPlayerId(1)),
            new WorldTurnStartingEvent(new WorldPlayerId(2)),
            new WorldStackMovementRestoredEvent(new WorldStackId("Stack2"), 4),
            new WorldTurnStartedEvent(new WorldPlayerId(2)),
            new WorldTurnResourcesGeneratedEvent(new WorldPlayerId(2), new WorldResourceSet(91, 0, 0, 0, 22, 0))
        ]);

        engine.CheckCommand(new WorldEndTurnCommand(), [
            new WorldTurnEndedEvent(new WorldPlayerId(2)),
            new WorldRoundEndedEvent(),
            new WorldRoundStartedEvent(),
            new WorldTurnStartingEvent(new WorldPlayerId(1)),
            new WorldStackMovementRestoredEvent(new WorldStackId("Stack1"), 4),
            new WorldTurnStartedEvent(new WorldPlayerId(1)),
            new WorldTurnResourcesGeneratedEvent(new WorldPlayerId(1), new WorldResourceSet(130, 32, 0, 0, 0, 0))
        ]);
    }

    [Test]
    public void AverageTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer ai = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map) with
        {
            IsAi = true
        };

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 4, leadership: 6);

        WorldStack humanStack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            human.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 4);

        WorldStack aiStack = TestHelpers.SimpleStack(
            new WorldStackId("Stack2"),
            ai.Id,
            new WorldPosition(2, 2),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader2"), leader),
            movementPoints: 4);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDifficulty(WorldDifficulty.Average)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithDefinition(TestHelpers.SimpleLord(ai.LordId, ai.Race, LordCategory.Warrior))
            .WithPlayer(ai)
            .WithStack(humanStack)
            .WithStack(aiStack)
            .Build();

        engine.CheckCommand(new WorldEndTurnCommand(), [
            new WorldTurnEndedEvent(new WorldPlayerId(1)),
            new WorldTurnStartingEvent(new WorldPlayerId(2)),
            new WorldStackMovementRestoredEvent(new WorldStackId("Stack2"), 4),
            new WorldTurnStartedEvent(new WorldPlayerId(2)),
            new WorldTurnResourcesGeneratedEvent(new WorldPlayerId(2), new WorldResourceSet(100, 0, 0, 0, 25, 0))
        ]);

        engine.CheckCommand(new WorldEndTurnCommand(), [
            new WorldTurnEndedEvent(new WorldPlayerId(2)),
            new WorldRoundEndedEvent(),
            new WorldRoundStartedEvent(),
            new WorldTurnStartingEvent(new WorldPlayerId(1)),
            new WorldStackMovementRestoredEvent(new WorldStackId("Stack1"), 4),
            new WorldTurnStartedEvent(new WorldPlayerId(1)),
            new WorldTurnResourcesGeneratedEvent(new WorldPlayerId(1), new WorldResourceSet(100, 25, 0, 0, 0, 0))
        ]);
    }

    [Test]
    public void HardTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer ai = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map) with
        {
            IsAi = true
        };

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 4, leadership: 6);

        WorldStack humanStack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            human.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 4);

        WorldStack aiStack = TestHelpers.SimpleStack(
            new WorldStackId("Stack2"),
            ai.Id,
            new WorldPosition(2, 2),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader2"), leader),
            movementPoints: 4);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDifficulty(WorldDifficulty.Hard)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithDefinition(TestHelpers.SimpleLord(ai.LordId, ai.Race, LordCategory.Warrior))
            .WithPlayer(ai)
            .WithStack(humanStack)
            .WithStack(aiStack)
            .Build();

        engine.CheckCommand(new WorldEndTurnCommand(), [
            new WorldTurnEndedEvent(new WorldPlayerId(1)),
            new WorldTurnStartingEvent(new WorldPlayerId(2)),
            new WorldStackMovementRestoredEvent(new WorldStackId("Stack2"), 4),
            new WorldTurnStartedEvent(new WorldPlayerId(2)),
            new WorldTurnResourcesGeneratedEvent(new WorldPlayerId(2), new WorldResourceSet(130, 0, 0, 0, 32, 0))
        ]);

        engine.CheckCommand(new WorldEndTurnCommand(), [
            new WorldTurnEndedEvent(new WorldPlayerId(2)),
            new WorldRoundEndedEvent(),
            new WorldRoundStartedEvent(),
            new WorldTurnStartingEvent(new WorldPlayerId(1)),
            new WorldStackMovementRestoredEvent(new WorldStackId("Stack1"), 4),
            new WorldTurnStartedEvent(new WorldPlayerId(1)),
            new WorldTurnResourcesGeneratedEvent(new WorldPlayerId(1), new WorldResourceSet(100, 25, 0, 0, 0, 0))
        ]);
    }

    [Test]
    public void VeryHardTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer ai = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map) with
        {
            IsAi = true
        };

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 4, leadership: 6);

        WorldStack humanStack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            human.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 4);

        WorldStack aiStack = TestHelpers.SimpleStack(
            new WorldStackId("Stack2"),
            ai.Id,
            new WorldPosition(2, 2),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader2"), leader),
            movementPoints: 4);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDifficulty(WorldDifficulty.VeryHard)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithDefinition(TestHelpers.SimpleLord(ai.LordId, ai.Race, LordCategory.Warrior))
            .WithPlayer(ai)
            .WithStack(humanStack)
            .WithStack(aiStack)
            .Build();

        engine.CheckCommand(new WorldEndTurnCommand(), [
            new WorldTurnEndedEvent(new WorldPlayerId(1)),
            new WorldTurnStartingEvent(new WorldPlayerId(2)),
            new WorldStackMovementRestoredEvent(new WorldStackId("Stack2"), 4),
            new WorldTurnStartedEvent(new WorldPlayerId(2)),
            new WorldTurnResourcesGeneratedEvent(new WorldPlayerId(2), new WorldResourceSet(145, 0, 0, 0, 36, 0))
        ]);

        engine.CheckCommand(new WorldEndTurnCommand(), [
            new WorldTurnEndedEvent(new WorldPlayerId(2)),
            new WorldRoundEndedEvent(),
            new WorldRoundStartedEvent(),
            new WorldTurnStartingEvent(new WorldPlayerId(1)),
            new WorldStackMovementRestoredEvent(new WorldStackId("Stack1"), 4),
            new WorldTurnStartedEvent(new WorldPlayerId(1)),
            new WorldTurnResourcesGeneratedEvent(new WorldPlayerId(1), new WorldResourceSet(100, 25, 0, 0, 0, 0))
        ]);
    }

    [Test]
    public void EasyResourceNodeTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer ai = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map) with
        {
            IsAi = true
        };

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 4, leadership: 6);

        WorldStack humanStack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            human.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 4);

        WorldStack aiStack = TestHelpers.SimpleStack(
            new WorldStackId("Stack2"),
            ai.Id,
            new WorldPosition(2, 2),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader2"), leader),
            movementPoints: 4);

        WorldResourceNode humanNode = new(
            new WorldResourceNodeId("node1"),
            new WorldPlayerId(1),
            new WorldPosition(1, 0),
            WorldResourceType.Gold,
            AiPriority: 3);

        WorldResourceNode aiNode = new(
            new WorldResourceNodeId("node2"),
            new WorldPlayerId(2),
            new WorldPosition(1, 2),
            WorldResourceType.InfernalMana,
            AiPriority: 3);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDifficulty(WorldDifficulty.Easy)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithDefinition(TestHelpers.SimpleLord(ai.LordId, ai.Race, LordCategory.Warrior))
            .WithPlayer(ai)
            .WithStack(humanStack)
            .WithStack(aiStack)
            .WithResourceNode(humanNode)
            .WithResourceNode(aiNode)
            .Build();

        engine.CheckCommand(new WorldEndTurnCommand(), [
            new WorldTurnEndedEvent(new WorldPlayerId(1)),
            new WorldTurnStartingEvent(new WorldPlayerId(2)),
            new WorldStackMovementRestoredEvent(new WorldStackId("Stack2"), 4),
            new WorldTurnStartedEvent(new WorldPlayerId(2)),
            new WorldTurnResourcesGeneratedEvent(new WorldPlayerId(2), new WorldResourceSet(91, 0, 0, 0, 67, 0))
        ]);

        engine.CheckCommand(new WorldEndTurnCommand(), [
            new WorldTurnEndedEvent(new WorldPlayerId(2)),
            new WorldRoundEndedEvent(),
            new WorldRoundStartedEvent(),
            new WorldTurnStartingEvent(new WorldPlayerId(1)),
            new WorldStackMovementRestoredEvent(new WorldStackId("Stack1"), 4),
            new WorldTurnStartedEvent(new WorldPlayerId(1)),
            new WorldTurnResourcesGeneratedEvent(new WorldPlayerId(1), new WorldResourceSet(195, 32, 0, 0, 0, 0))
        ]);
    }
}
