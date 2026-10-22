namespace Gallean.Engine.Tests;

public sealed class BattleEngineTests
{
    [Test]
    public void GoldenTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 100, initiative: 50, damage: 20) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("attacker"), initiative: 50, damage: 20, accuracy: 100, reach: UnitAttackReach.Adjacent)
        };

        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 100, initiative: 40, damage: 10) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("defender"), initiative: 40, damage: 10, accuracy: 100, reach: UnitAttackReach.Adjacent)
        };

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: attackerDefinition.MaxHitpoints);

        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: defenderDefinition.MaxHitpoints);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(attacker, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommandError(
            new BattleAttackCommand(new BattleUnitId(2), new BattleAttackUnitTarget(new BattleUnitId(1))),
            BattleCommandError.NotUnitTurn
        );

        engine.CheckCommand(
            new BattleAttackCommand(new BattleUnitId(1), new BattleAttackUnitTarget(new BattleUnitId(2))),
            [
                new BattleUnitAttackEvent(new BattleUnitId(1), new BattleAttackUnitTarget(new BattleUnitId(2)), [new UnitAttackDefinitionId("attacker")]),
                new BattleUnitAttackedEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("attacker")),
                new BattleUnitAttackHitEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("attacker"), 20),
                new BattleUnitTurnEndingEvent(new BattleUnitId(1)),
                new BattleUnitTurnEndedEvent(new BattleUnitId(1)),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(2), BattleUnitTurnType.Normal))
            ]
        );

        UnitGrid<BattleUnit> expectedAttackerGrid = new UnitGrid<BattleUnit>.Builder()
            .Place(attacker, GridPosition.FrontBottom, false)
            .Build();

        UnitGrid<BattleUnit> expectedDefenderGrid = new UnitGrid<BattleUnit>.Builder()
            .Place(defender with { Hitpoints = 80 }, GridPosition.FrontBottom, false)
            .Build();

        Assert.That(engine.State, Is.EqualTo(new BattleState(
            AttackerGrid: expectedAttackerGrid,
            DefenderGrid: expectedDefenderGrid,
            CurrentUnitTurn: new BattleUnitTurn(new BattleUnitId(2), BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: null,
            Modifiers: BattleModifiers.None
        )));
    }

    [Test]
    public void EventStepsTest()
    {
        UnitDefinition impDefinition = TestHelpers.SimpleUnitDefinition("imp", maxHitpoints: 20, initiative: 35, damage: 8);
        UnitDefinition summonerDefinition = TestHelpers.SimpleUnitDefinition("summoner", maxHitpoints: 30, initiative: 50, damage: 5) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("summoner"), initiative: 50, damage: 5, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Type = UnitAttackType.Summon,
                SummonUnitIds = [impDefinition.Id]
            }
        };
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 40, damage: 5);

        BattleUnit summoner = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, summonerDefinition.Id, BattleSide.Attacker, hitpoints: 30);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(summonerDefinition)
            .WithDefinition(impDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(summoner, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        List<EngineStep<BattleState, BattleEvent>> steps = [];
        using IDisposable subscription = engine.Events.Subscribe(steps.Add);

        BattleState initialState = engine.State;
        BattleAttackCommand command = new(summoner.Id, new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.FrontCenter));

        BattleUnitId summonId = new(2);
        BattleUnit summon = new(summonId, null, impDefinition.Id, BattleSide.Attacker, 20, 0, 0, 1, [], [], summoner.Id, [], null, false);

        BattleUnitAttackEvent attackEvent = new(summoner.Id, new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.FrontCenter), [new UnitAttackDefinitionId("summoner")]);
        BattleUnitSummonedEvent summonedEvent = new(summonId, impDefinition.Id, BattleSide.Attacker, GridPosition.FrontCenter, 20, 1, summoner.Id);
        BattleUnitTurnEndingEvent turnEndingEvent = new(summoner.Id);
        BattleUnitTurnEndedEvent turnEndedEvent = new(summoner.Id);
        BattleUnitTurnStartedEvent turnStartedEvent = new(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal));

        engine.CheckCommand(command, [attackEvent, summonedEvent, turnEndingEvent, turnEndedEvent, turnStartedEvent], dryRun: true);
        Assert.That(steps, Is.Empty);

        engine.CheckCommand(command, [attackEvent, summonedEvent, turnEndingEvent, turnEndedEvent, turnStartedEvent]);

        BattleState afterSummon = initialState with
        {
            AttackerGrid = new UnitGrid<BattleUnit>.Builder(initialState.AttackerGrid)
                .PlaceOverlay(summon, GridPosition.FrontCenter, false)
                .Build()
        };

        Assert.That(steps, Is.EqualTo([
            new EngineStep<BattleState, BattleEvent>(initialState, initialState, attackEvent),
            new EngineStep<BattleState, BattleEvent>(initialState, afterSummon, summonedEvent),
            new EngineStep<BattleState, BattleEvent>(afterSummon, afterSummon, turnEndingEvent),
            new EngineStep<BattleState, BattleEvent>(afterSummon, afterSummon, turnEndedEvent),
            new EngineStep<BattleState, BattleEvent>(afterSummon, engine.State, turnStartedEvent)
        ]));
    }

    [Test]
    public void DryRunTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 100, initiative: 50, damage: 20) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("attacker"), initiative: 50, damage: 20, accuracy: 100, reach: UnitAttackReach.Adjacent)
        };

        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 100, initiative: 40, damage: 10) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("defender"), initiative: 40, damage: 10, accuracy: 100, reach: UnitAttackReach.Adjacent)
        };

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: attackerDefinition.MaxHitpoints);

        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: defenderDefinition.MaxHitpoints);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(attacker, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        BattleState previousState = engine.State;

        engine.CheckCommand(
            new BattleAttackCommand(new BattleUnitId(1), new BattleAttackUnitTarget(new BattleUnitId(2))),
            [
                new BattleUnitAttackEvent(new BattleUnitId(1), new BattleAttackUnitTarget(new BattleUnitId(2)), [new UnitAttackDefinitionId("attacker")]),
                new BattleUnitAttackedEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("attacker")),
                new BattleUnitAttackHitEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("attacker"), 20),
                new BattleUnitTurnEndingEvent(new BattleUnitId(1)),
                new BattleUnitTurnEndedEvent(new BattleUnitId(1)),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(2), BattleUnitTurnType.Normal))
            ],
            dryRun: true
        );

        Assert.That(engine.State, Is.EqualTo(previousState));
    }

    [Test]
    public void CannotAttackOwnSideTest()
    {
        UnitDefinition warrior1Definition = TestHelpers.SimpleUnitDefinition("warrior1", maxHitpoints: 20, initiative: 50, damage: 5);
        UnitDefinition warrior2Definition = TestHelpers.SimpleUnitDefinition("warrior2", maxHitpoints: 20, initiative: 40, damage: 5);
        UnitDefinition scoutDefinition = TestHelpers.SimpleUnitDefinition("scout", maxHitpoints: 20, initiative: 30, damage: 5);

        BattleUnit warrior1 = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, warrior1Definition.Id, BattleSide.Attacker, hitpoints: warrior1Definition.MaxHitpoints);

        BattleUnit warrior2 = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, warrior2Definition.Id, BattleSide.Attacker, hitpoints: warrior2Definition.MaxHitpoints);

        BattleUnit scout = TestHelpers.SimpleBattleUnit(new BattleUnitId(3), null, scoutDefinition.Id, BattleSide.Defender, hitpoints: scoutDefinition.MaxHitpoints);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(warrior1Definition)
            .WithDefinition(warrior2Definition)
            .WithDefinition(scoutDefinition)
            .WithAttacker(warrior1, GridPosition.FrontBottom)
            .WithAttacker(warrior2, GridPosition.FrontCenter)
            .WithDefender(scout, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommandError(
            new BattleAttackCommand(warrior1.Id, new BattleAttackUnitTarget(warrior2.Id)),
            BattleCommandError.TargetIncorrect
        );
    }

    [Test]
    public void DeadUnitTest()
    {
        UnitDefinition warriorDefinition = TestHelpers.SimpleUnitDefinition("warrior", maxHitpoints: 100, initiative: 50, damage: 50);
        UnitDefinition scoutDefinition = TestHelpers.SimpleUnitDefinition("scout", maxHitpoints: 10, initiative: 40, damage: 5);
        UnitDefinition mageDefinition = TestHelpers.SimpleUnitDefinition("mage", maxHitpoints: 100, initiative: 30, damage: 5);

        BattleUnit warrior = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, warriorDefinition.Id, BattleSide.Attacker, hitpoints: warriorDefinition.MaxHitpoints);

        BattleUnit scout = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, scoutDefinition.Id, BattleSide.Defender, hitpoints: scoutDefinition.MaxHitpoints);

        BattleUnit mage = TestHelpers.SimpleBattleUnit(new BattleUnitId(3), null, mageDefinition.Id, BattleSide.Defender, hitpoints: mageDefinition.MaxHitpoints);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(warriorDefinition)
            .WithDefinition(scoutDefinition)
            .WithDefinition(mageDefinition)
            .WithAttacker(warrior, GridPosition.FrontBottom)
            .WithDefender(scout, GridPosition.FrontBottom)
            .WithDefender(mage, GridPosition.FrontCenter)
            .Build();

        engine.CheckCommand(
            new BattleAttackCommand(warrior.Id, new BattleAttackUnitTarget(scout.Id)),
            [
                new BattleUnitAttackEvent(warrior.Id, new BattleAttackUnitTarget(scout.Id), [new UnitAttackDefinitionId("warrior")]),
                new BattleUnitAttackedEvent(warrior.Id, scout.Id, new UnitAttackDefinitionId("warrior")),
                new BattleUnitAttackHitEvent(warrior.Id, scout.Id, new UnitAttackDefinitionId("warrior"), 50),
                new BattleUnitDiedEvent(scout.Id),
                new BattleUnitTurnEndingEvent(warrior.Id),
                new BattleUnitTurnEndedEvent(warrior.Id),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(mage.Id, BattleUnitTurnType.Normal))
            ]
        );

        UnitGrid<BattleUnit> expectedDefenderGrid = new UnitGrid<BattleUnit>.Builder()
            .Place(scout with { Hitpoints = 0 }, GridPosition.FrontBottom, false)
            .Place(mage, GridPosition.FrontCenter, false)
            .Build();

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                DefenderGrid = expectedDefenderGrid,
                CurrentUnitTurn = new BattleUnitTurn(mage.Id, BattleUnitTurnType.Normal)
            }));
    }

    [Test]
    public void TargetDeadTest()
    {
        UnitDefinition warriorDefinition = TestHelpers.SimpleUnitDefinition("warrior", maxHitpoints: 20, initiative: 50, damage: 10);
        UnitDefinition scoutDefinition = TestHelpers.SimpleUnitDefinition("scout", maxHitpoints: 10, initiative: 40, damage: 10);
        UnitDefinition healerDefinition = TestHelpers.SimpleUnitDefinition("healer", maxHitpoints: 10, initiative: 30, damage: 0);
        UnitDefinition defenderWarriorDefinition = TestHelpers.SimpleUnitDefinition("defender_warrior", maxHitpoints: 10, initiative: 20, damage: 10);

        BattleUnit warrior = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, warriorDefinition.Id, BattleSide.Attacker, hitpoints: warriorDefinition.MaxHitpoints);

        BattleUnit scout = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, scoutDefinition.Id, BattleSide.Attacker, hitpoints: scoutDefinition.MaxHitpoints);

        BattleUnit healer = TestHelpers.SimpleBattleUnit(new BattleUnitId(3), null, healerDefinition.Id, BattleSide.Defender, hitpoints: healerDefinition.MaxHitpoints);

        BattleUnit defenderWarrior = TestHelpers.SimpleBattleUnit(new BattleUnitId(4), null, defenderWarriorDefinition.Id, BattleSide.Defender, hitpoints: defenderWarriorDefinition.MaxHitpoints);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(warriorDefinition)
            .WithDefinition(scoutDefinition)
            .WithDefinition(healerDefinition)
            .WithDefinition(defenderWarriorDefinition)
            .WithAttacker(warrior, GridPosition.FrontBottom)
            .WithAttacker(scout, GridPosition.FrontCenter)
            .WithDefender(healer, GridPosition.FrontBottom)
            .WithDefender(defenderWarrior, GridPosition.FrontCenter)
            .Build();

        engine.CheckCommand(
            new BattleAttackCommand(warrior.Id, new BattleAttackUnitTarget(healer.Id)),
            [
                new BattleUnitAttackEvent(warrior.Id, new BattleAttackUnitTarget(healer.Id), [new UnitAttackDefinitionId("warrior")]),
                new BattleUnitAttackedEvent(warrior.Id, healer.Id, new UnitAttackDefinitionId("warrior")),
                new BattleUnitAttackHitEvent(warrior.Id, healer.Id, new UnitAttackDefinitionId("warrior"), 10),
                new BattleUnitDiedEvent(healer.Id),
                new BattleUnitTurnEndingEvent(warrior.Id),
                new BattleUnitTurnEndedEvent(warrior.Id),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(scout.Id, BattleUnitTurnType.Normal))
            ]
        );

        engine.CheckCommandError(
            new BattleAttackCommand(scout.Id, new BattleAttackUnitTarget(healer.Id)),
            BattleCommandError.TargetDead
        );
    }

    [Test]
    public void AllReachTargetDeadTest()
    {
        UnitDefinition warriorDefinition = TestHelpers.SimpleUnitDefinition("warrior", maxHitpoints: 20, initiative: 50, damage: 10);
        UnitDefinition mageDefinition = TestHelpers.SimpleUnitDefinition("mage", maxHitpoints: 10, initiative: 40, damage: 5) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("mage"), initiative: 40, damage: 5, accuracy: 100, reach: UnitAttackReach.All)
        };
        UnitDefinition healerDefinition = TestHelpers.SimpleUnitDefinition("healer", maxHitpoints: 10, initiative: 30, damage: 0);
        UnitDefinition defenderWarriorDefinition = TestHelpers.SimpleUnitDefinition("defender_warrior", maxHitpoints: 10, initiative: 20, damage: 10);

        BattleUnit warrior = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, warriorDefinition.Id, BattleSide.Attacker, hitpoints: warriorDefinition.MaxHitpoints);

        BattleUnit mage = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, mageDefinition.Id, BattleSide.Attacker, hitpoints: mageDefinition.MaxHitpoints);

        BattleUnit healer = TestHelpers.SimpleBattleUnit(new BattleUnitId(3), null, healerDefinition.Id, BattleSide.Defender, hitpoints: healerDefinition.MaxHitpoints);

        BattleUnit defenderWarrior = TestHelpers.SimpleBattleUnit(new BattleUnitId(4), null, defenderWarriorDefinition.Id, BattleSide.Defender, hitpoints: defenderWarriorDefinition.MaxHitpoints);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(warriorDefinition)
            .WithDefinition(mageDefinition)
            .WithDefinition(healerDefinition)
            .WithDefinition(defenderWarriorDefinition)
            .WithAttacker(warrior, GridPosition.FrontBottom)
            .WithAttacker(mage, GridPosition.BackCenter)
            .WithDefender(healer, GridPosition.FrontBottom)
            .WithDefender(defenderWarrior, GridPosition.FrontCenter)
            .Build();

        engine.CheckCommand(
            new BattleAttackCommand(warrior.Id, new BattleAttackUnitTarget(healer.Id)),
            [
                new BattleUnitAttackEvent(warrior.Id, new BattleAttackUnitTarget(healer.Id), [new UnitAttackDefinitionId("warrior")]),
                new BattleUnitAttackedEvent(warrior.Id, healer.Id, new UnitAttackDefinitionId("warrior")),
                new BattleUnitAttackHitEvent(warrior.Id, healer.Id, new UnitAttackDefinitionId("warrior"), 10),
                new BattleUnitDiedEvent(healer.Id),
                new BattleUnitTurnEndingEvent(warrior.Id),
                new BattleUnitTurnEndedEvent(warrior.Id),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(mage.Id, BattleUnitTurnType.Normal))
            ]
        );

        engine.CheckCommandError(
            new BattleAttackCommand(mage.Id, new BattleAttackUnitTarget(healer.Id)),
            BattleCommandError.TargetIncorrect
        );
    }

    [Test]
    public void NoActiveBattleTest()
    {
        UnitDefinition warriorDefinition = TestHelpers.SimpleUnitDefinition("warrior", maxHitpoints: 20, initiative: 50, damage: 25);
        UnitDefinition scoutDefinition = TestHelpers.SimpleUnitDefinition("scout", maxHitpoints: 10, initiative: 40, damage: 5);

        BattleUnit warrior = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, warriorDefinition.Id, BattleSide.Attacker, hitpoints: warriorDefinition.MaxHitpoints);

        BattleUnit scout = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, scoutDefinition.Id, BattleSide.Defender, hitpoints: scoutDefinition.MaxHitpoints);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(warriorDefinition)
            .WithDefinition(scoutDefinition)
            .WithAttacker(warrior, GridPosition.FrontBottom)
            .WithDefender(scout, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(
            new BattleAttackCommand(warrior.Id, new BattleAttackUnitTarget(scout.Id)),
            [
                new BattleUnitAttackEvent(warrior.Id, new BattleAttackUnitTarget(scout.Id), [new UnitAttackDefinitionId("warrior")]),
                new BattleUnitAttackedEvent(warrior.Id, scout.Id, new UnitAttackDefinitionId("warrior")),
                new BattleUnitAttackHitEvent(warrior.Id, scout.Id, new UnitAttackDefinitionId("warrior"), 25),
                new BattleUnitDiedEvent(scout.Id),
                new BattleUnitTurnEndingEvent(warrior.Id),
                new BattleRoundEndedEvent(),
                new BattleEndedEvent(BattleSide.Attacker),
                new BattleUnitExperienceGainedEvent(warrior.Id, 1)
            ]
        );

        engine.CheckCommandError(
            new BattleSkipCommand(warrior.Id),
            BattleCommandError.NoActiveBattle
        );
    }

    [Test]
    public void UnitDiesBattleEndsTest()
    {
        UnitDefinition warriorDefinition = TestHelpers.SimpleUnitDefinition("warrior", maxHitpoints: 20, initiative: 50, damage: 25);
        UnitDefinition scoutDefinition = TestHelpers.SimpleUnitDefinition("scout", maxHitpoints: 10, initiative: 40, damage: 5);

        BattleUnit warrior = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, warriorDefinition.Id, BattleSide.Attacker, hitpoints: warriorDefinition.MaxHitpoints);

        BattleUnit scout = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, scoutDefinition.Id, BattleSide.Defender, hitpoints: scoutDefinition.MaxHitpoints);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(warriorDefinition)
            .WithDefinition(scoutDefinition)
            .WithAttacker(warrior, GridPosition.FrontBottom)
            .WithDefender(scout, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(
            new BattleAttackCommand(warrior.Id, new BattleAttackUnitTarget(scout.Id)),
            [
                new BattleUnitAttackEvent(warrior.Id, new BattleAttackUnitTarget(scout.Id), [new UnitAttackDefinitionId("warrior")]),
                new BattleUnitAttackedEvent(warrior.Id, scout.Id, new UnitAttackDefinitionId("warrior")),
                new BattleUnitAttackHitEvent(warrior.Id, scout.Id, new UnitAttackDefinitionId("warrior"), 25),
                new BattleUnitDiedEvent(scout.Id),
                new BattleUnitTurnEndingEvent(warrior.Id),
                new BattleRoundEndedEvent(),
                new BattleEndedEvent(BattleSide.Attacker),
                new BattleUnitExperienceGainedEvent(warrior.Id, 1)
            ]
        );

        UnitGrid<BattleUnit> expectedDefenderGrid = new UnitGrid<BattleUnit>.Builder()
            .Place(scout with { Hitpoints = 0 }, GridPosition.FrontBottom, false)
            .Build();

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                DefenderGrid = expectedDefenderGrid,
                Outcome = new BattleOutcome(BattleSide.Attacker)
            }));
    }

    [Test]
    public void DryRunRandomTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 100, initiative: 50, damage: 20) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("attacker"), initiative: 50, damage: 20, accuracy: 51, reach: UnitAttackReach.Adjacent)
        };

        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 100, initiative: 40, damage: 10) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("defender"), initiative: 40, damage: 10, accuracy: 51, reach: UnitAttackReach.Adjacent)
        };

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: attackerDefinition.MaxHitpoints);

        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: defenderDefinition.MaxHitpoints);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(attacker, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        BattleEngine referenceEngine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(attacker, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(
            new BattleAttackCommand(attacker.Id, new BattleAttackUnitTarget(defender.Id)),
            [
                new BattleUnitAttackEvent(attacker.Id, new BattleAttackUnitTarget(defender.Id), [new UnitAttackDefinitionId("attacker")]),
                new BattleUnitAttackedEvent(attacker.Id, defender.Id, new UnitAttackDefinitionId("attacker")),
                new BattleUnitAttackHitEvent(attacker.Id, defender.Id, new UnitAttackDefinitionId("attacker"), 20),
                new BattleUnitTurnEndingEvent(attacker.Id),
                new BattleUnitTurnEndedEvent(attacker.Id),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal))
            ],
            dryRun: true
        );

        engine.CheckCommand(
            new BattleAttackCommand(attacker.Id, new BattleAttackUnitTarget(defender.Id)),
            [
                new BattleUnitAttackEvent(attacker.Id, new BattleAttackUnitTarget(defender.Id), [new UnitAttackDefinitionId("attacker")]),
                new BattleUnitAttackedEvent(attacker.Id, defender.Id, new UnitAttackDefinitionId("attacker")),
                new BattleUnitAttackHitEvent(attacker.Id, defender.Id, new UnitAttackDefinitionId("attacker"), 20),
                new BattleUnitTurnEndingEvent(attacker.Id),
                new BattleUnitTurnEndedEvent(attacker.Id),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal))
            ],
            dryRun: true
        );

        BattleCommandResult result = engine.DispatchCommand(
            new BattleAttackCommand(attacker.Id, new BattleAttackUnitTarget(defender.Id)));
        BattleCommandResult referenceResult = referenceEngine.DispatchCommand(
            new BattleAttackCommand(attacker.Id, new BattleAttackUnitTarget(defender.Id)));

        Assert.That(result.Error, Is.Null);
        Assert.That(referenceResult.Error, Is.Null);
        BattleEvent[] expectedEvents =
        [
            new BattleUnitAttackEvent(attacker.Id, new BattleAttackUnitTarget(defender.Id), [new UnitAttackDefinitionId("attacker")]),
            new BattleUnitAttackedEvent(attacker.Id, defender.Id, new UnitAttackDefinitionId("attacker")),
            new BattleUnitAttackHitEvent(attacker.Id, defender.Id, new UnitAttackDefinitionId("attacker"), 20),
            new BattleUnitTurnEndingEvent(attacker.Id),
            new BattleUnitTurnEndedEvent(attacker.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal))
        ];

        Assert.That(result.Events, Is.EqualTo(expectedEvents));
        Assert.That(referenceResult.Events, Is.EqualTo(expectedEvents));
        Assert.That(engine.State, Is.EqualTo(referenceEngine.State));
    }
}
