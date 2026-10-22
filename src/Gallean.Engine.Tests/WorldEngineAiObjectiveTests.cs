using Gallean.Common;

namespace Gallean.Engine.Tests;

public sealed class WorldEngineAiObjectiveTests
{
    [Test]
    public void BeforeObjectiveTurnTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "...........",
            "..........."
        ]);

        UnitDefinition mover = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("mover", maxHitpoints: 10, initiative: 40, damage: 5),
            movementPoints: 4, leadership: 6);
        UnitDefinition enemy = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 5, initiative: 1, damage: 1),
            movementPoints: 6, leadership: 6);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with { IsAi = true };
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack moverStack = TestHelpers.SimpleStack(
            new WorldStackId("Mover"),
            player1.Id,
            new WorldPosition(5, 1),
            new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("mover"), mover), GridPosition.FrontBottom, false).Build(),
            movementPoints: 2);

        WorldStack nearTarget = TestHelpers.SimpleStack(
            new WorldStackId("Alpha"),
            player2.Id,
            new WorldPosition(2, 1),
            new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("enemy"), enemy), GridPosition.FrontBottom, false).Build(),
            movementPoints: 0);

        WorldStack objectiveTarget = TestHelpers.SimpleStack(
            new WorldStackId("Beta"),
            player2.Id,
            new WorldPosition(10, 1),
            new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("enemy2"), enemy), GridPosition.FrontBottom, false).Build(),
            movementPoints: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithTurnInfo(new WorldTurnInfo(49, player1.Id))
            .WithDifficulty(WorldDifficulty.Average)
            .WithDefinition(mover)
            .WithDefinition(enemy)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(moverStack)
            .WithStack(nearTarget)
            .WithStack(objectiveTarget)
            .WithScenarioEvent(new ScenarioEventDefinition(
                new ScenarioEventId("Victory"),
                [WorldRace.Undead],
                [WorldRace.Undead],
                [new ScenarioEventDestroyingStackCondition(new ScenarioStackReference.Stack(new WorldStackId("Beta")))],
                [new ScenarioEventWinEffect(new WorldPlayerId(1))],
                Enabled: true,
                OccurOnce: true,
                Chance: 100,
                Order: 0))
            .Build();

        WorldAiMemory memory = new WorldAiMemory(
                new WorldAiTurnMemory(49, player1.Id, 1, 0, 0, []))
            .WithStackMemory(moverStack.Id, new WorldAiStackMemory(WorldStackOrder.Normal.Instance, null));

        WorldAiActionPlan plan = new(
            new WorldAiActionScore(1, 1000),
            new WorldAiMoveStackCommand(moverStack.Id, new WorldPosition(4, 1)),
            memory);

        engine.CheckCommand(new WorldAiActionCommand(), [
            new WorldAiActionStartedEvent(plan),
            new WorldStackMovingEvent(moverStack.Id, new WorldPosition(5, 1), new WorldPosition(4, 1), WorldStackDirection.NorthWest, 0, [], null),
            new WorldStackMovedEvent(moverStack.Id, new WorldPosition(5, 1), new WorldPosition(4, 1), WorldStackDirection.NorthWest, 0, [], null),
            new WorldAiActionEndedEvent(player1.Id, memory)
        ]);
    }

    [Test]
    public void ObjectiveTurnTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "...........",
            "..........."
        ]);

        UnitDefinition mover = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("mover", maxHitpoints: 10, initiative: 40, damage: 5),
            movementPoints: 4, leadership: 6);
        UnitDefinition enemy = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 5, initiative: 1, damage: 1),
            movementPoints: 6, leadership: 6);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with { IsAi = true };
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack moverStack = TestHelpers.SimpleStack(
            new WorldStackId("Mover"),
            player1.Id,
            new WorldPosition(5, 1),
            new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("mover"), mover), GridPosition.FrontBottom, false).Build(),
            movementPoints: 2);

        WorldStack nearTarget = TestHelpers.SimpleStack(
            new WorldStackId("Alpha"),
            player2.Id,
            new WorldPosition(2, 1),
            new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("enemy"), enemy), GridPosition.FrontBottom, false).Build(),
            movementPoints: 0);

        WorldStack objectiveTarget = TestHelpers.SimpleStack(
            new WorldStackId("Beta"),
            player2.Id,
            new WorldPosition(10, 1),
            new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("enemy2"), enemy), GridPosition.FrontBottom, false).Build(),
            movementPoints: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithTurnInfo(new WorldTurnInfo(50, player1.Id))
            .WithDifficulty(WorldDifficulty.Average)
            .WithDefinition(mover)
            .WithDefinition(enemy)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(moverStack)
            .WithStack(nearTarget)
            .WithStack(objectiveTarget)
            .WithScenarioEvent(new ScenarioEventDefinition(
                new ScenarioEventId("Victory"),
                [WorldRace.Undead],
                [WorldRace.Undead],
                [new ScenarioEventDestroyingStackCondition(new ScenarioStackReference.Stack(new WorldStackId("Beta")))],
                [new ScenarioEventWinEffect(new WorldPlayerId(1))],
                Enabled: true,
                OccurOnce: true,
                Chance: 100,
                Order: 0))
            .Build();

        WorldAiMemory memory = new WorldAiMemory(
                new WorldAiTurnMemory(50, player1.Id, 1, 0, 0, []))
            .WithStackMemory(moverStack.Id, new WorldAiStackMemory(WorldStackOrder.Normal.Instance, null));

        WorldAiActionPlan plan = new(
            new WorldAiActionScore(1, 1000),
            new WorldAiMoveStackCommand(moverStack.Id, new WorldPosition(6, 1)),
            memory);

        engine.CheckCommand(new WorldAiActionCommand(), [
            new WorldAiActionStartedEvent(plan),
            new WorldStackMovingEvent(moverStack.Id, new WorldPosition(5, 1), new WorldPosition(6, 1), WorldStackDirection.SouthEast, 0, [], null),
            new WorldStackMovedEvent(moverStack.Id, new WorldPosition(5, 1), new WorldPosition(6, 1), WorldStackDirection.SouthEast, 0, [], null),
            new WorldAiActionEndedEvent(player1.Id, memory)
        ]);
    }

    [Test]
    public void EasyBeforeObjectiveTurnTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "...........",
            "..........."
        ]);

        UnitDefinition mover = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("mover", maxHitpoints: 10, initiative: 40, damage: 5),
            movementPoints: 4, leadership: 6);
        UnitDefinition enemy = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 5, initiative: 1, damage: 1),
            movementPoints: 6, leadership: 6);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with { IsAi = true };
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack moverStack = TestHelpers.SimpleStack(
            new WorldStackId("Mover"),
            player1.Id,
            new WorldPosition(5, 1),
            new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("mover"), mover), GridPosition.FrontBottom, false).Build(),
            movementPoints: 2);

        WorldStack nearTarget = TestHelpers.SimpleStack(
            new WorldStackId("Alpha"),
            player2.Id,
            new WorldPosition(2, 1),
            new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("enemy"), enemy), GridPosition.FrontBottom, false).Build(),
            movementPoints: 0);

        WorldStack objectiveTarget = TestHelpers.SimpleStack(
            new WorldStackId("Beta"),
            player2.Id,
            new WorldPosition(10, 1),
            new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("enemy2"), enemy), GridPosition.FrontBottom, false).Build(),
            movementPoints: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithTurnInfo(new WorldTurnInfo(59, player1.Id))
            .WithDifficulty(WorldDifficulty.Easy)
            .WithDefinition(mover)
            .WithDefinition(enemy)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(moverStack)
            .WithStack(nearTarget)
            .WithStack(objectiveTarget)
            .WithScenarioEvent(new ScenarioEventDefinition(
                new ScenarioEventId("Victory"),
                [WorldRace.Undead],
                [WorldRace.Undead],
                [new ScenarioEventDestroyingStackCondition(new ScenarioStackReference.Stack(new WorldStackId("Beta")))],
                [new ScenarioEventWinEffect(new WorldPlayerId(1))],
                Enabled: true,
                OccurOnce: true,
                Chance: 100,
                Order: 0))
            .Build();

        WorldAiMemory memory = new WorldAiMemory(
                new WorldAiTurnMemory(59, player1.Id, 1, 0, 0, []))
            .WithStackMemory(moverStack.Id, new WorldAiStackMemory(WorldStackOrder.Normal.Instance, null));

        WorldAiActionPlan plan = new(
            new WorldAiActionScore(1, 1000),
            new WorldAiMoveStackCommand(moverStack.Id, new WorldPosition(4, 1)),
            memory);

        engine.CheckCommand(new WorldAiActionCommand(), [
            new WorldAiActionStartedEvent(plan),
            new WorldStackMovingEvent(moverStack.Id, new WorldPosition(5, 1), new WorldPosition(4, 1), WorldStackDirection.NorthWest, 0, [], null),
            new WorldStackMovedEvent(moverStack.Id, new WorldPosition(5, 1), new WorldPosition(4, 1), WorldStackDirection.NorthWest, 0, [], null),
            new WorldAiActionEndedEvent(player1.Id, memory)
        ]);
    }

    [Test]
    public void EasyObjectiveTurnTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "...........",
            "..........."
        ]);

        UnitDefinition mover = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("mover", maxHitpoints: 10, initiative: 40, damage: 5),
            movementPoints: 4, leadership: 6);
        UnitDefinition enemy = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 5, initiative: 1, damage: 1),
            movementPoints: 6, leadership: 6);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with { IsAi = true };
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack moverStack = TestHelpers.SimpleStack(
            new WorldStackId("Mover"),
            player1.Id,
            new WorldPosition(5, 1),
            new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("mover"), mover), GridPosition.FrontBottom, false).Build(),
            movementPoints: 2);

        WorldStack nearTarget = TestHelpers.SimpleStack(
            new WorldStackId("Alpha"),
            player2.Id,
            new WorldPosition(2, 1),
            new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("enemy"), enemy), GridPosition.FrontBottom, false).Build(),
            movementPoints: 0);

        WorldStack objectiveTarget = TestHelpers.SimpleStack(
            new WorldStackId("Beta"),
            player2.Id,
            new WorldPosition(10, 1),
            new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("enemy2"), enemy), GridPosition.FrontBottom, false).Build(),
            movementPoints: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithTurnInfo(new WorldTurnInfo(60, player1.Id))
            .WithDifficulty(WorldDifficulty.Easy)
            .WithDefinition(mover)
            .WithDefinition(enemy)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(moverStack)
            .WithStack(nearTarget)
            .WithStack(objectiveTarget)
            .WithScenarioEvent(new ScenarioEventDefinition(
                new ScenarioEventId("Victory"),
                [WorldRace.Undead],
                [WorldRace.Undead],
                [new ScenarioEventDestroyingStackCondition(new ScenarioStackReference.Stack(new WorldStackId("Beta")))],
                [new ScenarioEventWinEffect(new WorldPlayerId(1))],
                Enabled: true,
                OccurOnce: true,
                Chance: 100,
                Order: 0))
            .Build();

        WorldAiMemory memory = new WorldAiMemory(
                new WorldAiTurnMemory(60, player1.Id, 1, 0, 0, []))
            .WithStackMemory(moverStack.Id, new WorldAiStackMemory(WorldStackOrder.Normal.Instance, null));

        WorldAiActionPlan plan = new(
            new WorldAiActionScore(1, 1000),
            new WorldAiMoveStackCommand(moverStack.Id, new WorldPosition(6, 1)),
            memory);

        engine.CheckCommand(new WorldAiActionCommand(), [
            new WorldAiActionStartedEvent(plan),
            new WorldStackMovingEvent(moverStack.Id, new WorldPosition(5, 1), new WorldPosition(6, 1), WorldStackDirection.SouthEast, 0, [], null),
            new WorldStackMovedEvent(moverStack.Id, new WorldPosition(5, 1), new WorldPosition(6, 1), WorldStackDirection.SouthEast, 0, [], null),
            new WorldAiActionEndedEvent(player1.Id, memory)
        ]);
    }

    [Test]
    public void HardObjectiveTurnTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "...........",
            "..........."
        ]);

        UnitDefinition mover = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("mover", maxHitpoints: 10, initiative: 40, damage: 5),
            movementPoints: 4, leadership: 6);
        UnitDefinition enemy = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 5, initiative: 1, damage: 1),
            movementPoints: 6, leadership: 6);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with { IsAi = true };
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack moverStack = TestHelpers.SimpleStack(
            new WorldStackId("Mover"),
            player1.Id,
            new WorldPosition(5, 1),
            new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("mover"), mover), GridPosition.FrontBottom, false).Build(),
            movementPoints: 2);

        WorldStack nearTarget = TestHelpers.SimpleStack(
            new WorldStackId("Alpha"),
            player2.Id,
            new WorldPosition(2, 1),
            new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("enemy"), enemy), GridPosition.FrontBottom, false).Build(),
            movementPoints: 0);

        WorldStack objectiveTarget = TestHelpers.SimpleStack(
            new WorldStackId("Beta"),
            player2.Id,
            new WorldPosition(10, 1),
            new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("enemy2"), enemy), GridPosition.FrontBottom, false).Build(),
            movementPoints: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithTurnInfo(new WorldTurnInfo(40, player1.Id))
            .WithDifficulty(WorldDifficulty.Hard)
            .WithDefinition(mover)
            .WithDefinition(enemy)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(moverStack)
            .WithStack(nearTarget)
            .WithStack(objectiveTarget)
            .WithScenarioEvent(new ScenarioEventDefinition(
                new ScenarioEventId("Victory"),
                [WorldRace.Undead],
                [WorldRace.Undead],
                [new ScenarioEventDestroyingStackCondition(new ScenarioStackReference.Stack(new WorldStackId("Beta")))],
                [new ScenarioEventWinEffect(new WorldPlayerId(1))],
                Enabled: true,
                OccurOnce: true,
                Chance: 100,
                Order: 0))
            .Build();

        WorldAiMemory memory = new WorldAiMemory(
                new WorldAiTurnMemory(40, player1.Id, 1, 0, 0, []))
            .WithStackMemory(moverStack.Id, new WorldAiStackMemory(WorldStackOrder.Normal.Instance, null));

        WorldAiActionPlan plan = new(
            new WorldAiActionScore(1, 1000),
            new WorldAiMoveStackCommand(moverStack.Id, new WorldPosition(6, 1)),
            memory);

        engine.CheckCommand(new WorldAiActionCommand(), [
            new WorldAiActionStartedEvent(plan),
            new WorldStackMovingEvent(moverStack.Id, new WorldPosition(5, 1), new WorldPosition(6, 1), WorldStackDirection.SouthEast, 0, [], null),
            new WorldStackMovedEvent(moverStack.Id, new WorldPosition(5, 1), new WorldPosition(6, 1), WorldStackDirection.SouthEast, 0, [], null),
            new WorldAiActionEndedEvent(player1.Id, memory)
        ]);
    }

    [Test]
    public void VeryHardBeforeObjectiveTurnTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "...........",
            "..........."
        ]);

        UnitDefinition mover = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("mover", maxHitpoints: 10, initiative: 40, damage: 5),
            movementPoints: 4, leadership: 6);
        UnitDefinition enemy = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 5, initiative: 1, damage: 1),
            movementPoints: 6, leadership: 6);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with { IsAi = true };
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack moverStack = TestHelpers.SimpleStack(
            new WorldStackId("Mover"),
            player1.Id,
            new WorldPosition(5, 1),
            new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("mover"), mover), GridPosition.FrontBottom, false).Build(),
            movementPoints: 2);

        WorldStack nearTarget = TestHelpers.SimpleStack(
            new WorldStackId("Alpha"),
            player2.Id,
            new WorldPosition(2, 1),
            new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("enemy"), enemy), GridPosition.FrontBottom, false).Build(),
            movementPoints: 0);

        WorldStack objectiveTarget = TestHelpers.SimpleStack(
            new WorldStackId("Beta"),
            player2.Id,
            new WorldPosition(10, 1),
            new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("enemy2"), enemy), GridPosition.FrontBottom, false).Build(),
            movementPoints: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithTurnInfo(new WorldTurnInfo(29, player1.Id))
            .WithDifficulty(WorldDifficulty.VeryHard)
            .WithDefinition(mover)
            .WithDefinition(enemy)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(moverStack)
            .WithStack(nearTarget)
            .WithStack(objectiveTarget)
            .WithScenarioEvent(new ScenarioEventDefinition(
                new ScenarioEventId("Victory"),
                [WorldRace.Undead],
                [WorldRace.Undead],
                [new ScenarioEventDestroyingStackCondition(new ScenarioStackReference.Stack(new WorldStackId("Beta")))],
                [new ScenarioEventWinEffect(new WorldPlayerId(1))],
                Enabled: true,
                OccurOnce: true,
                Chance: 100,
                Order: 0))
            .Build();

        WorldAiMemory memory = new WorldAiMemory(
                new WorldAiTurnMemory(29, player1.Id, 1, 0, 0, []))
            .WithStackMemory(moverStack.Id, new WorldAiStackMemory(WorldStackOrder.Normal.Instance, null));

        WorldAiActionPlan plan = new(
            new WorldAiActionScore(1, 1000),
            new WorldAiMoveStackCommand(moverStack.Id, new WorldPosition(4, 1)),
            memory);

        engine.CheckCommand(new WorldAiActionCommand(), [
            new WorldAiActionStartedEvent(plan),
            new WorldStackMovingEvent(moverStack.Id, new WorldPosition(5, 1), new WorldPosition(4, 1), WorldStackDirection.NorthWest, 0, [], null),
            new WorldStackMovedEvent(moverStack.Id, new WorldPosition(5, 1), new WorldPosition(4, 1), WorldStackDirection.NorthWest, 0, [], null),
            new WorldAiActionEndedEvent(player1.Id, memory)
        ]);
    }

    [Test]
    public void VeryHardObjectiveTurnTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "...........",
            "..........."
        ]);

        UnitDefinition mover = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("mover", maxHitpoints: 10, initiative: 40, damage: 5),
            movementPoints: 4, leadership: 6);
        UnitDefinition enemy = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 5, initiative: 1, damage: 1),
            movementPoints: 6, leadership: 6);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with { IsAi = true };
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack moverStack = TestHelpers.SimpleStack(
            new WorldStackId("Mover"),
            player1.Id,
            new WorldPosition(5, 1),
            new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("mover"), mover), GridPosition.FrontBottom, false).Build(),
            movementPoints: 2);

        WorldStack nearTarget = TestHelpers.SimpleStack(
            new WorldStackId("Alpha"),
            player2.Id,
            new WorldPosition(2, 1),
            new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("enemy"), enemy), GridPosition.FrontBottom, false).Build(),
            movementPoints: 0);

        WorldStack objectiveTarget = TestHelpers.SimpleStack(
            new WorldStackId("Beta"),
            player2.Id,
            new WorldPosition(10, 1),
            new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("enemy2"), enemy), GridPosition.FrontBottom, false).Build(),
            movementPoints: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithTurnInfo(new WorldTurnInfo(30, player1.Id))
            .WithDifficulty(WorldDifficulty.VeryHard)
            .WithDefinition(mover)
            .WithDefinition(enemy)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(moverStack)
            .WithStack(nearTarget)
            .WithStack(objectiveTarget)
            .WithScenarioEvent(new ScenarioEventDefinition(
                new ScenarioEventId("Victory"),
                [WorldRace.Undead],
                [WorldRace.Undead],
                [new ScenarioEventDestroyingStackCondition(new ScenarioStackReference.Stack(new WorldStackId("Beta")))],
                [new ScenarioEventWinEffect(new WorldPlayerId(1))],
                Enabled: true,
                OccurOnce: true,
                Chance: 100,
                Order: 0))
            .Build();

        WorldAiMemory memory = new WorldAiMemory(
                new WorldAiTurnMemory(30, player1.Id, 1, 0, 0, []))
            .WithStackMemory(moverStack.Id, new WorldAiStackMemory(WorldStackOrder.Normal.Instance, null));

        WorldAiActionPlan plan = new(
            new WorldAiActionScore(1, 1000),
            new WorldAiMoveStackCommand(moverStack.Id, new WorldPosition(6, 1)),
            memory);

        engine.CheckCommand(new WorldAiActionCommand(), [
            new WorldAiActionStartedEvent(plan),
            new WorldStackMovingEvent(moverStack.Id, new WorldPosition(5, 1), new WorldPosition(6, 1), WorldStackDirection.SouthEast, 0, [], null),
            new WorldStackMovedEvent(moverStack.Id, new WorldPosition(5, 1), new WorldPosition(6, 1), WorldStackDirection.SouthEast, 0, [], null),
            new WorldAiActionEndedEvent(player1.Id, memory)
        ]);
    }

    [Test]
    public void RivalVictoryTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "...........",
            "..........."
        ]);

        UnitDefinition mover = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("mover", maxHitpoints: 10, initiative: 40, damage: 5),
            movementPoints: 4, leadership: 6);
        UnitDefinition enemy = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 5, initiative: 1, damage: 1),
            movementPoints: 6, leadership: 6);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with { IsAi = true };
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack moverStack = TestHelpers.SimpleStack(
            new WorldStackId("Mover"),
            player1.Id,
            new WorldPosition(5, 1),
            new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("mover"), mover), GridPosition.FrontBottom, false).Build(),
            movementPoints: 2);

        WorldStack nearTarget = TestHelpers.SimpleStack(
            new WorldStackId("Alpha"),
            player2.Id,
            new WorldPosition(2, 1),
            new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("enemy"), enemy), GridPosition.FrontBottom, false).Build(),
            movementPoints: 0);

        WorldStack objectiveTarget = TestHelpers.SimpleStack(
            new WorldStackId("Beta"),
            player2.Id,
            new WorldPosition(10, 1),
            new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("enemy2"), enemy), GridPosition.FrontBottom, false).Build(),
            movementPoints: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithTurnInfo(new WorldTurnInfo(50, player1.Id))
            .WithDifficulty(WorldDifficulty.Average)
            .WithDefinition(mover)
            .WithDefinition(enemy)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(moverStack)
            .WithStack(nearTarget)
            .WithStack(objectiveTarget)
            .WithScenarioEvent(new ScenarioEventDefinition(
                new ScenarioEventId("Victory"),
                [WorldRace.Undead, WorldRace.Heretic],
                [WorldRace.Undead, WorldRace.Heretic],
                [new ScenarioEventDestroyingStackCondition(new ScenarioStackReference.Stack(new WorldStackId("Beta")))],
                [new ScenarioEventWinEffect(new WorldPlayerId(2))],
                Enabled: true,
                OccurOnce: true,
                Chance: 100,
                Order: 0))
            .Build();

        WorldAiMemory memory = new WorldAiMemory(
                new WorldAiTurnMemory(50, player1.Id, 1, 0, 0, []))
            .WithStackMemory(moverStack.Id, new WorldAiStackMemory(WorldStackOrder.Normal.Instance, null));

        WorldAiActionPlan plan = new(
            new WorldAiActionScore(1, 1000),
            new WorldAiMoveStackCommand(moverStack.Id, new WorldPosition(4, 1)),
            memory);

        engine.CheckCommand(new WorldAiActionCommand(), [
            new WorldAiActionStartedEvent(plan),
            new WorldStackMovingEvent(moverStack.Id, new WorldPosition(5, 1), new WorldPosition(4, 1), WorldStackDirection.NorthWest, 0, [], null),
            new WorldStackMovedEvent(moverStack.Id, new WorldPosition(5, 1), new WorldPosition(4, 1), WorldStackDirection.NorthWest, 0, [], null),
            new WorldAiActionEndedEvent(player1.Id, memory)
        ]);
    }

    [Test]
    public void LossEventTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "...........",
            "..........."
        ]);

        UnitDefinition mover = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("mover", maxHitpoints: 10, initiative: 40, damage: 5),
            movementPoints: 4, leadership: 6);
        UnitDefinition enemy = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 5, initiative: 1, damage: 1),
            movementPoints: 6, leadership: 6);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with { IsAi = true };
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack moverStack = TestHelpers.SimpleStack(
            new WorldStackId("Mover"),
            player1.Id,
            new WorldPosition(5, 1),
            new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("mover"), mover), GridPosition.FrontBottom, false).Build(),
            movementPoints: 2);

        WorldStack nearTarget = TestHelpers.SimpleStack(
            new WorldStackId("Alpha"),
            player2.Id,
            new WorldPosition(2, 1),
            new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("enemy"), enemy), GridPosition.FrontBottom, false).Build(),
            movementPoints: 0);

        WorldStack objectiveTarget = TestHelpers.SimpleStack(
            new WorldStackId("Beta"),
            player2.Id,
            new WorldPosition(10, 1),
            new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("enemy2"), enemy), GridPosition.FrontBottom, false).Build(),
            movementPoints: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithTurnInfo(new WorldTurnInfo(50, player1.Id))
            .WithDifficulty(WorldDifficulty.Average)
            .WithDefinition(mover)
            .WithDefinition(enemy)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(moverStack)
            .WithStack(nearTarget)
            .WithStack(objectiveTarget)
            .WithScenarioEvent(new ScenarioEventDefinition(
                new ScenarioEventId("Defeat"),
                [WorldRace.Undead],
                [WorldRace.Undead],
                [new ScenarioEventDestroyingStackCondition(new ScenarioStackReference.Stack(new WorldStackId("Beta")))],
                [new ScenarioEventWinEffect(WinnerId: null)],
                Enabled: true,
                OccurOnce: true,
                Chance: 100,
                Order: 0))
            .Build();

        WorldAiMemory memory = new WorldAiMemory(
                new WorldAiTurnMemory(50, player1.Id, 1, 0, 0, []))
            .WithStackMemory(moverStack.Id, new WorldAiStackMemory(WorldStackOrder.Normal.Instance, null));

        WorldAiActionPlan plan = new(
            new WorldAiActionScore(1, 1000),
            new WorldAiMoveStackCommand(moverStack.Id, new WorldPosition(6, 1)),
            memory);

        engine.CheckCommand(new WorldAiActionCommand(), [
            new WorldAiActionStartedEvent(plan),
            new WorldStackMovingEvent(moverStack.Id, new WorldPosition(5, 1), new WorldPosition(6, 1), WorldStackDirection.SouthEast, 0, [], null),
            new WorldStackMovedEvent(moverStack.Id, new WorldPosition(5, 1), new WorldPosition(6, 1), WorldStackDirection.SouthEast, 0, [], null),
            new WorldAiActionEndedEvent(player1.Id, memory)
        ]);
    }

    [Test]
    public void LossEventWithVictoryTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "...........",
            "..........."
        ]);

        UnitDefinition mover = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("mover", maxHitpoints: 10, initiative: 40, damage: 5),
            movementPoints: 4, leadership: 6);
        UnitDefinition enemy = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 5, initiative: 1, damage: 1),
            movementPoints: 6, leadership: 6);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with { IsAi = true };
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack moverStack = TestHelpers.SimpleStack(
            new WorldStackId("Mover"),
            player1.Id,
            new WorldPosition(5, 1),
            new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("mover"), mover), GridPosition.FrontBottom, false).Build(),
            movementPoints: 2);

        WorldStack nearTarget = TestHelpers.SimpleStack(
            new WorldStackId("Alpha"),
            player2.Id,
            new WorldPosition(2, 1),
            new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("enemy"), enemy), GridPosition.FrontBottom, false).Build(),
            movementPoints: 0);

        WorldStack objectiveTarget = TestHelpers.SimpleStack(
            new WorldStackId("Beta"),
            player2.Id,
            new WorldPosition(10, 1),
            new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("enemy2"), enemy), GridPosition.FrontBottom, false).Build(),
            movementPoints: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithTurnInfo(new WorldTurnInfo(50, player1.Id))
            .WithDifficulty(WorldDifficulty.Average)
            .WithDefinition(mover)
            .WithDefinition(enemy)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(moverStack)
            .WithStack(nearTarget)
            .WithStack(objectiveTarget)
            .WithScenarioEvent(new ScenarioEventDefinition(
                new ScenarioEventId("Victory"),
                [WorldRace.Undead],
                [WorldRace.Undead],
                [new ScenarioEventFrequencyCondition(1)],
                [new ScenarioEventWinEffect(new WorldPlayerId(1))],
                Enabled: true,
                OccurOnce: true,
                Chance: 100,
                Order: 0))
            .WithScenarioEvent(new ScenarioEventDefinition(
                new ScenarioEventId("Defeat"),
                [WorldRace.Undead],
                [WorldRace.Undead],
                [new ScenarioEventDestroyingStackCondition(new ScenarioStackReference.Stack(new WorldStackId("Beta")))],
                [new ScenarioEventWinEffect(WinnerId: null)],
                Enabled: true,
                OccurOnce: true,
                Chance: 100,
                Order: 0))
            .Build();

        WorldAiMemory memory = new WorldAiMemory(
                new WorldAiTurnMemory(50, player1.Id, 1, 0, 0, []))
            .WithStackMemory(moverStack.Id, new WorldAiStackMemory(WorldStackOrder.Normal.Instance, null));

        WorldAiActionPlan plan = new(
            new WorldAiActionScore(1, 1000),
            new WorldAiMoveStackCommand(moverStack.Id, new WorldPosition(4, 1)),
            memory);

        engine.CheckCommand(new WorldAiActionCommand(), [
            new WorldAiActionStartedEvent(plan),
            new WorldStackMovingEvent(moverStack.Id, new WorldPosition(5, 1), new WorldPosition(4, 1), WorldStackDirection.NorthWest, 0, [], null),
            new WorldStackMovedEvent(moverStack.Id, new WorldPosition(5, 1), new WorldPosition(4, 1), WorldStackDirection.NorthWest, 0, [], null),
            new WorldAiActionEndedEvent(player1.Id, memory)
        ]);
    }

    [Test]
    public void OwningItemBeforeObjectiveTurnTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "...............",
            "...............",
            "...............",
            "...............",
            "..............."
        ]);

        UnitDefinition mover = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("mover", maxHitpoints: 10, initiative: 40, damage: 5),
            movementPoints: 4, leadership: 6);
        UnitDefinition enemy = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 5, initiative: 1, damage: 1),
            movementPoints: 6, leadership: 6);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with { IsAi = true };
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack moverStack = TestHelpers.SimpleStack(
            new WorldStackId("Mover"),
            player1.Id,
            new WorldPosition(6, 4),
            new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("mover"), mover), GridPosition.FrontBottom, false).Build(),
            movementPoints: 2);

        WorldStack nearTarget = TestHelpers.SimpleStack(
            new WorldStackId("Alpha"),
            player2.Id,
            new WorldPosition(3, 4),
            new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("enemy"), enemy), GridPosition.FrontBottom, false).Build(),
            movementPoints: 0);

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Heretic, 1), WorldResourceSet.Empty);

        WorldCity city = TestHelpers.SimpleCity(
                new WorldFortificationId("City"),
                cityDefinition,
                player2.Id,
                new WorldPosition(13, 3),
                UnitGrid<WorldUnit>.Empty) with
            {
                Inventory = new WorldInventory([new WorldItem(new WorldItemId("Relic"), new ItemDefinitionId("Relic"), Charges: null)])
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithTurnInfo(new WorldTurnInfo(49, player1.Id))
            .WithDefinition(mover)
            .WithDefinition(enemy)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(moverStack)
            .WithStack(nearTarget)
            .WithCity(city)
            .WithScenarioEvent(new ScenarioEventDefinition(
                new ScenarioEventId("Victory"),
                [WorldRace.Undead],
                [WorldRace.Undead],
                [new ScenarioEventOwningItemCondition(new ItemDefinitionId("Relic"))],
                [new ScenarioEventWinEffect(new WorldPlayerId(1))],
                Enabled: true,
                OccurOnce: true,
                Chance: 100,
                Order: 0))
            .Build();

        WorldAiMemory memory = new WorldAiMemory(
                new WorldAiTurnMemory(49, player1.Id, 1, 0, 0, []))
            .WithStackMemory(moverStack.Id, new WorldAiStackMemory(WorldStackOrder.Normal.Instance, null));

        WorldAiActionPlan plan = new(
            new WorldAiActionScore(1, 1000),
            new WorldAiMoveStackCommand(moverStack.Id, new WorldPosition(5, 4)),
            memory);

        engine.CheckCommand(new WorldAiActionCommand(), [
            new WorldAiActionStartedEvent(plan),
            new WorldStackMovingEvent(moverStack.Id, new WorldPosition(6, 4), new WorldPosition(5, 4), WorldStackDirection.NorthWest, 0, [], null),
            new WorldStackMovedEvent(moverStack.Id, new WorldPosition(6, 4), new WorldPosition(5, 4), WorldStackDirection.NorthWest, 0, [], null),
            new WorldAiActionEndedEvent(player1.Id, memory)
        ]);
    }

    [Test]
    public void OwningItemTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "...............",
            "...............",
            "...............",
            "...............",
            "..............."
        ]);

        UnitDefinition mover = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("mover", maxHitpoints: 10, initiative: 40, damage: 5),
            movementPoints: 4, leadership: 6);
        UnitDefinition enemy = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 5, initiative: 1, damage: 1),
            movementPoints: 6, leadership: 6);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with { IsAi = true };
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack moverStack = TestHelpers.SimpleStack(
            new WorldStackId("Mover"),
            player1.Id,
            new WorldPosition(6, 4),
            new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("mover"), mover), GridPosition.FrontBottom, false).Build(),
            movementPoints: 2);

        WorldStack nearTarget = TestHelpers.SimpleStack(
            new WorldStackId("Alpha"),
            player2.Id,
            new WorldPosition(3, 4),
            new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("enemy"), enemy), GridPosition.FrontBottom, false).Build(),
            movementPoints: 0);

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Heretic, 1), WorldResourceSet.Empty);

        WorldCity city = TestHelpers.SimpleCity(
                new WorldFortificationId("City"),
                cityDefinition,
                player2.Id,
                new WorldPosition(13, 3),
                UnitGrid<WorldUnit>.Empty) with
            {
                Inventory = new WorldInventory([new WorldItem(new WorldItemId("Relic"), new ItemDefinitionId("Relic"), Charges: null)])
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithTurnInfo(new WorldTurnInfo(50, player1.Id))
            .WithDefinition(mover)
            .WithDefinition(enemy)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(moverStack)
            .WithStack(nearTarget)
            .WithCity(city)
            .WithScenarioEvent(new ScenarioEventDefinition(
                new ScenarioEventId("Victory"),
                [WorldRace.Undead],
                [WorldRace.Undead],
                [new ScenarioEventOwningItemCondition(new ItemDefinitionId("Relic"))],
                [new ScenarioEventWinEffect(new WorldPlayerId(1))],
                Enabled: true,
                OccurOnce: true,
                Chance: 100,
                Order: 0))
            .Build();

        WorldAiMemory memory = new WorldAiMemory(
                new WorldAiTurnMemory(50, player1.Id, 1, 0, 0, []))
            .WithStackMemory(moverStack.Id, new WorldAiStackMemory(WorldStackOrder.Normal.Instance, null));

        WorldAiActionPlan plan = new(
            new WorldAiActionScore(1, 1000),
            new WorldAiMoveStackCommand(moverStack.Id, new WorldPosition(7, 4)),
            memory);

        engine.CheckCommand(new WorldAiActionCommand(), [
            new WorldAiActionStartedEvent(plan),
            new WorldStackMovingEvent(moverStack.Id, new WorldPosition(6, 4), new WorldPosition(7, 4), WorldStackDirection.SouthEast, 0, [], null),
            new WorldStackMovedEvent(moverStack.Id, new WorldPosition(6, 4), new WorldPosition(7, 4), WorldStackDirection.SouthEast, 0, [], null),
            new WorldAiActionEndedEvent(player1.Id, memory)
        ]);
    }

    [Test]
    public void AttackBeforeObjectiveTurnTest()
    {
        WorldMap map = TestHelpers.SimpleMap(9, 1);

        UnitDefinition attacker = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 100, initiative: 50, damage: 50),
            movementPoints: 6, leadership: 6);
        UnitDefinition weakEnemy = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("weak", maxHitpoints: 10, initiative: 40, damage: 5),
            movementPoints: 6, leadership: 6);
        UnitDefinition strongEnemy = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("strong", maxHitpoints: 100, initiative: 40, damage: 30),
            movementPoints: 6, leadership: 6);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack attackerStack = TestHelpers.SimpleStack(
            new WorldStackId("Attacker"),
            player1.Id,
            new WorldPosition(4, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("attacker"), attacker), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 6);

        WorldStack weakTarget = TestHelpers.SimpleStack(
            new WorldStackId("Alpha"),
            player2.Id,
            new WorldPosition(2, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("weakEnemy"), weakEnemy), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 0);

        WorldStack objectiveTarget = TestHelpers.SimpleStack(
            new WorldStackId("Beta"),
            player2.Id,
            new WorldPosition(6, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("strongEnemy"), strongEnemy), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 0);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithTurnInfo(new WorldTurnInfo(49, player1.Id))
            .WithDefinition(attacker)
            .WithDefinition(weakEnemy)
            .WithDefinition(strongEnemy)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(attackerStack)
            .WithStack(weakTarget)
            .WithStack(objectiveTarget)
            .WithScenarioEvent(new ScenarioEventDefinition(
                new ScenarioEventId("Victory"),
                [WorldRace.Undead],
                [WorldRace.Undead],
                [new ScenarioEventDestroyingStackCondition(new ScenarioStackReference.Stack(new WorldStackId("Beta")))],
                [new ScenarioEventWinEffect(new WorldPlayerId(1))],
                Enabled: true,
                OccurOnce: true,
                Chance: 100,
                Order: 0))
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        IReadOnlyList<TestWorldAiRunner.Trace> report = runner.PlayTurn();

        Assert.That(TestWorldAiTraceFormatter.Format(report), Is.EqualTo([
            "[P2 910] Move Attacker -> (2, 0) (Attacker wins)",
            "[P1 1000] Move Attacker -> (4, 0)"
        ]));
    }

    [Test]
    public void AttackObjectiveTest()
    {
        WorldMap map = TestHelpers.SimpleMap(9, 1);

        UnitDefinition attacker = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 100, initiative: 50, damage: 50),
            movementPoints: 6, leadership: 6);
        UnitDefinition weakEnemy = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("weak", maxHitpoints: 10, initiative: 40, damage: 5),
            movementPoints: 6, leadership: 6);
        UnitDefinition strongEnemy = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("strong", maxHitpoints: 100, initiative: 40, damage: 30),
            movementPoints: 6, leadership: 6);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack attackerStack = TestHelpers.SimpleStack(
            new WorldStackId("Attacker"),
            player1.Id,
            new WorldPosition(4, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("attacker"), attacker), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 6);

        WorldStack weakTarget = TestHelpers.SimpleStack(
            new WorldStackId("Alpha"),
            player2.Id,
            new WorldPosition(2, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("weakEnemy"), weakEnemy), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 0);

        WorldStack objectiveTarget = TestHelpers.SimpleStack(
            new WorldStackId("Beta"),
            player2.Id,
            new WorldPosition(6, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("strongEnemy"), strongEnemy), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 0);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithTurnInfo(new WorldTurnInfo(50, player1.Id))
            .WithDefinition(attacker)
            .WithDefinition(weakEnemy)
            .WithDefinition(strongEnemy)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(attackerStack)
            .WithStack(weakTarget)
            .WithStack(objectiveTarget)
            .WithScenarioEvent(new ScenarioEventDefinition(
                new ScenarioEventId("Victory"),
                [WorldRace.Undead],
                [WorldRace.Undead],
                [new ScenarioEventDestroyingStackCondition(new ScenarioStackReference.Stack(new WorldStackId("Beta")))],
                [new ScenarioEventWinEffect(new WorldPlayerId(1))],
                Enabled: true,
                OccurOnce: true,
                Chance: 100,
                Order: 0))
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        IReadOnlyList<TestWorldAiRunner.Trace> report = runner.PlayTurn();

        Assert.That(TestWorldAiTraceFormatter.Format(report), Is.EqualTo([
            "[P2 563] Move Attacker -> (6, 0) (Attacker wins)"
        ]));
    }
}
