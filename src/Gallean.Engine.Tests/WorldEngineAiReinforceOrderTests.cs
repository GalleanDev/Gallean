using Gallean.Common;

namespace Gallean.Engine.Tests;

public sealed class WorldEngineAiReinforceOrderTests
{
    [Test]
    public void SimpleTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^^^^^^^",
            "^^^^^^^^^^^^",
            "^^^^^^^^^^^^",
            "^^^^^^^^^^^^",
            "^^^^.^^^^^^.",
            "............"
        ]);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 3);
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 10);

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
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 2);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(TestHelpers.SimpleLord(aiPlayer.LordId, aiPlayer.Race, LordCategory.Warrior))
            .WithPlayer(aiPlayer)
            .WithDefinition(TestHelpers.SimpleLord(humanPlayer.LordId, humanPlayer.Race, LordCategory.Warrior))
            .WithPlayer(humanPlayer)
            .WithCapital(TestHelpers.SimpleCapital(
                new WorldFortificationId("cap"),
                aiPlayer.Id,
                new WorldPosition(4, 4),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition), GridPosition.FrontBottom, false)
                    .Build()))
            .WithCapital(TestHelpers.SimpleCapital(
                new WorldFortificationId("enemyCap"),
                humanPlayer.Id,
                new WorldPosition(11, 4),
                UnitGrid<WorldUnit>.Empty))
            .WithStack(stack)
            .Build();

        WorldAiMemory memory = new WorldAiMemory(new WorldAiTurnMemory(1, aiPlayer.Id, 1, 0, 0, []))
            .WithStackMemory(stack.Id, new WorldAiStackMemory(new WorldStackOrder.Reinforce(new WorldFortificationId("cap")), null));
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
    public void ReinforceAtCapitalTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^^^^^^^",
            "^^^^^^^^^^^^",
            "^^^^^^^^^^^^",
            "^^^^^^^^^^^^",
            "^^^^.^^^^^^.",
            "............"
        ]);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 3);
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 10);

        WorldUnit leader = TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition);
        WorldUnit soldier = TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition);

        WorldPlayer aiPlayer = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            IsAi = true,
            Resources = WorldResourceSet.Empty
        };
        WorldPlayer humanPlayer = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            aiPlayer.Id,
            new WorldPosition(4, 4),
            new UnitGrid<WorldUnit>.Builder()
                .Place(leader, GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 2);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(TestHelpers.SimpleLord(aiPlayer.LordId, aiPlayer.Race, LordCategory.Warrior))
            .WithPlayer(aiPlayer)
            .WithDefinition(TestHelpers.SimpleLord(humanPlayer.LordId, humanPlayer.Race, LordCategory.Warrior))
            .WithPlayer(humanPlayer)
            .WithCapital(TestHelpers.SimpleCapital(
                new WorldFortificationId("cap"),
                aiPlayer.Id,
                new WorldPosition(4, 4),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(soldier, GridPosition.FrontBottom, false)
                    .Build()))
            .WithCapital(TestHelpers.SimpleCapital(
                new WorldFortificationId("enemyCap"),
                humanPlayer.Id,
                new WorldPosition(11, 4),
                UnitGrid<WorldUnit>.Empty))
            .WithStack(stack)
            .Build();

        WorldAiMemory memory = new WorldAiMemory(new WorldAiTurnMemory(1, aiPlayer.Id, 1, 0, 0, []))
            .WithStackMemory(stack.Id, new WorldAiStackMemory(new WorldStackOrder.Reinforce(new WorldFortificationId("cap")), null));
        UnitGrid<WorldUnit> reinforcedUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(leader, GridPosition.BackCenter, false)
            .Place(soldier, GridPosition.BackBottom, false)
            .Build();
        WorldAiActionPlan plan = new(
            new WorldAiActionScore(3, 1000),
            new WorldAutoFitStackCommand(stack.Id),
            memory);

        engine.CheckCommand(new WorldAiActionCommand(), [
            new WorldAiActionStartedEvent(plan),
            new WorldStackAutoFittedEvent(
                stack.Id,
                reinforcedUnits,
                UnitGrid<WorldUnit>.Empty,
                WorldInventory.Empty,
                WorldInventory.Empty,
                LeaderEquipment.Empty),
            new WorldAiActionEndedEvent(aiPlayer.Id, memory)
        ]);
    }

    [Test]
    public void EmptyGarrisonTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^^^^^^^",
            "^^^^^^^^^^^^",
            "^^^^^^^^^^^^",
            "^^^^^^^^^^^^",
            "^^^^.^^^^^^.",
            "............"
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
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(TestHelpers.SimpleLord(aiPlayer.LordId, aiPlayer.Race, LordCategory.Warrior))
            .WithPlayer(aiPlayer)
            .WithDefinition(TestHelpers.SimpleLord(humanPlayer.LordId, humanPlayer.Race, LordCategory.Warrior))
            .WithPlayer(humanPlayer)
            .WithCapital(TestHelpers.SimpleCapital(
                new WorldFortificationId("cap"),
                aiPlayer.Id,
                new WorldPosition(4, 4),
                UnitGrid<WorldUnit>.Empty))
            .WithCapital(TestHelpers.SimpleCapital(
                new WorldFortificationId("enemyCap"),
                humanPlayer.Id,
                new WorldPosition(11, 4),
                UnitGrid<WorldUnit>.Empty))
            .WithStack(stack)
            .Build();

        WorldAiMemory memory = new WorldAiMemory(new WorldAiTurnMemory(1, aiPlayer.Id, 0, 0, 0, [stack.Id]))
            .WithStackMemory(stack.Id, new WorldAiStackMemory(WorldStackOrder.Normal.Instance, null));

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
    }

    [Test]
    public void FullStrengthTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^^^^^^^",
            "^^^^^^^^^^^^",
            "^^^^^^^^^^^^",
            "^^^^^^^^^^^^",
            "^^^^.^^^^^^.",
            "............"
        ]);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 3);
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 10);

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
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.BackBottom, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition), GridPosition.BackCenter, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier2"), soldierDefinition), GridPosition.BackTop, false)
                .Build(),
            movementPoints: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(TestHelpers.SimpleLord(aiPlayer.LordId, aiPlayer.Race, LordCategory.Warrior))
            .WithPlayer(aiPlayer)
            .WithDefinition(TestHelpers.SimpleLord(humanPlayer.LordId, humanPlayer.Race, LordCategory.Warrior))
            .WithPlayer(humanPlayer)
            .WithCapital(TestHelpers.SimpleCapital(
                new WorldFortificationId("cap"),
                aiPlayer.Id,
                new WorldPosition(4, 4),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier3"), soldierDefinition), GridPosition.FrontBottom, false)
                    .Build()))
            .WithCapital(TestHelpers.SimpleCapital(
                new WorldFortificationId("enemyCap"),
                humanPlayer.Id,
                new WorldPosition(11, 4),
                UnitGrid<WorldUnit>.Empty))
            .WithStack(stack)
            .Build();

        WorldAiMemory memory = new WorldAiMemory(new WorldAiTurnMemory(1, aiPlayer.Id, 0, 0, 0, [stack.Id]))
            .WithStackMemory(stack.Id, new WorldAiStackMemory(WorldStackOrder.Normal.Instance, null));

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
    }
}
