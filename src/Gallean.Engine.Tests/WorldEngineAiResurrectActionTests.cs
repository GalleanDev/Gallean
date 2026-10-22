using Gallean.Common;

namespace Gallean.Engine.Tests;

public sealed class WorldEngineAiResurrectActionTests
{
    private static WorldPlayerBuildingDefinition Temple()
    {
        return TestHelpers.SimpleBuildingDefinition(new WorldPlayerBuildingDefinitionId("temple"), WorldPlayerBuildingCategory.Heal);
    }

    [Test]
    public void DeadUnitTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^^..",
            "^^^^.."
        ]);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 3);
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 10) with
        {
            ReviveCost = new WorldResourceSet(50, 0, 0, 0, 0, 0)
        };

        WorldPlayerBuildingDefinition temple = Temple();

        WorldPlayer aiPlayer = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            IsAi = true,
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0),
            Buildings = new WorldPlayerBuildings([temple.Id], [temple.Id])
        };
        WorldPlayer humanPlayer = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            aiPlayer.Id,
            new WorldPosition(4, 4),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.BackBottom, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition) with { Hitpoints = 0 }, GridPosition.BackCenter, false)
                .Build(),
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(temple)
            .WithDefinition(TestHelpers.SimpleLord(aiPlayer.LordId, aiPlayer.Race, LordCategory.Warrior))
            .WithPlayer(aiPlayer)
            .WithDefinition(TestHelpers.SimpleLord(humanPlayer.LordId, humanPlayer.Race, LordCategory.Warrior))
            .WithPlayer(humanPlayer)
            .WithCapital(TestHelpers.SimpleCapital(new WorldFortificationId("cap"), aiPlayer.Id, new WorldPosition(4, 4), UnitGrid<WorldUnit>.Empty))
            .WithStack(stack)
            .Build();

        WorldAiMemory memory = new WorldAiMemory(new WorldAiTurnMemory(1, aiPlayer.Id, 1, 0, 0, []))
            .WithStackMemory(stack.Id, new WorldAiStackMemory(WorldStackOrder.Heal.Instance, null));
        WorldAiActionPlan plan = new(
            new WorldAiActionScore(5, 100),
            new WorldResurrectUnitCommand(new WorldUnitGroup.Stack(stack.Id), new WorldUnitId("soldier")),
            memory);

        engine.CheckCommand(new WorldAiActionCommand(), [
            new WorldAiActionStartedEvent(plan),
            new WorldUnitResurrectedEvent(new WorldUnitGroup.Stack(stack.Id), new WorldUnitId("soldier"), 1, new WorldResourceSet(50, 0, 0, 0, 0, 0)),
            new WorldAiActionEndedEvent(aiPlayer.Id, memory)
        ]);
    }

    [Test]
    public void MostValuableFirstTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^^..",
            "^^^^.."
        ]);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 3);
        UnitDefinition weakDefinition = TestHelpers.SimpleUnitDefinition("weak", maxHitpoints: 60, initiative: 40, damage: 10) with
        {
            ReviveCost = new WorldResourceSet(50, 0, 0, 0, 0, 0)
        };
        UnitDefinition strongDefinition = TestHelpers.SimpleUnitDefinition("strong", maxHitpoints: 120, initiative: 40, damage: 10) with
        {
            ReviveCost = new WorldResourceSet(50, 0, 0, 0, 0, 0)
        };

        WorldPlayerBuildingDefinition temple = Temple();

        WorldPlayer aiPlayer = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            IsAi = true,
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0),
            Buildings = new WorldPlayerBuildings([temple.Id], [temple.Id])
        };
        WorldPlayer humanPlayer = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            aiPlayer.Id,
            new WorldPosition(4, 4),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.BackBottom, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("weak"), weakDefinition) with { Hitpoints = 0 }, GridPosition.BackCenter, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("strong"), strongDefinition) with { Hitpoints = 0 }, GridPosition.BackTop, false)
                .Build(),
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(weakDefinition)
            .WithDefinition(strongDefinition)
            .WithDefinition(temple)
            .WithDefinition(TestHelpers.SimpleLord(aiPlayer.LordId, aiPlayer.Race, LordCategory.Warrior))
            .WithPlayer(aiPlayer)
            .WithDefinition(TestHelpers.SimpleLord(humanPlayer.LordId, humanPlayer.Race, LordCategory.Warrior))
            .WithPlayer(humanPlayer)
            .WithCapital(TestHelpers.SimpleCapital(new WorldFortificationId("cap"), aiPlayer.Id, new WorldPosition(4, 4), UnitGrid<WorldUnit>.Empty))
            .WithStack(stack)
            .Build();

        WorldAiMemory memory = new WorldAiMemory(new WorldAiTurnMemory(1, aiPlayer.Id, 1, 0, 0, []))
            .WithStackMemory(stack.Id, new WorldAiStackMemory(WorldStackOrder.Heal.Instance, null));
        WorldAiActionPlan plan = new(
            new WorldAiActionScore(5, 120),
            new WorldResurrectUnitCommand(new WorldUnitGroup.Stack(stack.Id), new WorldUnitId("strong")),
            memory);

        engine.CheckCommand(new WorldAiActionCommand(), [
            new WorldAiActionStartedEvent(plan),
            new WorldUnitResurrectedEvent(new WorldUnitGroup.Stack(stack.Id), new WorldUnitId("strong"), 1, new WorldResourceSet(50, 0, 0, 0, 0, 0)),
            new WorldAiActionEndedEvent(aiPlayer.Id, memory)
        ]);
    }

    [Test]
    public void InsufficientResourcesTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^^..",
            "^^^^.."
        ]);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 3);
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 10) with
        {
            ReviveCost = new WorldResourceSet(50, 0, 0, 0, 0, 0)
        };

        WorldPlayerBuildingDefinition temple = Temple();

        WorldPlayer aiPlayer = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            IsAi = true,
            Resources = new WorldResourceSet(10, 0, 0, 0, 0, 0),
            Buildings = new WorldPlayerBuildings([temple.Id], [temple.Id])
        };
        WorldPlayer humanPlayer = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            aiPlayer.Id,
            new WorldPosition(4, 4),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.BackCenter, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition) with { Hitpoints = 0 }, GridPosition.BackBottom, false)
                .Build(),
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(temple)
            .WithDefinition(TestHelpers.SimpleLord(aiPlayer.LordId, aiPlayer.Race, LordCategory.Warrior))
            .WithPlayer(aiPlayer)
            .WithDefinition(TestHelpers.SimpleLord(humanPlayer.LordId, humanPlayer.Race, LordCategory.Warrior))
            .WithPlayer(humanPlayer)
            .WithCapital(TestHelpers.SimpleCapital(new WorldFortificationId("cap"), aiPlayer.Id, new WorldPosition(4, 4), UnitGrid<WorldUnit>.Empty))
            .WithStack(stack)
            .Build();

        WorldAiMemory memory = new WorldAiMemory(new WorldAiTurnMemory(1, aiPlayer.Id, 0, 0, 0, [stack.Id]))
            .WithStackMemory(stack.Id, new WorldAiStackMemory(WorldStackOrder.Heal.Instance, null));

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
