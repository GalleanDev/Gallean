using Gallean.Common;

namespace Gallean.Engine.Tests;

public sealed class WorldEngineAiVitalStackTests
{
    private static UnitDefinition Leader(string id, int hitpoints, int damage)
    {
        return TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition(id, maxHitpoints: hitpoints, initiative: 50, damage: damage),
            movementPoints: 6, leadership: 6);
    }

    private static WorldStack LeaderStack(WorldUnitId leaderId, string id, WorldPlayerId ownerId, WorldPosition position, UnitDefinition leader, WorldCanonicalStackOrder order)
    {
        return TestHelpers.SimpleStack(
                new WorldStackId(id),
                ownerId,
                position,
                new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(leaderId, leader), GridPosition.FrontBottom, false).Build(),
                movementPoints: 6) with
            {
                Order = order
            };
    }

    private static ScenarioEventDefinition DefeatEvent(WorldStackId stackId, WorldPlayerId winnerId, bool enabled)
    {
        return new ScenarioEventDefinition(
            new ScenarioEventId("Defeat"),
            [WorldRace.Heretic],
            [WorldRace.Heretic],
            [new ScenarioEventDestroyingStackCondition(new ScenarioStackReference.Stack(stackId))],
            [new ScenarioEventWinEffect(winnerId)],
            Enabled: enabled,
            OccurOnce: true,
            Chance: 100,
            Order: 0);
    }

    [Test]
    public void UnsafeTargetTest()
    {
        WorldMap map = TestHelpers.SimpleMap(12, 1);

        UnitDefinition main = Leader("main", hitpoints: 100, damage: 50);
        UnitDefinition rival = Leader("rival", hitpoints: 100, damage: 50);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack vitalStack = LeaderStack(new WorldUnitId("vitalLeader"), "Vital", player1.Id, new WorldPosition(0, 0), main, WorldStackOrder.Normal.Instance);
        WorldStack rivalStack = LeaderStack(new WorldUnitId("rivalLeader"), "Rival", player2.Id, new WorldPosition(10, 0), rival, WorldStackOrder.Stand.Instance);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(main)
            .WithDefinition(rival)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(vitalStack)
            .WithStack(rivalStack)
            .WithScenarioEvent(DefeatEvent(vitalStack.Id, player2.Id, enabled: true))
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.Empty);
    }

    [Test]
    public void DisabledEventTest()
    {
        WorldMap map = TestHelpers.SimpleMap(12, 1);

        UnitDefinition main = Leader("main", hitpoints: 100, damage: 50);
        UnitDefinition rival = Leader("rival", hitpoints: 100, damage: 50);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack vitalStack = LeaderStack(new WorldUnitId("vitalLeader"), "Vital", player1.Id, new WorldPosition(0, 0), main, WorldStackOrder.Normal.Instance);
        WorldStack rivalStack = LeaderStack(new WorldUnitId("rivalLeader"), "Rival", player2.Id, new WorldPosition(10, 0), rival, WorldStackOrder.Stand.Instance);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(main)
            .WithDefinition(rival)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(vitalStack)
            .WithStack(rivalStack)
            .WithScenarioEvent(DefeatEvent(vitalStack.Id, player2.Id, enabled: false))
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo(["[P1 1000] Move Vital -> (3, 0)"]));
    }

    [Test]
    public void SafeTargetTest()
    {
        WorldMap map = TestHelpers.SimpleMap(12, 1);

        UnitDefinition main = Leader("main", hitpoints: 100, damage: 50);
        UnitDefinition weak = Leader("weak", hitpoints: 10, damage: 5);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack vitalStack = LeaderStack(new WorldUnitId("vitalLeader"), "Vital", player1.Id, new WorldPosition(0, 0), main, WorldStackOrder.Normal.Instance);
        WorldStack weakStack = LeaderStack(new WorldUnitId("weakLeader"), "Weak", player2.Id, new WorldPosition(10, 0), weak, WorldStackOrder.Stand.Instance);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(main)
            .WithDefinition(weak)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(vitalStack)
            .WithStack(weakStack)
            .WithScenarioEvent(DefeatEvent(vitalStack.Id, player2.Id, enabled: true))
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo(["[P1 1000] Move Vital -> (3, 0)"]));
    }

    [Test]
    public void UnsafeOrderedAttackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        UnitDefinition main = Leader("main", hitpoints: 100, damage: 50);
        UnitDefinition rival = Leader("rival", hitpoints: 100, damage: 50);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack vitalStack = LeaderStack(new WorldUnitId("vitalLeader"), "Vital", player1.Id, new WorldPosition(0, 0), main, new WorldStackOrder.AttackStack(new WorldStackId("Rival")));
        WorldStack rivalStack = LeaderStack(new WorldUnitId("rivalLeader"), "Rival", player2.Id, new WorldPosition(1, 0), rival, WorldStackOrder.Stand.Instance);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(main)
            .WithDefinition(rival)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(vitalStack)
            .WithStack(rivalStack)
            .WithScenarioEvent(DefeatEvent(vitalStack.Id, player2.Id, enabled: true))
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.Empty);
    }

    [Test]
    public void SafeAttackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        UnitDefinition main = Leader("main", hitpoints: 100, damage: 50);
        UnitDefinition weak = Leader("weak", hitpoints: 10, damage: 5);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack vitalStack = LeaderStack(new WorldUnitId("vitalLeader"), "Vital", player1.Id, new WorldPosition(0, 0), main, WorldStackOrder.Normal.Instance);
        WorldStack weakStack = LeaderStack(new WorldUnitId("weakLeader"), "Weak", player2.Id, new WorldPosition(1, 0), weak, WorldStackOrder.Stand.Instance);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(main)
            .WithDefinition(weak)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(vitalStack)
            .WithStack(weakStack)
            .WithScenarioEvent(DefeatEvent(vitalStack.Id, player2.Id, enabled: true))
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo(["[P2 910] Move Vital -> (1, 0) (Attacker wins)"]));
    }

    [Test]
    public void NeutralOwnerTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        UnitDefinition main = Leader("main", hitpoints: 100, damage: 50);
        UnitDefinition rival = Leader("rival", hitpoints: 100, damage: 50);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Neutral, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack vitalStack = LeaderStack(new WorldUnitId("vitalLeader"), "Vital", player1.Id, new WorldPosition(0, 0), main, new WorldStackOrder.AttackStack(new WorldStackId("Rival")));
        WorldStack rivalStack = LeaderStack(new WorldUnitId("rivalLeader"), "Rival", player2.Id, new WorldPosition(1, 0), rival, WorldStackOrder.Stand.Instance);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(main)
            .WithDefinition(rival)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(vitalStack)
            .WithStack(rivalStack)
            .WithScenarioEvent(DefeatEvent(vitalStack.Id, player2.Id, enabled: true))
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo(["[P2 500] Move Vital -> (1, 0) (Attacker wins)"]));
    }

    [Test]
    public void SpawnedDefenderTest()
    {
        WorldMap map = TestHelpers.SimpleMap(12, 1);

        UnitDefinition main = Leader("main", hitpoints: 100, damage: 50);
        UnitDefinition weak = Leader("weak", hitpoints: 10, damage: 5);
        UnitDefinition monster = Leader("monster", hitpoints: 100, damage: 50);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack vitalStack = LeaderStack(new WorldUnitId("vitalLeader"), "Vital", player1.Id, new WorldPosition(0, 0), main, WorldStackOrder.Normal.Instance);
        WorldStack weakStack = LeaderStack(new WorldUnitId("weakLeader"), "Weak", player2.Id, new WorldPosition(10, 0), weak, WorldStackOrder.Stand.Instance);

        ScenarioLocationDefinition lair = new(new ScenarioLocationId("Lair"), new ScenarioTextId("Lair"), new WorldPosition(5, 0), 2);

        ScenarioStackTemplateDefinition monsterTemplate = new(
            new ScenarioStackTemplateId("Monster"),
            player2.Id,
            TestHelpers.NeutralSubrace.Id,
            TestHelpers.SingleTemplateUnitGrid(monster),
            new WorldStackOrder.DefendLocation(lair.Id, lair.Position, lair.Radius),
            Direction: null,
            AiPriority: 3);

        ScenarioEventDefinition spawnEvent = new(
            new ScenarioEventId("Spawn"),
            [WorldRace.Undead],
            [WorldRace.Undead],
            [new ScenarioEventEnteringZoneCondition(lair.Id)],
            [new ScenarioEventCreateStackEffect(monsterTemplate.Id, lair.Id)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 1);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(main)
            .WithDefinition(weak)
            .WithDefinition(monster)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(vitalStack)
            .WithStack(weakStack)
            .WithScenarioLocation(lair)
            .WithScenarioStackTemplate(monsterTemplate)
            .WithScenarioEvent(spawnEvent)
            .WithScenarioEvent(DefeatEvent(vitalStack.Id, player2.Id, enabled: true))
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.Empty);
    }

    [Test]
    public void SafeSpawnedDefenderTest()
    {
        WorldMap map = TestHelpers.SimpleMap(12, 1);

        UnitDefinition main = Leader("main", hitpoints: 100, damage: 50);
        UnitDefinition weak = Leader("weak", hitpoints: 10, damage: 5);
        UnitDefinition monster = Leader("monster", hitpoints: 10, damage: 5);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack vitalStack = LeaderStack(new WorldUnitId("vitalLeader"), "Vital", player1.Id, new WorldPosition(0, 0), main, WorldStackOrder.Normal.Instance);
        WorldStack weakStack = LeaderStack(new WorldUnitId("weakLeader"), "Weak", player2.Id, new WorldPosition(10, 0), weak, WorldStackOrder.Stand.Instance);

        ScenarioLocationDefinition lair = new(new ScenarioLocationId("Lair"), new ScenarioTextId("Lair"), new WorldPosition(5, 0), 2);

        ScenarioStackTemplateDefinition monsterTemplate = new(
            new ScenarioStackTemplateId("Monster"),
            player2.Id,
            TestHelpers.NeutralSubrace.Id,
            TestHelpers.SingleTemplateUnitGrid(monster),
            new WorldStackOrder.DefendLocation(lair.Id, lair.Position, lair.Radius),
            Direction: null,
            AiPriority: 3);

        ScenarioEventDefinition spawnEvent = new(
            new ScenarioEventId("Spawn"),
            [WorldRace.Undead],
            [WorldRace.Undead],
            [new ScenarioEventEnteringZoneCondition(lair.Id)],
            [new ScenarioEventCreateStackEffect(monsterTemplate.Id, lair.Id)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 1);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(main)
            .WithDefinition(weak)
            .WithDefinition(monster)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(vitalStack)
            .WithStack(weakStack)
            .WithScenarioLocation(lair)
            .WithScenarioStackTemplate(monsterTemplate)
            .WithScenarioEvent(spawnEvent)
            .WithScenarioEvent(DefeatEvent(vitalStack.Id, player2.Id, enabled: true))
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo(["[P1 1000] Move Vital -> (3, 0)"]));
    }
}
