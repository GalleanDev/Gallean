using Gallean.Common;

namespace Gallean.Engine.Tests;

public sealed class WorldEngineAiOrderTests
{
    [Test]
    public void HurtStackTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^^.^",
            "......"
        ]);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 3);

        WorldPlayer aiPlayer = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            IsAi = true,
            Resources = WorldResourceSet.Empty
        };
        WorldPlayer humanPlayer = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            aiPlayer.Id,
            new WorldPosition(0, 5),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition) with { Hitpoints = 30 }, GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 2);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(TestHelpers.SimpleLord(aiPlayer.LordId, aiPlayer.Race, LordCategory.Warrior))
            .WithPlayer(aiPlayer)
            .WithDefinition(TestHelpers.SimpleLord(humanPlayer.LordId, humanPlayer.Race, LordCategory.Warrior))
            .WithPlayer(humanPlayer)
            .WithCapital(TestHelpers.SimpleCapital(new WorldFortificationId("cap"), aiPlayer.Id, new WorldPosition(4, 4), UnitGrid<WorldUnit>.Empty))
            .WithStack(stack)
            .Build();

        WorldAiMemory memory = new WorldAiMemory(
                new WorldAiTurnMemory(1, aiPlayer.Id, 1, 0, 0, []))
            .WithStackMemory(stack.Id, new WorldAiStackMemory(WorldStackOrder.Heal.Instance, null));
        WorldAiActionPlan plan = new(
            new WorldAiActionScore(1, 1000),
            new WorldAiMoveStackCommand(stack.Id, new WorldPosition(1, 5)),
            memory);

        engine.CheckCommand(new WorldAiActionCommand(), [
            new WorldAiActionStartedEvent(plan),
            new WorldStackMovingEvent(stack.Id, new WorldPosition(0, 5), new WorldPosition(1, 5), WorldStackDirection.SouthEast, 0, [], null),
            new WorldStackMovedEvent(stack.Id, new WorldPosition(0, 5), new WorldPosition(1, 5), WorldStackDirection.SouthEast, 0, [], null),
            new WorldAiActionEndedEvent(aiPlayer.Id, memory)
        ]);
    }

    [Test]
    public void AttackTargetGoneTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 1);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 3);

        WorldPlayer aiPlayer = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            IsAi = true,
            Resources = WorldResourceSet.Empty
        };
        WorldPlayer humanPlayer = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Stack1"),
                aiPlayer.Id,
                new WorldPosition(0, 0),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom, false)
                    .Build(),
                movementPoints: 0) with
            {
                Order = new WorldStackOrder.AttackStack(new WorldStackId("missing"))
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(TestHelpers.SimpleLord(aiPlayer.LordId, aiPlayer.Race, LordCategory.Warrior))
            .WithPlayer(aiPlayer)
            .WithDefinition(TestHelpers.SimpleLord(humanPlayer.LordId, humanPlayer.Race, LordCategory.Warrior))
            .WithPlayer(humanPlayer)
            .WithStack(stack)
            .Build();

        WorldAiMemory memory = new(new WorldAiTurnMemory(1, aiPlayer.Id, 1, 0, 0, []));
        WorldAiActionPlan plan = new(
            new WorldAiActionScore(0, 1000),
            new WorldChangeStackOrderCommand(stack.Id, WorldStackOrder.Berserk.Instance),
            memory);

        engine.CheckCommand(new WorldAiActionCommand(), [
            new WorldAiActionStartedEvent(plan),
            new WorldStackOrderChangedEvent(stack.Id, WorldStackOrder.Berserk.Instance),
            new WorldAiActionEndedEvent(aiPlayer.Id, memory)
        ]);
    }

    [Test]
    public void AttackTargetConcealedTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 1);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 3);

        WorldPlayer aiPlayer = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            IsAi = true,
            Resources = WorldResourceSet.Empty
        };
        WorldPlayer humanPlayer = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack targetStack = TestHelpers.SimpleStack(
                new WorldStackId("Target"),
                humanPlayer.Id,
                new WorldPosition(1, 0),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom, false)
                    .Build(),
                movementPoints: 0) with
            {
                Concealed = true
            };

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Stack1"),
                aiPlayer.Id,
                new WorldPosition(0, 0),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader2"), leaderDefinition), GridPosition.FrontBottom, false)
                    .Build(),
                movementPoints: 0) with
            {
                Order = new WorldStackOrder.AttackStack(targetStack.Id)
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(TestHelpers.SimpleLord(aiPlayer.LordId, aiPlayer.Race, LordCategory.Warrior))
            .WithPlayer(aiPlayer)
            .WithDefinition(TestHelpers.SimpleLord(humanPlayer.LordId, humanPlayer.Race, LordCategory.Warrior))
            .WithPlayer(humanPlayer)
            .WithStack(targetStack)
            .WithStack(stack)
            .Build();

        WorldAiMemory memory = new(new WorldAiTurnMemory(1, aiPlayer.Id, 1, 0, 0, []));
        WorldAiActionPlan plan = new(
            new WorldAiActionScore(0, 1000),
            new WorldChangeStackOrderCommand(stack.Id, WorldStackOrder.Berserk.Instance),
            memory);

        engine.CheckCommand(new WorldAiActionCommand(), [
            new WorldAiActionStartedEvent(plan),
            new WorldStackOrderChangedEvent(stack.Id, WorldStackOrder.Berserk.Instance),
            new WorldAiActionEndedEvent(aiPlayer.Id, memory)
        ]);
    }

    [Test]
    public void DefendTargetGoneTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 1);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 3);

        WorldPlayer aiPlayer = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            IsAi = true,
            Resources = WorldResourceSet.Empty
        };
        WorldPlayer humanPlayer = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Stack1"),
                aiPlayer.Id,
                new WorldPosition(0, 0),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom, false)
                    .Build(),
                movementPoints: 0) with
            {
                Order = new WorldStackOrder.DefendStack(new WorldStackId("missing"))
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(TestHelpers.SimpleLord(aiPlayer.LordId, aiPlayer.Race, LordCategory.Warrior))
            .WithPlayer(aiPlayer)
            .WithDefinition(TestHelpers.SimpleLord(humanPlayer.LordId, humanPlayer.Race, LordCategory.Warrior))
            .WithPlayer(humanPlayer)
            .WithStack(stack)
            .Build();

        WorldAiMemory memory = new(new WorldAiTurnMemory(1, aiPlayer.Id, 1, 0, 0, []));
        WorldAiActionPlan plan = new(
            new WorldAiActionScore(0, 1000),
            new WorldChangeStackOrderCommand(stack.Id, WorldStackOrder.Normal.Instance),
            memory);

        engine.CheckCommand(new WorldAiActionCommand(), [
            new WorldAiActionStartedEvent(plan),
            new WorldStackOrderChangedEvent(stack.Id, WorldStackOrder.Normal.Instance),
            new WorldAiActionEndedEvent(aiPlayer.Id, memory)
        ]);
    }

    [Test]
    public void NeutralDefendTargetGoneTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 1);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 3);

        WorldPlayer neutralPlayer = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Neutral, map) with
        {
            IsAi = true,
            Resources = WorldResourceSet.Empty
        };
        WorldPlayer humanPlayer = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Stack1"),
                neutralPlayer.Id,
                new WorldPosition(0, 0),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom, false)
                    .Build(),
                movementPoints: 0) with
            {
                Order = new WorldStackOrder.DefendStack(new WorldStackId("missing"))
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(TestHelpers.SimpleLord(neutralPlayer.LordId, neutralPlayer.Race, LordCategory.Warrior))
            .WithPlayer(neutralPlayer)
            .WithDefinition(TestHelpers.SimpleLord(humanPlayer.LordId, humanPlayer.Race, LordCategory.Warrior))
            .WithPlayer(humanPlayer)
            .WithStack(stack)
            .Build();

        WorldAiMemory memory = new(new WorldAiTurnMemory(1, neutralPlayer.Id, 1, 0, 0, []));
        WorldAiActionPlan plan = new(
            new WorldAiActionScore(0, 1000),
            new WorldChangeStackOrderCommand(stack.Id, WorldStackOrder.Roam.Instance),
            memory);

        engine.CheckCommand(new WorldAiActionCommand(), [
            new WorldAiActionStartedEvent(plan),
            new WorldStackOrderChangedEvent(stack.Id, WorldStackOrder.Roam.Instance),
            new WorldAiActionEndedEvent(neutralPlayer.Id, memory)
        ]);
    }

    [Test]
    public void MoveToLocationReachedTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 1);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 3);

        WorldPlayer aiPlayer = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            IsAi = true,
            Resources = WorldResourceSet.Empty
        };
        WorldPlayer humanPlayer = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Stack1"),
                aiPlayer.Id,
                new WorldPosition(0, 0),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom, false)
                    .Build(),
                movementPoints: 0) with
            {
                Order = new WorldStackOrder.MoveToLocation(new ScenarioLocationId("Target"), new WorldPosition(0, 0), Radius: 0)
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(TestHelpers.SimpleLord(aiPlayer.LordId, aiPlayer.Race, LordCategory.Warrior))
            .WithPlayer(aiPlayer)
            .WithDefinition(TestHelpers.SimpleLord(humanPlayer.LordId, humanPlayer.Race, LordCategory.Warrior))
            .WithPlayer(humanPlayer)
            .WithStack(stack)
            .Build();

        WorldAiMemory memory = new(new WorldAiTurnMemory(1, aiPlayer.Id, 1, 0, 0, []));
        WorldAiActionPlan plan = new(
            new WorldAiActionScore(0, 1000),
            new WorldChangeStackOrderCommand(stack.Id, WorldStackOrder.Stand.Instance),
            memory);

        engine.CheckCommand(new WorldAiActionCommand(), [
            new WorldAiActionStartedEvent(plan),
            new WorldStackOrderChangedEvent(stack.Id, WorldStackOrder.Stand.Instance),
            new WorldAiActionEndedEvent(aiPlayer.Id, memory)
        ]);
    }

    [Test]
    public void HurtStackAuthoredOrderTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^^.^",
            "......"
        ]);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 3);

        WorldPlayer aiPlayer = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            IsAi = true,
            Resources = WorldResourceSet.Empty
        };
        WorldPlayer humanPlayer = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Stack1"),
                aiPlayer.Id,
                new WorldPosition(0, 5),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition) with { Hitpoints = 30 }, GridPosition.FrontBottom, false)
                    .Build(),
                movementPoints: 2) with
            {
                Order = WorldStackOrder.Roam.Instance
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(TestHelpers.SimpleLord(aiPlayer.LordId, aiPlayer.Race, LordCategory.Warrior))
            .WithPlayer(aiPlayer)
            .WithDefinition(TestHelpers.SimpleLord(humanPlayer.LordId, humanPlayer.Race, LordCategory.Warrior))
            .WithPlayer(humanPlayer)
            .WithCapital(TestHelpers.SimpleCapital(new WorldFortificationId("cap"), aiPlayer.Id, new WorldPosition(4, 4), UnitGrid<WorldUnit>.Empty))
            .WithStack(stack)
            .Build();

        WorldAiMemory memory = new(new WorldAiTurnMemory(1, aiPlayer.Id, 0, 0, 0, [stack.Id]));

        engine.CheckCommand(new WorldAiActionCommand(), [
            new WorldAiActionStartedEvent(WorldAiActionPlan.EndTurn(memory)),
            new WorldTurnEndedEvent(aiPlayer.Id),
            new WorldRoundEndedEvent(),
            new WorldRoundStartedEvent(),
            new WorldTurnStartingEvent(humanPlayer.Id),
            new WorldTurnStartedEvent(humanPlayer.Id),
            new WorldTurnResourcesGeneratedEvent(humanPlayer.Id, new WorldResourceSet(100, 0, 0, 0, 25, 0)),
            new WorldAiActionEndedEvent(aiPlayer.Id, memory)
        ]);

        Assert.That(engine.State.Stacks[stack.Id].Order, Is.EqualTo(WorldStackOrder.Roam.Instance));
    }

    [Test]
    public void BasicOrderTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 3);

        WorldPlayer aiPlayer = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            IsAi = true,
            Resources = WorldResourceSet.Empty
        };
        WorldPlayer humanPlayer = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            aiPlayer.Id,
            new WorldPosition(0, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 0);

        WorldStack enemyStack = TestHelpers.SimpleStack(
            new WorldStackId("Stack2"),
            humanPlayer.Id,
            new WorldPosition(2, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader2"), leaderDefinition), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(TestHelpers.SimpleLord(aiPlayer.LordId, aiPlayer.Race, LordCategory.Warrior))
            .WithPlayer(aiPlayer)
            .WithDefinition(TestHelpers.SimpleLord(humanPlayer.LordId, humanPlayer.Race, LordCategory.Warrior))
            .WithPlayer(humanPlayer)
            .WithStack(stack)
            .WithStack(enemyStack)
            .Build();

        WorldAiMemory memory = new WorldAiMemory(new WorldAiTurnMemory(1, aiPlayer.Id, 0, 0, 0, [stack.Id]))
            .WithStackMemory(stack.Id, new WorldAiStackMemory(WorldStackOrder.Normal.Instance, null));

        engine.CheckCommand(new WorldAiActionCommand(), [
            new WorldAiActionStartedEvent(WorldAiActionPlan.EndTurn(memory)),
            new WorldTurnEndedEvent(aiPlayer.Id),
            new WorldRoundEndedEvent(),
            new WorldRoundStartedEvent(),
            new WorldTurnStartingEvent(humanPlayer.Id),
            new WorldStackMovementRestoredEvent(enemyStack.Id, 6),
            new WorldTurnStartedEvent(humanPlayer.Id),
            new WorldTurnResourcesGeneratedEvent(humanPlayer.Id, new WorldResourceSet(100, 0, 0, 0, 25, 0)),
            new WorldAiActionEndedEvent(aiPlayer.Id, memory)
        ]);
    }

    [Test]
    public void NeutralNormalOrderTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 3);

        WorldPlayer neutralPlayer = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Neutral, map) with
        {
            IsAi = true,
            Resources = WorldResourceSet.Empty
        };
        WorldPlayer humanPlayer = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            neutralPlayer.Id,
            new WorldPosition(0, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 0);

        WorldStack enemyStack = TestHelpers.SimpleStack(
            new WorldStackId("Stack2"),
            humanPlayer.Id,
            new WorldPosition(2, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader2"), leaderDefinition), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(TestHelpers.SimpleLord(neutralPlayer.LordId, neutralPlayer.Race, LordCategory.Warrior))
            .WithPlayer(neutralPlayer)
            .WithDefinition(TestHelpers.SimpleLord(humanPlayer.LordId, humanPlayer.Race, LordCategory.Warrior))
            .WithPlayer(humanPlayer)
            .WithStack(stack)
            .WithStack(enemyStack)
            .Build();

        WorldAiMemory memory = new(new WorldAiTurnMemory(1, neutralPlayer.Id, 0, 0, 0, [stack.Id]));

        engine.CheckCommand(new WorldAiActionCommand(), [
            new WorldAiActionStartedEvent(WorldAiActionPlan.EndTurn(memory)),
            new WorldTurnEndedEvent(neutralPlayer.Id),
            new WorldRoundEndedEvent(),
            new WorldRoundStartedEvent(),
            new WorldTurnStartingEvent(humanPlayer.Id),
            new WorldStackMovementRestoredEvent(enemyStack.Id, 6),
            new WorldTurnStartedEvent(humanPlayer.Id),
            new WorldTurnResourcesGeneratedEvent(humanPlayer.Id, new WorldResourceSet(100, 0, 0, 0, 25, 0)),
            new WorldAiActionEndedEvent(neutralPlayer.Id, memory)
        ]);
    }

    [Test]
    public void AttackAlliedTargetTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 1);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 3);

        WorldPlayer aiPlayer = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            IsAi = true,
            Resources = WorldResourceSet.Empty
        };
        WorldPlayer alliedPlayer = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Human, map);
        WorldPlayer enemyPlayer = TestHelpers.SimplePlayer(new WorldPlayerId(3), WorldRace.Heretic, map);

        WorldRelation alliance = new(
            new WorldRelationId(WorldRace.Undead, WorldRace.Human),
            Current: 100,
            Previous: 100,
            Alliance: true,
            AllianceTurn: 1,
            PermanentWar: false,
            PermanentAlliance: false);

        WorldStack targetStack = TestHelpers.SimpleStack(
            new WorldStackId("Target"),
            alliedPlayer.Id,
            new WorldPosition(3, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 0);

        WorldStack enemyStack = TestHelpers.SimpleStack(
            new WorldStackId("Enemy"),
            enemyPlayer.Id,
            new WorldPosition(4, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader2"), leaderDefinition), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 0);

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Stack1"),
                aiPlayer.Id,
                new WorldPosition(0, 0),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader3"), leaderDefinition), GridPosition.FrontBottom, false)
                    .Build(),
                movementPoints: 6) with
            {
                Order = new WorldStackOrder.AttackStack(targetStack.Id)
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(TestHelpers.SimpleLord(aiPlayer.LordId, aiPlayer.Race, LordCategory.Warrior))
            .WithPlayer(aiPlayer)
            .WithDefinition(TestHelpers.SimpleLord(alliedPlayer.LordId, alliedPlayer.Race, LordCategory.Warrior))
            .WithPlayer(alliedPlayer)
            .WithDefinition(TestHelpers.SimpleLord(enemyPlayer.LordId, enemyPlayer.Race, LordCategory.Warrior))
            .WithPlayer(enemyPlayer)
            .WithRelation(alliance)
            .WithStack(targetStack)
            .WithStack(enemyStack)
            .WithStack(stack)
            .Build();

        WorldAiMemory memory = new(new WorldAiTurnMemory(1, aiPlayer.Id, 1, 0, 0, []));
        WorldAiActionPlan plan = new(
            new WorldAiActionScore(1, 666),
            new WorldAiMoveStackCommand(stack.Id, new WorldPosition(2, 0)),
            memory);

        engine.CheckCommand(new WorldAiActionCommand(), [
            new WorldAiActionStartedEvent(plan),
            new WorldStackMovingEvent(stack.Id, new WorldPosition(0, 0), new WorldPosition(1, 0), WorldStackDirection.SouthEast, 4, [new WorldPosition(2, 0)], null),
            new WorldStackMovedEvent(stack.Id, new WorldPosition(0, 0), new WorldPosition(1, 0), WorldStackDirection.SouthEast, 4, [new WorldPosition(2, 0)], null),
            new WorldStackMovingEvent(stack.Id, new WorldPosition(1, 0), new WorldPosition(2, 0), WorldStackDirection.SouthEast, 2, [], null),
            new WorldStackMovedEvent(stack.Id, new WorldPosition(1, 0), new WorldPosition(2, 0), WorldStackDirection.SouthEast, 2, [], null),
            new WorldAiActionEndedEvent(aiPlayer.Id, memory)
        ]);
    }

    [Test]
    public void AttackOwnTargetTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 1);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 3);

        WorldPlayer neutralPlayer = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Neutral, map) with
        {
            IsAi = true,
            Resources = WorldResourceSet.Empty
        };
        WorldPlayer humanPlayer = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack targetStack = TestHelpers.SimpleStack(
                new WorldStackId("Target"),
                neutralPlayer.Id,
                new WorldPosition(3, 0),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom, false)
                    .Build(),
                movementPoints: 0) with
            {
                Order = WorldStackOrder.Stand.Instance
            };

        WorldStack enemyStack = TestHelpers.SimpleStack(
            new WorldStackId("Enemy"),
            humanPlayer.Id,
            new WorldPosition(4, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader2"), leaderDefinition), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 0);

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Stack1"),
                neutralPlayer.Id,
                new WorldPosition(0, 0),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader3"), leaderDefinition), GridPosition.FrontBottom, false)
                    .Build(),
                movementPoints: 6) with
            {
                Order = new WorldStackOrder.AttackStack(targetStack.Id)
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(TestHelpers.SimpleLord(neutralPlayer.LordId, neutralPlayer.Race, LordCategory.Warrior))
            .WithPlayer(neutralPlayer)
            .WithDefinition(TestHelpers.SimpleLord(humanPlayer.LordId, humanPlayer.Race, LordCategory.Warrior))
            .WithPlayer(humanPlayer)
            .WithStack(targetStack)
            .WithStack(enemyStack)
            .WithStack(stack)
            .Build();

        WorldAiMemory memory = new(new WorldAiTurnMemory(1, neutralPlayer.Id, 1, 0, 0, []));
        WorldAiActionPlan plan = new(
            new WorldAiActionScore(1, 666),
            new WorldAiMoveStackCommand(stack.Id, new WorldPosition(2, 0)),
            memory);

        engine.CheckCommand(new WorldAiActionCommand(), [
            new WorldAiActionStartedEvent(plan),
            new WorldStackMovingEvent(stack.Id, new WorldPosition(0, 0), new WorldPosition(1, 0), WorldStackDirection.SouthEast, 4, [new WorldPosition(2, 0)], null),
            new WorldStackMovedEvent(stack.Id, new WorldPosition(0, 0), new WorldPosition(1, 0), WorldStackDirection.SouthEast, 4, [new WorldPosition(2, 0)], null),
            new WorldStackMovingEvent(stack.Id, new WorldPosition(1, 0), new WorldPosition(2, 0), WorldStackDirection.SouthEast, 2, [], null),
            new WorldStackMovedEvent(stack.Id, new WorldPosition(1, 0), new WorldPosition(2, 0), WorldStackDirection.SouthEast, 2, [], null),
            new WorldAiActionEndedEvent(neutralPlayer.Id, memory)
        ]);
    }
}
