using Gallean.Common;

namespace Gallean.Engine.Tests;

public sealed class WorldEngineAiFogOfWarTests
{
    [Test]
    public void UndiscoveredEnemyStackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 1);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 30, initiative: 40, damage: 15),
            movementPoints: 4, leadership: 6);
        UnitDefinition enemy = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 5, initiative: 1, damage: 1),
            movementPoints: 6, leadership: 6);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            IsAi = true,
            FogOfWar = TestHelpers.BuildFogOfWar(["....????"])
        };
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack aiStack = TestHelpers.SimpleStack(
                new WorldStackId("Ai"),
                player1.Id,
                new WorldPosition(0, 0),
                new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader), GridPosition.FrontBottom, false).Build(),
                movementPoints: 4) with
            {
                Order = WorldStackOrder.Berserk.Instance
            };

        WorldStack enemyStack = TestHelpers.SimpleStack(
            new WorldStackId("Enemy"),
            player2.Id,
            new WorldPosition(7, 0),
            new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("enemy"), enemy), GridPosition.FrontBottom, false).Build(),
            movementPoints: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(enemy)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(aiStack)
            .WithStack(enemyStack)
            .Build();

        WorldAiMemory memory = new(new WorldAiTurnMemory(1, player1.Id, 0, 0, 0, [aiStack.Id]));

        engine.CheckCommand(new WorldAiActionCommand(), [
            new WorldAiActionStartedEvent(WorldAiActionPlan.EndTurn(memory)),
            new WorldTurnEndedEvent(player1.Id),
            new WorldRoundEndedEvent(),
            new WorldRoundStartedEvent(),
            new WorldTurnStartingEvent(player2.Id),
            new WorldStackMovementRestoredEvent(enemyStack.Id, 6),
            new WorldTurnStartedEvent(player2.Id),
            new WorldTurnResourcesGeneratedEvent(player2.Id, new WorldResourceSet(100, 0, 0, 0, 25, 0)),
            new WorldAiActionEndedEvent(player1.Id, memory)
        ]);
    }

    [Test]
    public void DiscoveredEnemyStackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 1);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 30, initiative: 40, damage: 15),
            movementPoints: 4, leadership: 6);
        UnitDefinition enemy = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 5, initiative: 1, damage: 1),
            movementPoints: 6, leadership: 6);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            IsAi = true,
            FogOfWar = TestHelpers.BuildFogOfWar(["........"])
        };
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack aiStack = TestHelpers.SimpleStack(
                new WorldStackId("Ai"),
                player1.Id,
                new WorldPosition(0, 0),
                new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader), GridPosition.FrontBottom, false).Build(),
                movementPoints: 4) with
            {
                Order = WorldStackOrder.Berserk.Instance
            };

        WorldStack enemyStack = TestHelpers.SimpleStack(
            new WorldStackId("Enemy"),
            player2.Id,
            new WorldPosition(7, 0),
            new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("enemy"), enemy), GridPosition.FrontBottom, false).Build(),
            movementPoints: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(enemy)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(aiStack)
            .WithStack(enemyStack)
            .Build();

        WorldAiMemory memory = new WorldAiMemory(
                new WorldAiTurnMemory(1, player1.Id, 1, 0, 0, []))
            .WithStackMemory(aiStack.Id, new WorldAiStackMemory(WorldStackOrder.Normal.Instance, new WorldAiTarget.Stack(enemyStack.Id)));
        WorldAiActionPlan plan = new(
            new WorldAiActionScore(1, 1000),
            new WorldAiMoveStackCommand(aiStack.Id, new WorldPosition(2, 0)),
            memory);

        engine.CheckCommand(new WorldAiActionCommand(), [
            new WorldAiActionStartedEvent(plan),
            new WorldStackMovingEvent(aiStack.Id, new WorldPosition(0, 0), new WorldPosition(1, 0), WorldStackDirection.SouthEast, 2, [new WorldPosition(2, 0)], null),
            new WorldStackMovedEvent(aiStack.Id, new WorldPosition(0, 0), new WorldPosition(1, 0), WorldStackDirection.SouthEast, 2, [new WorldPosition(2, 0)], null),
            new WorldStackMovingEvent(aiStack.Id, new WorldPosition(1, 0), new WorldPosition(2, 0), WorldStackDirection.SouthEast, 0, [], null),
            new WorldStackMovedEvent(aiStack.Id, new WorldPosition(1, 0), new WorldPosition(2, 0), WorldStackDirection.SouthEast, 0, [], null),
            new WorldAiActionEndedEvent(player1.Id, memory)
        ]);
    }

    [Test]
    public void NeutralTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 1);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 30, initiative: 40, damage: 15),
            movementPoints: 4, leadership: 6);
        UnitDefinition enemy = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 5, initiative: 1, damage: 1),
            movementPoints: 6, leadership: 6);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Neutral, map) with
        {
            IsAi = true,
            FogOfWar = TestHelpers.BuildFogOfWar(["....????"])
        };
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack aiStack = TestHelpers.SimpleStack(
                new WorldStackId("Ai"),
                player1.Id,
                new WorldPosition(0, 0),
                new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader), GridPosition.FrontBottom, false).Build(),
                movementPoints: 4) with
            {
                Order = WorldStackOrder.Berserk.Instance
            };

        WorldStack enemyStack = TestHelpers.SimpleStack(
            new WorldStackId("Enemy"),
            player2.Id,
            new WorldPosition(7, 0),
            new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("enemy"), enemy), GridPosition.FrontBottom, false).Build(),
            movementPoints: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(enemy)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(aiStack)
            .WithStack(enemyStack)
            .Build();

        WorldAiMemory memory = new WorldAiMemory(
                new WorldAiTurnMemory(1, player1.Id, 1, 0, 0, []))
            .WithStackMemory(aiStack.Id, new WorldAiStackMemory(WorldStackOrder.Normal.Instance, new WorldAiTarget.Stack(enemyStack.Id)));
        WorldAiActionPlan plan = new(
            new WorldAiActionScore(1, 1000),
            new WorldAiMoveStackCommand(aiStack.Id, new WorldPosition(2, 0)),
            memory);

        engine.CheckCommand(new WorldAiActionCommand(), [
            new WorldAiActionStartedEvent(plan),
            new WorldStackMovingEvent(aiStack.Id, new WorldPosition(0, 0), new WorldPosition(1, 0), WorldStackDirection.SouthEast, 2, [new WorldPosition(2, 0)], null),
            new WorldStackMovedEvent(aiStack.Id, new WorldPosition(0, 0), new WorldPosition(1, 0), WorldStackDirection.SouthEast, 2, [new WorldPosition(2, 0)], null),
            new WorldStackMovingEvent(aiStack.Id, new WorldPosition(1, 0), new WorldPosition(2, 0), WorldStackDirection.SouthEast, 0, [], null),
            new WorldStackMovedEvent(aiStack.Id, new WorldPosition(1, 0), new WorldPosition(2, 0), WorldStackDirection.SouthEast, 0, [], null),
            new WorldAiActionEndedEvent(player1.Id, memory)
        ]);
    }

    [Test]
    public void UndiscoveredOrderTargetTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 1);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 30, initiative: 40, damage: 15),
            movementPoints: 4, leadership: 6);
        UnitDefinition enemy = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 5, initiative: 1, damage: 1),
            movementPoints: 6, leadership: 6);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            IsAi = true,
            FogOfWar = TestHelpers.BuildFogOfWar(["....????"])
        };
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack aiStack = TestHelpers.SimpleStack(
                new WorldStackId("Ai"),
                player1.Id,
                new WorldPosition(0, 0),
                new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader), GridPosition.FrontBottom, false).Build(),
                movementPoints: 4) with
            {
                Order = new WorldStackOrder.AttackStack(new WorldStackId("Enemy"))
            };

        WorldStack enemyStack = TestHelpers.SimpleStack(
            new WorldStackId("Enemy"),
            player2.Id,
            new WorldPosition(7, 0),
            new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("enemy"), enemy), GridPosition.FrontBottom, false).Build(),
            movementPoints: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(enemy)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(aiStack)
            .WithStack(enemyStack)
            .Build();

        WorldAiMemory memory = new(new WorldAiTurnMemory(1, player1.Id, 1, 0, 0, []));
        WorldAiActionPlan plan = new(
            new WorldAiActionScore(1, 1000),
            new WorldAiMoveStackCommand(aiStack.Id, new WorldPosition(2, 0)),
            memory);

        engine.CheckCommand(new WorldAiActionCommand(), [
            new WorldAiActionStartedEvent(plan),
            new WorldStackMovingEvent(aiStack.Id, new WorldPosition(0, 0), new WorldPosition(1, 0), WorldStackDirection.SouthEast, 2, [new WorldPosition(2, 0)], null),
            new WorldStackMovedEvent(aiStack.Id, new WorldPosition(0, 0), new WorldPosition(1, 0), WorldStackDirection.SouthEast, 2, [new WorldPosition(2, 0)], null),
            new WorldStackMovingEvent(aiStack.Id, new WorldPosition(1, 0), new WorldPosition(2, 0), WorldStackDirection.SouthEast, 0, [], null),
            new WorldStackMovedEvent(aiStack.Id, new WorldPosition(1, 0), new WorldPosition(2, 0), WorldStackDirection.SouthEast, 0, [], null),
            new WorldAiActionEndedEvent(player1.Id, memory)
        ]);
    }
}
