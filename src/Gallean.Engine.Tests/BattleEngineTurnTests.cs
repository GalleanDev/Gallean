namespace Gallean.Engine.Tests;

public sealed class BattleEngineTurnTests
{
    [Test]
    public void InitiativeOrderingTest()
    {
        UnitDefinition slowWarriorDefinition = TestHelpers.SimpleUnitDefinition("slow_warrior", maxHitpoints: 20, initiative: 30, damage: 5);
        UnitDefinition fastScoutDefinition = TestHelpers.SimpleUnitDefinition("fast_scout", maxHitpoints: 20, initiative: 50, damage: 5);
        UnitDefinition mediumMageDefinition = TestHelpers.SimpleUnitDefinition("medium_mage", maxHitpoints: 20, initiative: 40, damage: 5);

        BattleUnit slowWarrior = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, slowWarriorDefinition.Id, BattleSide.Attacker, hitpoints: 20);

        BattleUnit fastScout = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, fastScoutDefinition.Id, BattleSide.Defender, hitpoints: 20);

        BattleUnit mediumMage = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, mediumMageDefinition.Id, BattleSide.Defender, hitpoints: 20);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(slowWarriorDefinition)
            .WithDefinition(fastScoutDefinition)
            .WithDefinition(mediumMageDefinition)
            .WithAttacker(slowWarrior, new GridPosition(0, 0))
            .WithDefender(fastScout, new GridPosition(0, 0))
            .WithDefender(mediumMage, new GridPosition(0, 1))
            .Build();

        engine.CheckCommand(
            new BattleSkipCommand(fastScout.Id),
            [
                new BattleUnitTurnSkippedEvent(fastScout.Id),
                new BattleUnitTurnEndingEvent(fastScout.Id),
                new BattleUnitTurnEndedEvent(fastScout.Id),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(mediumMage.Id, BattleUnitTurnType.Normal))
            ]
        );

        engine.CheckCommand(
            new BattleSkipCommand(mediumMage.Id),
            [
                new BattleUnitTurnSkippedEvent(mediumMage.Id),
                new BattleUnitTurnEndingEvent(mediumMage.Id),
                new BattleUnitTurnEndedEvent(mediumMage.Id),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(slowWarrior.Id, BattleUnitTurnType.Normal))
            ]
        );

        engine.CheckCommand(
            new BattleAttackCommand(slowWarrior.Id, new BattleAttackUnitTarget(fastScout.Id)),
            [
                new BattleUnitAttackEvent(slowWarrior.Id, new BattleAttackUnitTarget(fastScout.Id), [new UnitAttackDefinitionId("slow_warrior")]),
                new BattleUnitAttackedEvent(slowWarrior.Id, fastScout.Id, new UnitAttackDefinitionId("slow_warrior")),
                new BattleUnitAttackHitEvent(slowWarrior.Id, fastScout.Id, new UnitAttackDefinitionId("slow_warrior"), 5),
                new BattleUnitTurnEndingEvent(slowWarrior.Id),
                new BattleUnitTurnEndedEvent(slowWarrior.Id),
                new BattleRoundEndedEvent(),
                new BattleRoundStartedEvent(BattleRound.Normal(2), [new BattleUnitTurn(fastScout.Id, BattleUnitTurnType.Normal), new BattleUnitTurn(mediumMage.Id, BattleUnitTurnType.Normal), new BattleUnitTurn(slowWarrior.Id, BattleUnitTurnType.Normal)]),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(fastScout.Id, BattleUnitTurnType.Normal))
            ]
        );

        UnitGrid<BattleUnit> expectedDefenderGrid = new UnitGrid<BattleUnit>.Builder()
            .Place(fastScout with { Hitpoints = 15 }, new GridPosition(0, 0), false)
            .Place(mediumMage, new GridPosition(0, 1), false)
            .Build();

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                DefenderGrid = expectedDefenderGrid,
                CurrentUnitTurn = new BattleUnitTurn(fastScout.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(2)
            }));
    }

    [Test]
    public void NotUnitTurnTest()
    {
        UnitDefinition warriorDefinition = TestHelpers.SimpleUnitDefinition("warrior", maxHitpoints: 20, initiative: 50, damage: 5);
        UnitDefinition scoutDefinition = TestHelpers.SimpleUnitDefinition("scout", maxHitpoints: 20, initiative: 40, damage: 5);

        BattleUnit warrior = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, warriorDefinition.Id, BattleSide.Attacker, hitpoints: 20);

        BattleUnit scout = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, scoutDefinition.Id, BattleSide.Defender, hitpoints: 20);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(warriorDefinition)
            .WithDefinition(scoutDefinition)
            .WithAttacker(warrior, new GridPosition(0, 0))
            .WithDefender(scout, new GridPosition(0, 0))
            .Build();

        engine.CheckCommandError(
            new BattleAttackCommand(scout.Id, new BattleAttackUnitTarget(warrior.Id)),
            BattleCommandError.NotUnitTurn
        );
    }

    [Test]
    public void SkipTurnTest()
    {
        UnitDefinition warriorDefinition = TestHelpers.SimpleUnitDefinition("warrior", maxHitpoints: 20, initiative: 50, damage: 5);
        UnitDefinition scoutDefinition = TestHelpers.SimpleUnitDefinition("scout", maxHitpoints: 20, initiative: 40, damage: 5);

        BattleUnit warrior = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, warriorDefinition.Id, BattleSide.Attacker, hitpoints: 20);

        BattleUnit scout = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, scoutDefinition.Id, BattleSide.Defender, hitpoints: 20);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(warriorDefinition)
            .WithDefinition(scoutDefinition)
            .WithAttacker(warrior, new GridPosition(0, 0))
            .WithDefender(scout, new GridPosition(0, 0))
            .Build();

        engine.CheckCommand(
            new BattleSkipCommand(warrior.Id),
            [
                new BattleUnitTurnSkippedEvent(warrior.Id),
                new BattleUnitTurnEndingEvent(warrior.Id),
                new BattleUnitTurnEndedEvent(warrior.Id),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(scout.Id, BattleUnitTurnType.Normal))
            ]
        );

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                CurrentUnitTurn = new BattleUnitTurn(scout.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(1)
            }));
    }
}
