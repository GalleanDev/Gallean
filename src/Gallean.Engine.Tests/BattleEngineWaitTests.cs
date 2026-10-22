namespace Gallean.Engine.Tests;

public sealed class BattleEngineWaitTests
{
    [Test]
    public void WaitOrderTest()
    {
        UnitDefinition fastDefinition = TestHelpers.SimpleUnitDefinition("fast", maxHitpoints: 20, initiative: 50, damage: 5);
        UnitDefinition mediumDefinition = TestHelpers.SimpleUnitDefinition("medium", maxHitpoints: 20, initiative: 40, damage: 5);
        UnitDefinition slowDefinition = TestHelpers.SimpleUnitDefinition("slow", maxHitpoints: 20, initiative: 30, damage: 5);

        BattleUnit fast = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, fastDefinition.Id, BattleSide.Attacker, hitpoints: 20);
        BattleUnit medium = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, mediumDefinition.Id, BattleSide.Defender, hitpoints: 20);
        BattleUnit slow = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, slowDefinition.Id, BattleSide.Defender, hitpoints: 20);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(fastDefinition)
            .WithDefinition(mediumDefinition)
            .WithDefinition(slowDefinition)
            .WithAttacker(fast, new GridPosition(0, 0))
            .WithDefender(medium, new GridPosition(0, 0))
            .WithDefender(slow, new GridPosition(0, 1))
            .Build();

        engine.CheckCommand(new BattleWaitCommand(fast.Id), [
            new BattleUnitWaitedEvent(fast.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(medium.Id, BattleUnitTurnType.Normal))
        ]);

        engine.CheckCommand(new BattleWaitCommand(medium.Id), [
            new BattleUnitWaitedEvent(medium.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(slow.Id, BattleUnitTurnType.Normal))
        ]);

        engine.CheckCommand(new BattleSkipCommand(slow.Id), [
            new BattleUnitTurnEndingEvent(slow.Id),
            new BattleUnitTurnEndedEvent(slow.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(fast.Id, BattleUnitTurnType.Resumed))
        ]);

        engine.CheckCommand(new BattleSkipCommand(fast.Id), [
            new BattleUnitTurnEndingEvent(fast.Id),
            new BattleUnitTurnEndedEvent(fast.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(medium.Id, BattleUnitTurnType.Resumed))
        ]);

        engine.CheckCommand(new BattleSkipCommand(medium.Id), [
            new BattleUnitTurnEndingEvent(medium.Id),
            new BattleUnitTurnEndedEvent(medium.Id),
            new BattleRoundEndedEvent(),
            new BattleRoundStartedEvent(BattleRound.Normal(2), [new BattleUnitTurn(fast.Id, BattleUnitTurnType.Normal), new BattleUnitTurn(medium.Id, BattleUnitTurnType.Normal), new BattleUnitTurn(slow.Id, BattleUnitTurnType.Normal)]),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(fast.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(fast, new GridPosition(0, 0), false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(medium, new GridPosition(0, 0), false)
                    .Place(slow, new GridPosition(0, 1), false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(fast.Id, BattleUnitTurnType.Normal),
                WaitingQueue = [],
                CurrentRound = BattleRound.Normal(2)
            }));
    }

    [Test]
    public void WaitLastTest()
    {
        UnitDefinition fastDefinition = TestHelpers.SimpleUnitDefinition("fast", maxHitpoints: 20, initiative: 50, damage: 5);
        UnitDefinition slowDefinition = TestHelpers.SimpleUnitDefinition("slow", maxHitpoints: 20, initiative: 40, damage: 5);

        BattleUnit fast = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, fastDefinition.Id, BattleSide.Attacker, hitpoints: 20);
        BattleUnit slow = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, slowDefinition.Id, BattleSide.Defender, hitpoints: 20);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(fastDefinition)
            .WithDefinition(slowDefinition)
            .WithAttacker(fast, new GridPosition(0, 0))
            .WithDefender(slow, new GridPosition(0, 0))
            .Build();

        engine.CheckCommand(new BattleWaitCommand(fast.Id), [
            new BattleUnitWaitedEvent(fast.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(slow.Id, BattleUnitTurnType.Normal))
        ]);

        engine.CheckCommand(new BattleSkipCommand(slow.Id), [
            new BattleUnitTurnEndingEvent(slow.Id),
            new BattleUnitTurnEndedEvent(slow.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(fast.Id, BattleUnitTurnType.Resumed))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(fast, new GridPosition(0, 0), false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(slow, new GridPosition(0, 0), false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(fast.Id, BattleUnitTurnType.Resumed),
                CurrentRound = BattleRound.Normal(1)
            }));
    }

    [Test]
    public void AlreadyWaitedTest()
    {
        UnitDefinition fastDefinition = TestHelpers.SimpleUnitDefinition("fast", maxHitpoints: 20, initiative: 50, damage: 5);
        UnitDefinition slowDefinition = TestHelpers.SimpleUnitDefinition("slow", maxHitpoints: 20, initiative: 40, damage: 5);

        BattleUnit fast = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, fastDefinition.Id, BattleSide.Attacker, hitpoints: 20);
        BattleUnit slow = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, slowDefinition.Id, BattleSide.Defender, hitpoints: 20);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(fastDefinition)
            .WithDefinition(slowDefinition)
            .WithAttacker(fast, new GridPosition(0, 0))
            .WithDefender(slow, new GridPosition(0, 0))
            .Build();

        _ = engine.DispatchCommand(new BattleWaitCommand(fast.Id));
        _ = engine.DispatchCommand(new BattleSkipCommand(slow.Id));

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(fast, new GridPosition(0, 0), false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(slow, new GridPosition(0, 0), false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(fast.Id, BattleUnitTurnType.Resumed),
                TurnQueue = [],
                WaitingQueue = [new BattleUnitTurn(fast.Id, BattleUnitTurnType.Resumed)],
                CurrentRound = BattleRound.Normal(1)
            }));

        engine.CheckCommandError(new BattleWaitCommand(fast.Id), BattleCommandError.UnitAlreadyWaited);
    }

    [Test]
    public void NotUnitTurnTest()
    {
        UnitDefinition fastDefinition = TestHelpers.SimpleUnitDefinition("fast", maxHitpoints: 20, initiative: 50, damage: 5);
        UnitDefinition slowDefinition = TestHelpers.SimpleUnitDefinition("slow", maxHitpoints: 20, initiative: 40, damage: 5);

        BattleUnit fast = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, fastDefinition.Id, BattleSide.Attacker, hitpoints: 20);
        BattleUnit slow = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, slowDefinition.Id, BattleSide.Defender, hitpoints: 20);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(fastDefinition)
            .WithDefinition(slowDefinition)
            .WithAttacker(fast, new GridPosition(0, 0))
            .WithDefender(slow, new GridPosition(0, 0))
            .Build();

        engine.CheckCommandError(new BattleWaitCommand(slow.Id), BattleCommandError.NotUnitTurn);
    }
}
