namespace Gallean.Engine.Tests;

public sealed class WorldEngineThiefSpyTests
{
    [Test]
    public void StackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 40, damage: 5),
            movementPoints: 6, leadership: 6);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack thiefStack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player1.Id,
            new WorldPosition(2, 2),
            TestHelpers.SingleUnitGrid(new WorldUnitId("thief"), thief),
            movementPoints: 20);

        WorldStack targetStack = TestHelpers.SimpleStack(
            new WorldStackId("enemy"),
            player2.Id,
            new WorldPosition(3, 2),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithRandom(new TestSequenceRandomProvider(0, 0))
            .WithDefinition(thief)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Spy, 100))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(thiefStack)
            .WithStack(targetStack)
            .Build();

        WorldState before = engine.State;

        engine.CheckCommand(
            new WorldThiefActionCommand(thiefStack.Id, new WorldThiefSpyAction(new WorldPosition(3, 2))),
            [
                new WorldThiefActionSucceededEvent(thiefStack.Id, WorldThiefActionType.Spy, player2.Id),
                new WorldSpyAddedEvent(player1.Id, player2.Id),
                new WorldStackMovementConsumedEvent(thiefStack.Id, 10)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            before with
            {
                Players = before.Players.SetItem(player1.Id, player1 with
                {
                    SpiedPlayers = [player2.Id],
                    SpiedRuins = []
                }),
                Stacks = before.Stacks.SetItem(thiefStack.Id, thiefStack with { MovementPoints = 10 })
            }));
    }

    [Test]
    public void CityTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition soldier = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 5);
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack thiefStack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player1.Id,
            new WorldPosition(4, 3),
            TestHelpers.SingleUnitGrid(new WorldUnitId("thief"), thief),
            movementPoints: 20);
        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            player2.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldier), GridPosition.FrontBottom, false)
                .Build());

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithRandom(new TestSequenceRandomProvider(0, 0))
            .WithDefinition(thief)
            .WithDefinition(soldier)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Spy, 100))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(thiefStack)
            .WithCity(city)
            .Build();

        WorldState before = engine.State;

        engine.CheckCommand(
            new WorldThiefActionCommand(thiefStack.Id, new WorldThiefSpyAction(new WorldPosition(3, 3))),
            [
                new WorldThiefActionSucceededEvent(thiefStack.Id, WorldThiefActionType.Spy, player2.Id),
                new WorldSpyAddedEvent(player1.Id, player2.Id),
                new WorldStackMovementConsumedEvent(thiefStack.Id, 10)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            before with
            {
                Players = before.Players.SetItem(player1.Id, player1 with
                {
                    SpiedPlayers = [player2.Id],
                    SpiedRuins = []
                }),
                Stacks = before.Stacks.SetItem(thiefStack.Id, thiefStack with { MovementPoints = 10 })
            }));
    }

    [Test]
    public void AlreadySpiedTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 40, damage: 5),
            movementPoints: 6, leadership: 6);

        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);
        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            SpiedPlayers = [new WorldPlayerId(2)],
            SpiedRuins = []
        };

        WorldStack thiefStack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player1.Id,
            new WorldPosition(2, 2),
            TestHelpers.SingleUnitGrid(new WorldUnitId("thief"), thief),
            movementPoints: 20);

        WorldStack targetStack = TestHelpers.SimpleStack(
            new WorldStackId("enemy"),
            player2.Id,
            new WorldPosition(3, 2),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Spy, 100))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(thiefStack)
            .WithStack(targetStack)
            .Build();

        WorldState before = engine.State;

        engine.CheckCommandError(
            new WorldThiefActionCommand(thiefStack.Id, new WorldThiefSpyAction(new WorldPosition(3, 2))),
            WorldCommandError.ThiefTargetAlreadyKnown);

        Assert.That(engine.State, Is.EqualTo(before));
    }

    [Test]
    public void NeutralTargetTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 40, damage: 5),
            movementPoints: 6, leadership: 6);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer neutral = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Neutral, map);

        WorldStack thiefStack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player1.Id,
            new WorldPosition(2, 2),
            TestHelpers.SingleUnitGrid(new WorldUnitId("thief"), thief),
            movementPoints: 20);

        WorldStack targetStack = TestHelpers.SimpleStack(
            new WorldStackId("neutral"),
            neutral.Id,
            new WorldPosition(3, 2),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Spy, 100))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(neutral.LordId, neutral.Race, LordCategory.Warrior))
            .WithPlayer(neutral)
            .WithStack(thiefStack)
            .WithStack(targetStack)
            .Build();

        WorldState before = engine.State;

        engine.CheckCommandError(
            new WorldThiefActionCommand(thiefStack.Id, new WorldThiefSpyAction(new WorldPosition(3, 2))),
            WorldCommandError.ThiefTargetAlreadyKnown);

        Assert.That(engine.State, Is.EqualTo(before));
    }

    [Test]
    public void RuinTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition guard = TestHelpers.SimpleUnitDefinition("guard", maxHitpoints: 100, initiative: 40, damage: 5);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldStack thiefStack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player1.Id,
            new WorldPosition(5, 4),
            TestHelpers.SingleUnitGrid(new WorldUnitId("thief"), thief),
            movementPoints: 20);

        WorldRuin ruin = new(
            new WorldFortificationId("ruin"),
            new ScenarioTextId("Ruin"),
            new WorldFootprint(new WorldPosition(2, 2), 3, 3),
            new WorldObjectVariant(0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("guard"), guard),
            new WorldResourceSet(300, 0, 0, 0, 0, 0),
            Item: null,
            LooterId: null,
            AiPriority: 3);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithRandom(new TestSequenceRandomProvider(0, 0))
            .WithDefinition(thief)
            .WithDefinition(guard)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.SpyRuin, 90))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithStack(thiefStack)
            .WithRuin(ruin)
            .Build();

        WorldState before = engine.State;

        engine.CheckCommand(
            new WorldThiefActionCommand(thiefStack.Id, new WorldThiefSpyRuinAction(new WorldPosition(4, 4))),
            [
                new WorldThiefActionSucceededEvent(thiefStack.Id, WorldThiefActionType.SpyRuin, null),
                new WorldRuinSpiedEvent(player1.Id, ruin.Id),
                new WorldStackMovementConsumedEvent(thiefStack.Id, 10)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            before with
            {
                Players = before.Players.SetItem(player1.Id, player1 with
                {
                    SpiedPlayers = [],
                    SpiedRuins = [ruin.Id]
                }),
                Stacks = before.Stacks.SetItem(thiefStack.Id, thiefStack with { MovementPoints = 10 })
            }));
    }

    [Test]
    public void RuinAlreadySpiedTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition guard = TestHelpers.SimpleUnitDefinition("guard", maxHitpoints: 100, initiative: 40, damage: 5);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            SpiedPlayers = [],
            SpiedRuins = [new WorldFortificationId("ruin")]
        };

        WorldStack thiefStack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player1.Id,
            new WorldPosition(5, 4),
            TestHelpers.SingleUnitGrid(new WorldUnitId("thief"), thief),
            movementPoints: 20);

        WorldRuin ruin = new(
            new WorldFortificationId("ruin"),
            new ScenarioTextId("Ruin"),
            new WorldFootprint(new WorldPosition(2, 2), 3, 3),
            new WorldObjectVariant(0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("guard"), guard),
            new WorldResourceSet(300, 0, 0, 0, 0, 0),
            Item: null,
            LooterId: null,
            AiPriority: 3);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(guard)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.SpyRuin, 90))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithStack(thiefStack)
            .WithRuin(ruin)
            .Build();

        WorldState before = engine.State;

        engine.CheckCommandError(
            new WorldThiefActionCommand(thiefStack.Id, new WorldThiefSpyRuinAction(new WorldPosition(4, 4))),
            WorldCommandError.ThiefTargetAlreadyKnown);

        Assert.That(engine.State, Is.EqualTo(before));
    }

    [Test]
    public void RuinActionAgainstStackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 40, damage: 5),
            movementPoints: 6, leadership: 6);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack thiefStack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player1.Id,
            new WorldPosition(2, 2),
            TestHelpers.SingleUnitGrid(new WorldUnitId("thief"), thief),
            movementPoints: 20);

        WorldStack targetStack = TestHelpers.SimpleStack(
            new WorldStackId("enemy"),
            player2.Id,
            new WorldPosition(3, 2),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.SpyRuin, 90))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(thiefStack)
            .WithStack(targetStack)
            .Build();

        WorldState before = engine.State;

        engine.CheckCommandError(
            new WorldThiefActionCommand(thiefStack.Id, new WorldThiefSpyRuinAction(new WorldPosition(3, 2))),
            WorldCommandError.ThiefTargetInvalid);

        Assert.That(engine.State, Is.EqualTo(before));
    }

    [Test]
    public void AllianceBrokenTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 40, damage: 5),
            movementPoints: 6, leadership: 6);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            SpiedPlayers = [new WorldPlayerId(2)],
            SpiedRuins = []
        };
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map) with
        {
            SpiedPlayers = [new WorldPlayerId(1)],
            SpiedRuins = []
        };

        WorldRelation relation = new(
            new WorldRelationId(WorldRace.Undead, WorldRace.Heretic),
            Current: 100,
            Previous: 100,
            Alliance: true,
            AllianceTurn: 1,
            PermanentWar: false,
            PermanentAlliance: false);

        WorldStack stack1 = TestHelpers.SimpleStack(
            new WorldStackId("stack1"),
            player1.Id,
            new WorldPosition(1, 1),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithRelation(relation)
            .WithStack(stack1)
            .Build();

        engine.CheckCommand(
            new WorldDiplomacyActionCommand(player2.Id, new WorldDiplomacyBreakAllianceAction()),
            [
                new WorldAllianceEndedEvent(player1.Id, player2.Id),
                new WorldRelationChangedEvent(player1.Id, player2.Id, 10),
                new WorldSpyRemovedEvent(player1.Id, player2.Id),
                new WorldSpyRemovedEvent(player2.Id, player1.Id)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Players = engine.State.Players
                    .SetItem(player1.Id, player1 with { SpiedPlayers = [] })
                    .SetItem(player2.Id, player2 with { SpiedPlayers = [] }),
                Relations = engine.State.Relations.SetItem(relation.Id, relation with
                {
                    Current = 10,
                    Alliance = false,
                    AllianceTurn = 0
                })
            }));
    }

    [Test]
    public void DiscoveredAtTurnStartTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 40, damage: 5),
            movementPoints: 6, leadership: 6);

        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);
        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            SpiedPlayers = [new WorldPlayerId(2)],
            SpiedRuins = []
        };

        WorldStack stack1 = TestHelpers.SimpleStack(
            new WorldStackId("stack1"),
            player1.Id,
            new WorldPosition(1, 1),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithRandom(new TestSequenceRandomProvider(0))
            .WithTurnInfo(new WorldTurnInfo(1, player2.Id))
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(stack1)
            .Build();

        engine.CheckCommand(new WorldEndTurnCommand(), [
            new WorldTurnEndedEvent(player2.Id),
            new WorldRoundEndedEvent(),
            new WorldRoundStartedEvent(),
            new WorldTurnStartingEvent(player1.Id),
            new WorldSpyRemovedEvent(player1.Id, player2.Id),
            new WorldStackMovementRestoredEvent(stack1.Id, 6),
            new WorldTurnStartedEvent(player1.Id),
            new WorldTurnResourcesGeneratedEvent(player1.Id, new WorldResourceSet(100, 25, 0, 0, 0, 0))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Players = engine.State.Players.SetItem(player1.Id, player1 with
                {
                    Resources = new WorldResourceSet(100, 25, 0, 0, 0, 0),
                    SpiedPlayers = []
                })
            }));
    }

    [Test]
    public void SurvivesTurnStartTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 40, damage: 5),
            movementPoints: 6, leadership: 6);

        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);
        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            SpiedPlayers = [new WorldPlayerId(2)],
            SpiedRuins = []
        };

        WorldStack stack1 = TestHelpers.SimpleStack(
            new WorldStackId("stack1"),
            player1.Id,
            new WorldPosition(1, 1),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithRandom(new TestSequenceRandomProvider(10))
            .WithTurnInfo(new WorldTurnInfo(1, player2.Id))
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(stack1)
            .Build();

        engine.CheckCommand(new WorldEndTurnCommand(), [
            new WorldTurnEndedEvent(player2.Id),
            new WorldRoundEndedEvent(),
            new WorldRoundStartedEvent(),
            new WorldTurnStartingEvent(player1.Id),
            new WorldStackMovementRestoredEvent(stack1.Id, 6),
            new WorldTurnStartedEvent(player1.Id),
            new WorldTurnResourcesGeneratedEvent(player1.Id, new WorldResourceSet(100, 25, 0, 0, 0, 0))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Players = engine.State.Players.SetItem(player1.Id, player1 with
                {
                    Resources = new WorldResourceSet(100, 25, 0, 0, 0, 0)
                })
            }));
    }
}
