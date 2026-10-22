using Gallean.Common;

namespace Gallean.Engine.Tests;

public sealed class WorldEngineAiActionTests
{
    [Test]
    public void PerformsActionTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            ".^^^^",
            "....."
        ]);

        UnitDefinition mover = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("mover", maxHitpoints: 10, initiative: 40, damage: 5),
            movementPoints: 4, leadership: 6);
        UnitDefinition enemy = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 5, initiative: 1, damage: 1),
            movementPoints: 6, leadership: 6);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with { IsAi = true };
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack guardStack = TestHelpers.SimpleStack(
                new WorldStackId("Guard"),
                player1.Id,
                new WorldPosition(0, 0),
                new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("mover"), mover), GridPosition.FrontBottom, false).Build(),
                movementPoints: 4) with
            {
                AiPriority = 4,
                Order = WorldStackOrder.Guard.Instance
            };

        WorldStack moverStack = TestHelpers.SimpleStack(
            new WorldStackId("Mover"),
            player1.Id,
            new WorldPosition(0, 1),
            new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("mover2"), mover), GridPosition.FrontBottom, false).Build(),
            movementPoints: 4);

        WorldStack enemyStack = TestHelpers.SimpleStack(
            new WorldStackId("Enemy"),
            player2.Id,
            new WorldPosition(4, 1),
            new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("enemy"), enemy), GridPosition.FrontBottom, false).Build(),
            movementPoints: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(mover)
            .WithDefinition(enemy)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(guardStack)
            .WithStack(moverStack)
            .WithStack(enemyStack)
            .Build();

        WorldAiMemory memory = new WorldAiMemory(
                new WorldAiTurnMemory(1, player1.Id, 1, 0, 0, [guardStack.Id]))
            .WithStackMemory(moverStack.Id, new WorldAiStackMemory(WorldStackOrder.Normal.Instance, null));
        WorldAiActionPlan plan = new(
            new WorldAiActionScore(1, 1000),
            new WorldAiMoveStackCommand(moverStack.Id, new WorldPosition(2, 1)),
            memory);

        engine.CheckCommand(new WorldAiActionCommand(), [
            new WorldAiActionStartedEvent(plan),
            new WorldStackMovingEvent(moverStack.Id, new WorldPosition(0, 1), new WorldPosition(1, 1), WorldStackDirection.SouthEast, 2, [new WorldPosition(2, 1)], null),
            new WorldStackMovedEvent(moverStack.Id, new WorldPosition(0, 1), new WorldPosition(1, 1), WorldStackDirection.SouthEast, 2, [new WorldPosition(2, 1)], null),
            new WorldStackMovingEvent(moverStack.Id, new WorldPosition(1, 1), new WorldPosition(2, 1), WorldStackDirection.SouthEast, 0, [], null),
            new WorldStackMovedEvent(moverStack.Id, new WorldPosition(1, 1), new WorldPosition(2, 1), WorldStackDirection.SouthEast, 0, [], null),
            new WorldAiActionEndedEvent(player1.Id, memory)
        ]);
    }

    [Test]
    public void NoActionTest()
    {
        WorldMap map = TestHelpers.SimpleMap(1, 1);

        WorldPlayer ai = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with { IsAi = true };
        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(ai.LordId, ai.Race, LordCategory.Warrior))
            .WithPlayer(ai)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .Build();

        WorldAiMemory memory = new(new WorldAiTurnMemory(1, ai.Id));

        engine.CheckCommand(new WorldAiActionCommand(), [
            new WorldAiActionStartedEvent(WorldAiActionPlan.EndTurn(memory)),
            new WorldTurnEndedEvent(ai.Id),
            new WorldRoundEndedEvent(),
            new WorldRoundStartedEvent(),
            new WorldTurnStartingEvent(human.Id),
            new WorldTurnStartedEvent(human.Id),
            new WorldTurnResourcesGeneratedEvent(human.Id, new WorldResourceSet(100, 0, 0, 0, 25, 0)),
            new WorldAiActionEndedEvent(ai.Id, memory)
        ]);
    }

    [Test]
    public void HumanTurnTest()
    {
        WorldMap map = TestHelpers.SimpleMap(1, 1);

        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .Build();

        engine.CheckCommandError(new WorldAiActionCommand(), WorldCommandError.NotAiTurn);
    }

    [Test]
    public void NoHumanPlayersTest()
    {
        WorldMap map = TestHelpers.SimpleMap(1, 1);

        WorldPlayer ai = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with { IsAi = true };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(ai.LordId, ai.Race, LordCategory.Warrior))
            .WithPlayer(ai)
            .Build();

        engine.CheckCommandError(new WorldAiActionCommand(), WorldCommandError.NoPlayersRemaining);
    }

    [Test]
    public void DefeatedHumanTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 5);

        WorldPlayer ai = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with { IsAi = true };
        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);
        WorldCapital capturedCapital = TestHelpers.SimpleCapital(
                new WorldFortificationId("capital"),
                human.Id,
                new WorldPosition(4, 4),
                UnitGrid<WorldUnit>.Empty) with
            {
                CapturedById = ai.Id
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(ai.LordId, ai.Race, LordCategory.Warrior))
            .WithPlayer(ai)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithCapital(capturedCapital)
            .Build();

        engine.CheckCommandError(new WorldAiActionCommand(), WorldCommandError.NoPlayersRemaining);
    }

    [Test]
    public void TargetPriorityTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            ".......",
            "......."
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
            new WorldPosition(3, 1),
            new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("mover"), mover), GridPosition.FrontBottom, false).Build(),
            movementPoints: 2);

        WorldStack westTarget = TestHelpers.SimpleStack(
                new WorldStackId("Alpha"),
                player2.Id,
                new WorldPosition(0, 1),
                new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("enemy"), enemy), GridPosition.FrontBottom, false).Build(),
                movementPoints: 0) with
            {
                AiPriority = 1
            };

        WorldStack eastTarget = TestHelpers.SimpleStack(
                new WorldStackId("Beta"),
                player2.Id,
                new WorldPosition(6, 1),
                new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("enemy2"), enemy), GridPosition.FrontBottom, false).Build(),
                movementPoints: 0) with
            {
                AiPriority = 6
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(mover)
            .WithDefinition(enemy)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(moverStack)
            .WithStack(westTarget)
            .WithStack(eastTarget)
            .Build();

        WorldAiMemory memory = new WorldAiMemory(
                new WorldAiTurnMemory(1, player1.Id, 1, 0, 0, []))
            .WithStackMemory(moverStack.Id, new WorldAiStackMemory(WorldStackOrder.Normal.Instance, null));

        WorldAiActionPlan plan = new(
            new WorldAiActionScore(1, 1000),
            new WorldAiMoveStackCommand(moverStack.Id, new WorldPosition(4, 1)),
            memory);

        engine.CheckCommand(new WorldAiActionCommand(), [
            new WorldAiActionStartedEvent(plan),
            new WorldStackMovingEvent(moverStack.Id, new WorldPosition(3, 1), new WorldPosition(4, 1), WorldStackDirection.SouthEast, 0, [], null),
            new WorldStackMovedEvent(moverStack.Id, new WorldPosition(3, 1), new WorldPosition(4, 1), WorldStackDirection.SouthEast, 0, [], null),
            new WorldAiActionEndedEvent(player1.Id, memory)
        ]);
    }

    [Test]
    public void ZeroTargetPriorityTest()
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
                movementPoints: 0) with
            {
                AiPriority = 0
            };

        WorldStack farTarget = TestHelpers.SimpleStack(
                new WorldStackId("Beta"),
                player2.Id,
                new WorldPosition(10, 1),
                new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("enemy2"), enemy), GridPosition.FrontBottom, false).Build(),
                movementPoints: 0) with
            {
                AiPriority = 3
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(mover)
            .WithDefinition(enemy)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(moverStack)
            .WithStack(nearTarget)
            .WithStack(farTarget)
            .Build();

        WorldAiMemory memory = new WorldAiMemory(
                new WorldAiTurnMemory(1, player1.Id, 1, 0, 0, []))
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
    public void ZeroTargetPriorityOnOrderTargetTest()
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
                movementPoints: 2) with
            {
                Order = new WorldStackOrder.AttackStack(new WorldStackId("Alpha"))
            };

        WorldStack nearTarget = TestHelpers.SimpleStack(
                new WorldStackId("Alpha"),
                player2.Id,
                new WorldPosition(2, 1),
                new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("enemy"), enemy), GridPosition.FrontBottom, false).Build(),
                movementPoints: 0) with
            {
                AiPriority = 0
            };

        WorldStack farTarget = TestHelpers.SimpleStack(
                new WorldStackId("Beta"),
                player2.Id,
                new WorldPosition(10, 1),
                new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("enemy2"), enemy), GridPosition.FrontBottom, false).Build(),
                movementPoints: 0) with
            {
                AiPriority = 3
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(mover)
            .WithDefinition(enemy)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(moverStack)
            .WithStack(nearTarget)
            .WithStack(farTarget)
            .WithAiMemory(
                player1.Id,
                new WorldAiMemory(new WorldAiTurnMemory(1, player1.Id, 0, 0, 0, []))
                    .WithStackMemory(moverStack.Id, new WorldAiStackMemory(WorldStackOrder.Berserk.Instance, null)))
            .Build();

        WorldAiMemory memory = new WorldAiMemory(
                new WorldAiTurnMemory(1, player1.Id, 1, 0, 0, []))
            .WithStackMemory(moverStack.Id, new WorldAiStackMemory(WorldStackOrder.Berserk.Instance, null));

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
    public void ThiefBesideEnemyTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 1);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition enemy = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 5, initiative: 1, damage: 1),
            movementPoints: 6, leadership: 6);

        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);
        WorldPlayer ai = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            IsAi = true,
            SpiedPlayers = [human.Id]
        };

        WorldStack thiefStack = TestHelpers.SimpleStack(
            new WorldStackId("Thief"),
            ai.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("thief"), thief),
            movementPoints: 20);

        WorldStack enemyStack = TestHelpers.SimpleStack(
            new WorldStackId("Enemy"),
            human.Id,
            new WorldPosition(1, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("enemy"), enemy),
            movementPoints: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(enemy)
            .WithDefinition(TestHelpers.SimpleLord(ai.LordId, ai.Race, LordCategory.Warrior))
            .WithPlayer(ai)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithStack(thiefStack)
            .WithStack(enemyStack)
            .Build();

        WorldAiMemory memory = new WorldAiMemory(
                new WorldAiTurnMemory(1, ai.Id, 0, 0, 0, [thiefStack.Id]))
            .WithStackMemory(thiefStack.Id, new WorldAiStackMemory(WorldStackOrder.Explore.Instance, null));

        engine.CheckCommand(new WorldAiActionCommand(), [
            new WorldAiActionStartedEvent(WorldAiActionPlan.EndTurn(memory)),
            new WorldTurnEndedEvent(ai.Id),
            new WorldRoundEndedEvent(),
            new WorldRoundStartedEvent(),
            new WorldTurnStartingEvent(human.Id),
            new WorldStackMovementRestoredEvent(enemyStack.Id, 6),
            new WorldTurnStartedEvent(human.Id),
            new WorldTurnResourcesGeneratedEvent(human.Id, new WorldResourceSet(100, 0, 0, 0, 25, 0)),
            new WorldAiActionEndedEvent(ai.Id, memory)
        ]);
    }

    [Test]
    public void OrderedThiefAttackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 1);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition enemy = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 5, initiative: 1, damage: 1),
            movementPoints: 6, leadership: 6);

        WorldPlayer ai = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with { IsAi = true };
        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack enemyStack = TestHelpers.SimpleStack(
            new WorldStackId("Enemy"),
            human.Id,
            new WorldPosition(1, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("enemy"), enemy),
            movementPoints: 0);

        WorldStack thiefStack = TestHelpers.SimpleStack(
                new WorldStackId("Thief"),
                ai.Id,
                new WorldPosition(0, 0),
                TestHelpers.SingleUnitGrid(new WorldUnitId("thief"), thief),
                movementPoints: 20) with
            {
                Order = new WorldStackOrder.AttackStack(enemyStack.Id)
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(enemy)
            .WithDefinition(TestHelpers.SimpleLord(ai.LordId, ai.Race, LordCategory.Warrior))
            .WithPlayer(ai)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithStack(thiefStack)
            .WithStack(enemyStack)
            .Build();

        BattleState expectedBattleState = new BattleStateBuilder(new TestGameDefinitionProviderBuilder().AddUnit(thief).AddUnit(enemy).Build())
            .Add(TestHelpers.SimpleBattleUnit(new BattleUnitId(0), new WorldUnitId("thief"), thief.Id, BattleSide.Attacker, hitpoints: 100), GridPosition.FrontBottom)
            .Add(TestHelpers.SimpleBattleUnit(new BattleUnitId(1), new WorldUnitId("enemy"), enemy.Id, BattleSide.Defender, hitpoints: 5), GridPosition.FrontBottom)
            .Build(BattleModifiers.None);

        WorldAiMemory memory = new WorldAiMemory(
                new WorldAiTurnMemory(1, ai.Id, 1, 0, 0, []))
            .WithStackMemory(thiefStack.Id, new WorldAiStackMemory(WorldStackOrder.Normal.Instance, new WorldAiTarget.Stack(enemyStack.Id)));
        WorldAiActionPlan plan = new(
            new WorldAiActionScore(2, 941),
            new WorldAiMoveStackCommand(thiefStack.Id, enemyStack.Position),
            memory);

        engine.CheckCommand(new WorldAiActionCommand(), [
            new WorldAiActionStartedEvent(plan),
            new WorldStackMovementConsumedEvent(thiefStack.Id, 10),
            new WorldStackBattleStartedEvent(thiefStack.Id, enemyStack.Id, enemyStack.Position, expectedBattleState, IsDuel: false),
            new WorldAiActionEndedEvent(ai.Id, memory)
        ]);
    }
}
