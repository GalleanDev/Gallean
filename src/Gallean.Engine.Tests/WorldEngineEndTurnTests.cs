using Gallean.Common;

namespace Gallean.Engine.Tests;

public sealed class WorldEngineEndTurnTests
{
    [Test]
    public void EndTurnTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

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

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                TurnInfo = new WorldTurnInfo(2, new WorldPlayerId(1))
            }));
    }

    [Test]
    public void NotPlayerTurnTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 1);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(9999, 0, 0, 0, 0, 0),
            FogOfWar = ImmutableBitmap.Create(map.Width, map.Height)
        };

        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map) with
        {
            Resources = new WorldResourceSet(9999, 0, 0, 0, 0, 0),
            FogOfWar = ImmutableBitmap.Create(map.Width, map.Height)
        };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .Build();

        engine.CheckCommandError(new WorldConstructBuildingCommand(player2.Id, new WorldPlayerBuildingDefinitionId("RootMage")), WorldCommandError.NotPlayerTurn);
    }

    [Test]
    public void NeutralTurnTest()
    {
        WorldMap map = TestHelpers.SimpleMap(4, 1);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player4 = TestHelpers.SimplePlayer(new WorldPlayerId(4), WorldRace.Neutral, map) with { IsAi = true };
        WorldPlayer player5 = TestHelpers.SimplePlayer(new WorldPlayerId(5), WorldRace.Elf, map) with { IsAi = true };

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 4, leadership: 6) with
            {
                Regeneration = 5
            };
        WorldStack stack5 = TestHelpers.SimpleStack(
            new WorldStackId("Stack5"),
            player5.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 0);
        WorldStack neutralStack = TestHelpers.SimpleStack(
            new WorldStackId("Neutral"),
            player4.Id,
            new WorldPosition(3, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader2"), leader) with { Hitpoints = 50 }, GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player4.LordId, player4.Race, LordCategory.Warrior))
            .WithPlayer(player4)
            .WithDefinition(TestHelpers.SimpleLord(player5.LordId, player5.Race, LordCategory.Warrior))
            .WithPlayer(player5)
            .WithStack(stack5)
            .WithStack(neutralStack)
            .Build();

        engine.CheckCommand(new WorldEndTurnCommand(), [
            new WorldTurnEndedEvent(player1.Id),
            new WorldTurnStartingEvent(player5.Id),
            new WorldStackMovementRestoredEvent(stack5.Id, 4),
            new WorldTurnStartedEvent(player5.Id),
            new WorldTurnResourcesGeneratedEvent(player5.Id, new WorldResourceSet(100, 0, 0, 0, 0, 25))
        ]);

        engine.CheckCommand(new WorldEndTurnCommand(), [
            new WorldTurnEndedEvent(player5.Id),
            new WorldTurnStartingEvent(player4.Id),
            new WorldStackMovementRestoredEvent(neutralStack.Id, 4),
            new WorldTurnStartedEvent(player4.Id),
            new WorldTurnResourcesGeneratedEvent(player4.Id, WorldResourceSet.Empty)
        ]);

        Assert.That(engine.State.Stacks[neutralStack.Id], Is.EqualTo(neutralStack with
        {
            Units = new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader2"), leader) with { Hitpoints = 65 }, GridPosition.FrontBottom, false)
                .Build(),
            MovementPoints = 4
        }));

        engine.CheckCommand(new WorldEndTurnCommand(), [
            new WorldTurnEndedEvent(player4.Id),
            new WorldRoundEndedEvent(),
            new WorldRoundStartedEvent(),
            new WorldTurnStartingEvent(player1.Id),
            new WorldTurnStartedEvent(player1.Id),
            new WorldTurnResourcesGeneratedEvent(player1.Id, new WorldResourceSet(100, 25, 0, 0, 0, 0))
        ]);

        Assert.That(engine.State.TurnInfo, Is.EqualTo(new WorldTurnInfo(2, player1.Id)));
    }

    [Test]
    public void TurnOrderTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Neutral, map) with { IsAi = true };
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Elf, map) with { IsAi = true };
        WorldPlayer player3 = TestHelpers.SimplePlayer(new WorldPlayerId(3), WorldRace.Undead, map);
        WorldPlayer player4 = TestHelpers.SimplePlayer(new WorldPlayerId(4), WorldRace.Human, map) with { IsAi = true };
        WorldPlayer player5 = TestHelpers.SimplePlayer(new WorldPlayerId(5), WorldRace.Heretic, map) with { IsAi = true };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithDefinition(TestHelpers.SimpleLord(player3.LordId, player3.Race, LordCategory.Warrior))
            .WithPlayer(player3)
            .WithDefinition(TestHelpers.SimpleLord(player4.LordId, player4.Race, LordCategory.Warrior))
            .WithPlayer(player4)
            .WithDefinition(TestHelpers.SimpleLord(player5.LordId, player5.Race, LordCategory.Warrior))
            .WithPlayer(player5)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player3.Id),
            new WorldTurnStartedEvent(player3.Id),
            new WorldTurnResourcesGeneratedEvent(player3.Id, new WorldResourceSet(100, 25, 0, 0, 0, 0))
        ]);

        engine.CheckCommand(new WorldEndTurnCommand(), [
            new WorldTurnEndedEvent(player3.Id),
            new WorldTurnStartingEvent(player4.Id),
            new WorldTurnStartedEvent(player4.Id),
            new WorldTurnResourcesGeneratedEvent(player4.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0))
        ]);

        engine.CheckCommand(new WorldEndTurnCommand(), [
            new WorldTurnEndedEvent(player4.Id),
            new WorldTurnStartingEvent(player5.Id),
            new WorldTurnStartedEvent(player5.Id),
            new WorldTurnResourcesGeneratedEvent(player5.Id, new WorldResourceSet(100, 0, 0, 0, 25, 0))
        ]);

        engine.CheckCommand(new WorldEndTurnCommand(), [
            new WorldTurnEndedEvent(player5.Id),
            new WorldTurnStartingEvent(player2.Id),
            new WorldTurnStartedEvent(player2.Id),
            new WorldTurnResourcesGeneratedEvent(player2.Id, new WorldResourceSet(100, 0, 0, 0, 0, 25))
        ]);

        engine.CheckCommand(new WorldEndTurnCommand(), [
            new WorldTurnEndedEvent(player2.Id),
            new WorldTurnStartingEvent(player1.Id),
            new WorldTurnStartedEvent(player1.Id),
            new WorldTurnResourcesGeneratedEvent(player1.Id, WorldResourceSet.Empty)
        ]);

        engine.CheckCommand(new WorldEndTurnCommand(), [
            new WorldTurnEndedEvent(player1.Id),
            new WorldRoundEndedEvent(),
            new WorldRoundStartedEvent(),
            new WorldTurnStartingEvent(player3.Id),
            new WorldTurnStartedEvent(player3.Id),
            new WorldTurnResourcesGeneratedEvent(player3.Id, new WorldResourceSet(100, 25, 0, 0, 0, 0))
        ]);
    }

    [Test]
    public void RelationTrendTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldRelation relation = new(
            new WorldRelationId(WorldRace.Undead, WorldRace.Heretic),
            Current: 45,
            Previous: 20,
            Alliance: false,
            AllianceTurn: 0,
            PermanentWar: false,
            PermanentAlliance: false);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithRelation(relation)
            .Build();

        _ = engine.DispatchCommand(new WorldEndTurnCommand());

        engine.CheckCommand(new WorldEndTurnCommand(), [
            new WorldTurnEndedEvent(player2.Id),
            new WorldRoundEndedEvent(),
            new WorldRoundStartedEvent(),
            new WorldTurnStartingEvent(player1.Id),
            new WorldTurnStartedEvent(player1.Id),
            new WorldTurnResourcesGeneratedEvent(player1.Id, new WorldResourceSet(100, 25, 0, 0, 0, 0))
        ]);

        Assert.That(engine.State.Relations.Values, Is.EqualTo([relation]));
    }

    [Test]
    public void StackNotFoundTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            FogOfWar = ImmutableBitmap.Create(map.Width, map.Height)
        };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .Build();

        engine.CheckCommandError(new WorldMoveStackCommand(new WorldStackId("FakeStack"), new WorldPosition(1, 1)), WorldCommandError.StackNotFound);
    }
}
