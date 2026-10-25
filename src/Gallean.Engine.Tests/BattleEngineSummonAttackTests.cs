namespace Gallean.Engine.Tests;

public sealed class BattleEngineSummonAttackTests
{
    [Test]
    public void PlacesUnitTest()
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

        BattleUnitId summonId = new(2);
        BattleUnit summon = new(summonId, null, impDefinition.Id, BattleSide.Attacker, 20, 0, 0, 1, [], [], summoner.Id, [], null, false);

        engine.CheckCommand(new BattleAttackCommand(summoner.Id, new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.FrontCenter)), [
            new BattleUnitAttackEvent(summoner.Id, new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.FrontCenter), [new UnitAttackDefinitionId("summoner")]),
            new BattleUnitSummonedEvent(summonId, impDefinition.Id, BattleSide.Attacker, GridPosition.FrontCenter, 20, 1, summoner.Id),
            new BattleUnitTurnEndingEvent(summoner.Id),
            new BattleUnitTurnEndedEvent(summoner.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(new BattleState(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(summoner, GridPosition.FrontBottom, false)
                .PlaceOverlay(summon, GridPosition.FrontCenter, false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder().Place(defender, GridPosition.FrontBottom, false).Build(),
            CurrentUnitTurn: new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: null,
            Modifiers: BattleModifiers.None)));
    }

    [Test]
    public void OverDeadUnitTest()
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
        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ally", maxHitpoints: 10, initiative: 45, damage: 5);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 40, damage: 5);

        BattleUnit summoner = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, summonerDefinition.Id, BattleSide.Attacker, hitpoints: 30);
        BattleUnit ally = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, allyDefinition.Id, BattleSide.Attacker, hitpoints: 0);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(summonerDefinition)
            .WithDefinition(impDefinition)
            .WithDefinition(allyDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(summoner, GridPosition.FrontBottom)
            .WithAttacker(ally, GridPosition.FrontCenter)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        BattleUnitId summonId = new(3);
        BattleUnit summon = new(summonId, null, impDefinition.Id, BattleSide.Attacker, 20, 0, 0, 1, [], [], summoner.Id, [], null, false);

        engine.CheckCommand(new BattleAttackCommand(summoner.Id, new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.FrontCenter)), [
            new BattleUnitAttackEvent(summoner.Id, new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.FrontCenter), [new UnitAttackDefinitionId("summoner")]),
            new BattleUnitSummonedEvent(summonId, impDefinition.Id, BattleSide.Attacker, GridPosition.FrontCenter, 20, 1, summoner.Id),
            new BattleUnitTurnEndingEvent(summoner.Id),
            new BattleUnitTurnEndedEvent(summoner.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(new BattleState(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(summoner, GridPosition.FrontBottom, false)
                .Place(ally, GridPosition.FrontCenter, false)
                .PlaceOverlay(summon, GridPosition.FrontCenter, false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder().Place(defender, GridPosition.FrontBottom, false).Build(),
            CurrentUnitTurn: new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: null,
            Modifiers: BattleModifiers.None)));
    }

    [Test]
    public void OverRetreatedUnitTest()
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
        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ally", maxHitpoints: 10, initiative: 45, damage: 5);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 40, damage: 5);

        BattleUnit summoner = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, summonerDefinition.Id, BattleSide.Attacker, hitpoints: 30);
        BattleUnit ally = new(new BattleUnitId(1), null, allyDefinition.Id, BattleSide.Attacker, 10, 0, 0, 1, [], [new BattleRetreatedStatusEffect()], null, [], null, false);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(summonerDefinition)
            .WithDefinition(impDefinition)
            .WithDefinition(allyDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(summoner, GridPosition.FrontBottom)
            .WithAttacker(ally, GridPosition.FrontCenter)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        BattleUnitId summonId = new(3);
        BattleUnit summon = new(summonId, null, impDefinition.Id, BattleSide.Attacker, 20, 0, 0, 1, [], [], summoner.Id, [], null, false);

        engine.CheckCommand(new BattleAttackCommand(summoner.Id, new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.FrontCenter)), [
            new BattleUnitAttackEvent(summoner.Id, new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.FrontCenter), [new UnitAttackDefinitionId("summoner")]),
            new BattleUnitSummonedEvent(summonId, impDefinition.Id, BattleSide.Attacker, GridPosition.FrontCenter, 20, 1, summoner.Id),
            new BattleUnitTurnEndingEvent(summoner.Id),
            new BattleUnitTurnEndedEvent(summoner.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(new BattleState(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(summoner, GridPosition.FrontBottom, false)
                .Place(ally, GridPosition.FrontCenter, false)
                .PlaceOverlay(summon, GridPosition.FrontCenter, false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder().Place(defender, GridPosition.FrontBottom, false).Build(),
            CurrentUnitTurn: new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: null,
            Modifiers: BattleModifiers.None)));
    }

    [Test]
    public void LargeSummonOverDeadUnitsTest()
    {
        UnitDefinition dragonDefinition = TestHelpers.SimpleUnitDefinition("dragon", maxHitpoints: 60, initiative: 35, damage: 20) with
        {
            IsLarge = true
        };
        UnitDefinition summonerDefinition = TestHelpers.SimpleUnitDefinition("summoner", maxHitpoints: 30, initiative: 50, damage: 5) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("summoner"), initiative: 50, damage: 5, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Type = UnitAttackType.Summon,
                SummonUnitIds = [dragonDefinition.Id]
            }
        };
        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ally", maxHitpoints: 10, initiative: 45, damage: 5);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 40, damage: 5);

        BattleUnit summoner = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, summonerDefinition.Id, BattleSide.Attacker, hitpoints: 30);
        BattleUnit frontAlly = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, allyDefinition.Id, BattleSide.Attacker, hitpoints: 0);
        BattleUnit backAlly = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, allyDefinition.Id, BattleSide.Attacker, hitpoints: 0);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(3), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(summonerDefinition)
            .WithDefinition(dragonDefinition)
            .WithDefinition(allyDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(summoner, GridPosition.FrontBottom)
            .WithAttacker(frontAlly, GridPosition.FrontCenter)
            .WithAttacker(backAlly, GridPosition.BackCenter)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        BattleUnitId summonId = new(4);
        BattleUnit summon = new(summonId, null, dragonDefinition.Id, BattleSide.Attacker, 60, 0, 0, 1, [], [], summoner.Id, [], null, false);

        engine.CheckCommand(new BattleAttackCommand(summoner.Id, new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.FrontCenter)), [
            new BattleUnitAttackEvent(summoner.Id, new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.FrontCenter), [new UnitAttackDefinitionId("summoner")]),
            new BattleUnitSummonedEvent(summonId, dragonDefinition.Id, BattleSide.Attacker, GridPosition.FrontCenter, 60, 1, summoner.Id),
            new BattleUnitTurnEndingEvent(summoner.Id),
            new BattleUnitTurnEndedEvent(summoner.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(new BattleState(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(summoner, GridPosition.FrontBottom, false)
                .Place(frontAlly, GridPosition.FrontCenter, false)
                .Place(backAlly, GridPosition.BackCenter, false)
                .PlaceOverlay(summon, GridPosition.FrontCenter, true)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder().Place(defender, GridPosition.FrontBottom, false).Build(),
            CurrentUnitTurn: new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: null,
            Modifiers: BattleModifiers.None)));
    }

    [Test]
    public void RestoresOriginalUnitTest()
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
        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ally", maxHitpoints: 10, initiative: 45, damage: 5);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 40, damage: 30);

        BattleUnit summoner = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, summonerDefinition.Id, BattleSide.Attacker, hitpoints: 30);
        BattleUnit ally = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, allyDefinition.Id, BattleSide.Attacker, hitpoints: 0);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(summonerDefinition)
            .WithDefinition(impDefinition)
            .WithDefinition(allyDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(summoner, GridPosition.FrontBottom)
            .WithAttacker(ally, GridPosition.FrontCenter)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        BattleUnitId summonId = new(3);

        _ = engine.DispatchCommand(new BattleAttackCommand(summoner.Id, new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.FrontCenter)));

        engine.CheckCommand(new BattleAttackCommand(defender.Id, new BattleAttackUnitTarget(summonId)), [
            new BattleUnitAttackEvent(defender.Id, new BattleAttackUnitTarget(summonId), [new UnitAttackDefinitionId("defender")]),
            new BattleUnitAttackedEvent(defender.Id, summonId, new UnitAttackDefinitionId("defender")),
            new BattleUnitAttackHitEvent(defender.Id, summonId, new UnitAttackDefinitionId("defender"), 30),
            new BattleUnitDiedEvent(summonId),
            new BattleUnitTurnEndingEvent(defender.Id),
            new BattleUnitUnsummonedEvent(summonId),
            new BattleUnitTurnEndedEvent(defender.Id),
            new BattleRoundEndedEvent(),
            new BattleRoundStartedEvent(BattleRound.Normal(2), [
                new BattleUnitTurn(summoner.Id, BattleUnitTurnType.Normal),
                new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal)
            ]),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(summoner.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(new BattleState(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(summoner, GridPosition.FrontBottom, false)
                .Place(ally, GridPosition.FrontCenter, false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder().Place(defender, GridPosition.FrontBottom, false).Build(),
            CurrentUnitTurn: new BattleUnitTurn(summoner.Id, BattleUnitTurnType.Normal),
            TurnQueue: [new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal)],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(2),
            Outcome: null,
            Modifiers: BattleModifiers.None)));
    }

    [Test]
    public void RemovesSummonTest()
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
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 40, damage: 30);

        BattleUnit summoner = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, summonerDefinition.Id, BattleSide.Attacker, hitpoints: 30);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(summonerDefinition)
            .WithDefinition(impDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(summoner, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        BattleUnitId summonId = new(2);
        BattleUnit summon = new(summonId, null, impDefinition.Id, BattleSide.Attacker, 20, 0, 0, 1, [], [], summoner.Id, [], null, false);

        _ = engine.DispatchCommand(new BattleAttackCommand(summoner.Id, new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.FrontCenter)));

        Assert.That(engine.State, Is.EqualTo(new BattleState(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(summoner, GridPosition.FrontBottom, false)
                .PlaceOverlay(summon, GridPosition.FrontCenter, false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder().Place(defender, GridPosition.FrontBottom, false).Build(),
            CurrentUnitTurn: new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: null,
            Modifiers: BattleModifiers.None)));

        engine.CheckCommand(new BattleAttackCommand(defender.Id, new BattleAttackUnitTarget(summonId)), [
            new BattleUnitAttackEvent(defender.Id, new BattleAttackUnitTarget(summonId), [new UnitAttackDefinitionId("defender")]),
            new BattleUnitAttackedEvent(defender.Id, summonId, new UnitAttackDefinitionId("defender")),
            new BattleUnitAttackHitEvent(defender.Id, summonId, new UnitAttackDefinitionId("defender"), 30),
            new BattleUnitDiedEvent(summonId),
            new BattleUnitTurnEndingEvent(defender.Id),
            new BattleUnitUnsummonedEvent(summonId),
            new BattleUnitTurnEndedEvent(defender.Id),
            new BattleRoundEndedEvent(),
            new BattleRoundStartedEvent(BattleRound.Normal(2), [new BattleUnitTurn(summoner.Id, BattleUnitTurnType.Normal), new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal)]),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(summoner.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(new BattleState(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder().Place(summoner, GridPosition.FrontBottom, false).Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder().Place(defender, GridPosition.FrontBottom, false).Build(),
            CurrentUnitTurn: new BattleUnitTurn(summoner.Id, BattleUnitTurnType.Normal),
            TurnQueue: [new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal)],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(2),
            Outcome: null,
            Modifiers: BattleModifiers.None)));
    }

    [Test]
    public void TargetOccupiedTest()
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

        engine.CheckCommandError(
            new BattleAttackCommand(summoner.Id, new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.FrontBottom)),
            BattleCommandError.SummonTargetOccupied);
    }

    [Test]
    public void WrongSideTest()
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

        engine.CheckCommandError(
            new BattleAttackCommand(summoner.Id, new BattleAttackTileTarget(BattleSide.Defender, GridPosition.FrontCenter)),
            BattleCommandError.SummonTargetInvalid);
    }

    [Test]
    public void RequiresUnitTargetTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 30, initiative: 50, damage: 5);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 40, damage: 5);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 30);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(attacker, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommandError(
            new BattleAttackCommand(attacker.Id, new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.FrontCenter)),
            BattleCommandError.AttackRequiresUnitTarget);
    }

    [Test]
    public void NotUnitTurnTest()
    {
        UnitDefinition impDefinition = TestHelpers.SimpleUnitDefinition("imp", maxHitpoints: 20, initiative: 35, damage: 8);
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 10, initiative: 50, damage: 5);
        UnitDefinition summonerDefinition = TestHelpers.SimpleUnitDefinition("summoner", maxHitpoints: 30, initiative: 40, damage: 5) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("summoner"), initiative: 40, damage: 5, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Type = UnitAttackType.Summon,
                SummonUnitIds = [impDefinition.Id]
            }
        };

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 10);
        BattleUnit summoner = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, summonerDefinition.Id, BattleSide.Defender, hitpoints: 30);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(impDefinition)
            .WithDefinition(summonerDefinition)
            .WithAttacker(attacker, GridPosition.FrontBottom)
            .WithDefender(summoner, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommandError(
            new BattleAttackCommand(summoner.Id, new BattleAttackTileTarget(BattleSide.Defender, GridPosition.FrontCenter)),
            BattleCommandError.NotUnitTurn);
    }

    [Test]
    public void ReviveUnitUnderSummonTest()
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
        UnitDefinition reviverDefinition = TestHelpers.SimpleUnitDefinition("reviver", maxHitpoints: 30, initiative: 48, damage: 5) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("reviver"), initiative: 48, damage: 0, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Type = UnitAttackType.Revive,
                Heal = 50
            }
        };
        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ally", maxHitpoints: 10, initiative: 45, damage: 5);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 40, damage: 5);

        BattleUnit summoner = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, summonerDefinition.Id, BattleSide.Attacker, hitpoints: 30);
        BattleUnit reviver = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, reviverDefinition.Id, BattleSide.Attacker, hitpoints: 30);
        BattleUnit ally = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, allyDefinition.Id, BattleSide.Attacker, hitpoints: 0);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(3), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(summonerDefinition)
            .WithDefinition(impDefinition)
            .WithDefinition(reviverDefinition)
            .WithDefinition(allyDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(summoner, GridPosition.FrontBottom)
            .WithAttacker(reviver, GridPosition.BackBottom)
            .WithAttacker(ally, GridPosition.FrontCenter)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        _ = engine.DispatchCommand(new BattleAttackCommand(summoner.Id, new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.FrontCenter)));

        engine.CheckCommandError(
            new BattleAttackCommand(reviver.Id, new BattleAttackUnitTarget(ally.Id)),
            BattleCommandError.TargetNotInBattle);
    }

    [Test]
    public void UnsummonedTargetTest()
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
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 40, damage: 30);

        BattleUnit summoner = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, summonerDefinition.Id, BattleSide.Attacker, hitpoints: 30);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(summonerDefinition)
            .WithDefinition(impDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(summoner, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        BattleUnitId summonId = new(2);

        _ = engine.DispatchCommand(new BattleAttackCommand(summoner.Id, new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.FrontCenter)));
        _ = engine.DispatchCommand(new BattleAttackCommand(defender.Id, new BattleAttackUnitTarget(summonId)));

        engine.CheckCommandError(
            new BattleAttackCommand(summoner.Id, new BattleAttackUnitTarget(summonId)),
            BattleCommandError.TargetNotInBattle);
    }

    [Test]
    public void CannotRetreatTest()
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
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 10, damage: 1);

        BattleUnit summoner = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, summonerDefinition.Id, BattleSide.Attacker, hitpoints: 30);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(summonerDefinition)
            .WithDefinition(impDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(summoner, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        BattleUnitId summonId = new(2);

        _ = engine.DispatchCommand(new BattleAttackCommand(summoner.Id, new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.FrontCenter)));
        _ = engine.DispatchCommand(new BattleDefendCommand(defender.Id));
        _ = engine.DispatchCommand(new BattleDefendCommand(summoner.Id));

        BattleUnit summon = new(summonId, null, impDefinition.Id, BattleSide.Attacker, 20, 0, 0, 1, [], [], summoner.Id, [], null, false);

        Assert.That(engine.State, Is.EqualTo(new BattleState(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(summoner with { StatusEffects = [new BattleDefendStatusEffect()] }, GridPosition.FrontBottom, false)
                .PlaceOverlay(summon, GridPosition.FrontCenter, false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(defender with { StatusEffects = [new BattleDefendStatusEffect()] }, GridPosition.FrontBottom, false)
                .Build(),
            CurrentUnitTurn: new BattleUnitTurn(summonId, BattleUnitTurnType.Normal),
            TurnQueue: [new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal)],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(2),
            Outcome: null,
            Modifiers: BattleModifiers.None)));

        engine.CheckCommandError(new BattleRetreatCommand(summonId), BattleCommandError.UnitRetreatNotAllowed);
    }

    [Test]
    public void BaseUnitSurvivesUpdateTest()
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
        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ally", maxHitpoints: 10, initiative: 45, damage: 5);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 40, damage: 30) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("defender"), initiative: 40, damage: 30, accuracy: 100, reach: UnitAttackReach.All)
        };

        BattleUnit summoner = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, summonerDefinition.Id, BattleSide.Attacker, hitpoints: 30);
        BattleUnit ally = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, allyDefinition.Id, BattleSide.Attacker, hitpoints: 0);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(summonerDefinition)
            .WithDefinition(impDefinition)
            .WithDefinition(allyDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(summoner, GridPosition.FrontBottom)
            .WithAttacker(ally, GridPosition.FrontCenter)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        BattleUnitId summonId = new(3);

        _ = engine.DispatchCommand(new BattleAttackCommand(summoner.Id, new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.FrontCenter)));
        engine.CheckCommand(new BattleAttackCommand(defender.Id, new BattleAttackUnitTarget(summonId)), [
            new BattleUnitAttackEvent(defender.Id, new BattleAttackUnitTarget(summonId), [new UnitAttackDefinitionId("defender")]),
            new BattleUnitAttackedEvent(defender.Id, summoner.Id, new UnitAttackDefinitionId("defender")),
            new BattleUnitAttackHitEvent(defender.Id, summoner.Id, new UnitAttackDefinitionId("defender"), 30),
            new BattleUnitDiedEvent(summoner.Id),
            new BattleUnitAttackedEvent(defender.Id, summonId, new UnitAttackDefinitionId("defender")),
            new BattleUnitAttackHitEvent(defender.Id, summonId, new UnitAttackDefinitionId("defender"), 30),
            new BattleUnitDiedEvent(summonId),
            new BattleUnitTurnEndingEvent(defender.Id),
            new BattleUnitUnsummonedEvent(summonId),
            new BattleRoundEndedEvent(),
            new BattleEndedEvent(BattleSide.Defender),
            new BattleUnitExperienceGainedEvent(defender.Id, 1)

        ]);

        Assert.That(engine.State, Is.EqualTo(new BattleState(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(summoner with { Hitpoints = 0 }, GridPosition.FrontBottom, false)
                .Place(ally, GridPosition.FrontCenter, false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(defender with { ExperienceGained = 1 }, GridPosition.FrontBottom, false)
                .Build(),
            CurrentUnitTurn: new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: new BattleOutcome(BattleSide.Defender),
            Modifiers: BattleModifiers.None)));
    }

    [Test]
    public void LargeBaseUnitTwoSummonsTest()
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
        UnitDefinition giantDefinition = TestHelpers.SimpleUnitDefinition("giant", maxHitpoints: 60, initiative: 45, damage: 5) with
        {
            IsLarge = true
        };
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 40, damage: 30);

        BattleUnit summoner = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, summonerDefinition.Id, BattleSide.Attacker, hitpoints: 30);
        BattleUnit giant = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, giantDefinition.Id, BattleSide.Attacker, hitpoints: 0);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(summonerDefinition)
            .WithDefinition(impDefinition)
            .WithDefinition(giantDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(summoner, GridPosition.FrontBottom)
            .WithAttacker(giant, GridPosition.FrontCenter)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        BattleUnitId firstSummonId = new(3);
        BattleUnit firstSummon = new(firstSummonId, null, impDefinition.Id, BattleSide.Attacker, 20, 0, 0, 1, [], [], summoner.Id, [], null, false);
        BattleUnitId secondSummonId = new(4);
        BattleUnit secondSummon = new(secondSummonId, null, impDefinition.Id, BattleSide.Attacker, 20, 0, 0, 1, [], [], summoner.Id, [], null, false);

        _ = engine.DispatchCommand(new BattleAttackCommand(summoner.Id, new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.FrontCenter)));
        _ = engine.DispatchCommand(new BattleDefendCommand(defender.Id));
        _ = engine.DispatchCommand(new BattleAttackCommand(summoner.Id, new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.BackCenter)));

        Assert.That(engine.State.AttackerGrid, Is.EqualTo(new UnitGrid<BattleUnit>.Builder()
            .Place(summoner, GridPosition.FrontBottom, false)
            .Place(giant, GridPosition.FrontCenter, true)
            .PlaceOverlay(firstSummon, GridPosition.FrontCenter, false)
            .PlaceOverlay(secondSummon, GridPosition.BackCenter, false)
            .Build()));

        engine.CheckCommand(new BattleAttackCommand(defender.Id, new BattleAttackUnitTarget(firstSummonId)), [
            new BattleUnitAttackEvent(defender.Id, new BattleAttackUnitTarget(firstSummonId), [new UnitAttackDefinitionId("defender")]),
            new BattleUnitAttackedEvent(defender.Id, firstSummonId, new UnitAttackDefinitionId("defender")),
            new BattleUnitAttackHitEvent(defender.Id, firstSummonId, new UnitAttackDefinitionId("defender"), 30),
            new BattleUnitDiedEvent(firstSummonId),
            new BattleUnitTurnEndingEvent(defender.Id),
            new BattleUnitUnsummonedEvent(firstSummonId),
            new BattleUnitTurnEndedEvent(defender.Id),
            new BattleRoundEndedEvent(),
            new BattleRoundStartedEvent(BattleRound.Normal(3), [new BattleUnitTurn(summoner.Id, BattleUnitTurnType.Normal), new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal), new BattleUnitTurn(secondSummonId, BattleUnitTurnType.Normal)]),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(summoner.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State.AttackerGrid, Is.EqualTo(new UnitGrid<BattleUnit>.Builder()
            .Place(summoner, GridPosition.FrontBottom, false)
            .Place(giant, GridPosition.FrontCenter, true)
            .PlaceOverlay(secondSummon, GridPosition.BackCenter, false)
            .Build()));

        _ = engine.DispatchCommand(new BattleDefendCommand(summoner.Id));

        engine.CheckCommand(new BattleAttackCommand(defender.Id, new BattleAttackUnitTarget(secondSummonId)), [
            new BattleUnitAttackEvent(defender.Id, new BattleAttackUnitTarget(secondSummonId), [new UnitAttackDefinitionId("defender")]),
            new BattleUnitAttackedEvent(defender.Id, secondSummonId, new UnitAttackDefinitionId("defender")),
            new BattleUnitAttackHitEvent(defender.Id, secondSummonId, new UnitAttackDefinitionId("defender"), 30),
            new BattleUnitDiedEvent(secondSummonId),
            new BattleUnitTurnEndingEvent(defender.Id),
            new BattleUnitUnsummonedEvent(secondSummonId),
            new BattleUnitTurnEndedEvent(defender.Id),
            new BattleRoundEndedEvent(),
            new BattleRoundStartedEvent(BattleRound.Normal(4), [new BattleUnitTurn(summoner.Id, BattleUnitTurnType.Normal), new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal)]),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(summoner.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State.AttackerGrid, Is.EqualTo(new UnitGrid<BattleUnit>.Builder()
            .Place(summoner, GridPosition.FrontBottom, false)
            .Place(giant, GridPosition.FrontCenter, true)
            .Build()));
    }

    [Test]
    public void AllReachTest()
    {
        UnitDefinition impDefinition = TestHelpers.SimpleUnitDefinition("imp", maxHitpoints: 20, initiative: 35, damage: 8);
        UnitDefinition summonerDefinition = TestHelpers.SimpleUnitDefinition("summoner", maxHitpoints: 30, initiative: 50, damage: 5) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("summoner"), initiative: 50, damage: 5, accuracy: 100, reach: UnitAttackReach.All) with
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

        BattleUnit frontCenterSummon = new(new BattleUnitId(2), null, impDefinition.Id, BattleSide.Attacker, 20, 0, 0, 1, [], [], summoner.Id, [], null, false);
        BattleUnit frontTopSummon = new(new BattleUnitId(3), null, impDefinition.Id, BattleSide.Attacker, 20, 0, 0, 1, [], [], summoner.Id, [], null, false);
        BattleUnit backBottomSummon = new(new BattleUnitId(4), null, impDefinition.Id, BattleSide.Attacker, 20, 0, 0, 1, [], [], summoner.Id, [], null, false);
        BattleUnit backCenterSummon = new(new BattleUnitId(5), null, impDefinition.Id, BattleSide.Attacker, 20, 0, 0, 1, [], [], summoner.Id, [], null, false);
        BattleUnit backTopSummon = new(new BattleUnitId(6), null, impDefinition.Id, BattleSide.Attacker, 20, 0, 0, 1, [], [], summoner.Id, [], null, false);

        engine.CheckCommand(new BattleAttackCommand(summoner.Id, new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.FrontCenter)), [
            new BattleUnitAttackEvent(summoner.Id, new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.FrontCenter), [new UnitAttackDefinitionId("summoner")]),
            new BattleUnitSummonedEvent(new BattleUnitId(2), impDefinition.Id, BattleSide.Attacker, GridPosition.FrontCenter, 20, 1, summoner.Id),
            new BattleUnitSummonedEvent(new BattleUnitId(3), impDefinition.Id, BattleSide.Attacker, GridPosition.FrontTop, 20, 1, summoner.Id),
            new BattleUnitSummonedEvent(new BattleUnitId(4), impDefinition.Id, BattleSide.Attacker, GridPosition.BackBottom, 20, 1, summoner.Id),
            new BattleUnitSummonedEvent(new BattleUnitId(5), impDefinition.Id, BattleSide.Attacker, GridPosition.BackCenter, 20, 1, summoner.Id),
            new BattleUnitSummonedEvent(new BattleUnitId(6), impDefinition.Id, BattleSide.Attacker, GridPosition.BackTop, 20, 1, summoner.Id),
            new BattleUnitTurnEndingEvent(summoner.Id),
            new BattleUnitTurnEndedEvent(summoner.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(new BattleState(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(summoner, GridPosition.FrontBottom, false)
                .PlaceOverlay(frontCenterSummon, GridPosition.FrontCenter, false)
                .PlaceOverlay(frontTopSummon, GridPosition.FrontTop, false)
                .PlaceOverlay(backBottomSummon, GridPosition.BackBottom, false)
                .PlaceOverlay(backCenterSummon, GridPosition.BackCenter, false)
                .PlaceOverlay(backTopSummon, GridPosition.BackTop, false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder().Place(defender, GridPosition.FrontBottom, false).Build(),
            CurrentUnitTurn: new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: null,
            Modifiers: BattleModifiers.None)));
    }

    [Test]
    public void AllReachOverDeadUnitsTest()
    {
        UnitDefinition impDefinition = TestHelpers.SimpleUnitDefinition("imp", maxHitpoints: 20, initiative: 35, damage: 8);
        UnitDefinition summonerDefinition = TestHelpers.SimpleUnitDefinition("summoner", maxHitpoints: 30, initiative: 50, damage: 5) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("summoner"), initiative: 50, damage: 5, accuracy: 100, reach: UnitAttackReach.All) with
            {
                Type = UnitAttackType.Summon,
                SummonUnitIds = [impDefinition.Id]
            }
        };
        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ally", maxHitpoints: 10, initiative: 45, damage: 5);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 40, damage: 5);

        BattleUnit summoner = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, summonerDefinition.Id, BattleSide.Attacker, hitpoints: 30);
        BattleUnit deadAlly = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, allyDefinition.Id, BattleSide.Attacker, hitpoints: 0);
        BattleUnit livingAlly = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, allyDefinition.Id, BattleSide.Attacker, hitpoints: 10);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(3), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(summonerDefinition)
            .WithDefinition(impDefinition)
            .WithDefinition(allyDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(summoner, GridPosition.FrontBottom)
            .WithAttacker(deadAlly, GridPosition.FrontCenter)
            .WithAttacker(livingAlly, GridPosition.FrontTop)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        BattleUnit frontCenterSummon = new(new BattleUnitId(4), null, impDefinition.Id, BattleSide.Attacker, 20, 0, 0, 1, [], [], summoner.Id, [], null, false);
        BattleUnit backBottomSummon = new(new BattleUnitId(5), null, impDefinition.Id, BattleSide.Attacker, 20, 0, 0, 1, [], [], summoner.Id, [], null, false);
        BattleUnit backCenterSummon = new(new BattleUnitId(6), null, impDefinition.Id, BattleSide.Attacker, 20, 0, 0, 1, [], [], summoner.Id, [], null, false);
        BattleUnit backTopSummon = new(new BattleUnitId(7), null, impDefinition.Id, BattleSide.Attacker, 20, 0, 0, 1, [], [], summoner.Id, [], null, false);

        engine.CheckCommand(new BattleAttackCommand(summoner.Id, new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.BackCenter)), [
            new BattleUnitAttackEvent(summoner.Id, new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.BackCenter), [new UnitAttackDefinitionId("summoner")]),
            new BattleUnitSummonedEvent(new BattleUnitId(4), impDefinition.Id, BattleSide.Attacker, GridPosition.FrontCenter, 20, 1, summoner.Id),
            new BattleUnitSummonedEvent(new BattleUnitId(5), impDefinition.Id, BattleSide.Attacker, GridPosition.BackBottom, 20, 1, summoner.Id),
            new BattleUnitSummonedEvent(new BattleUnitId(6), impDefinition.Id, BattleSide.Attacker, GridPosition.BackCenter, 20, 1, summoner.Id),
            new BattleUnitSummonedEvent(new BattleUnitId(7), impDefinition.Id, BattleSide.Attacker, GridPosition.BackTop, 20, 1, summoner.Id),
            new BattleUnitTurnEndingEvent(summoner.Id),
            new BattleUnitTurnEndedEvent(summoner.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(livingAlly.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(new BattleState(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(summoner, GridPosition.FrontBottom, false)
                .Place(deadAlly, GridPosition.FrontCenter, false)
                .Place(livingAlly, GridPosition.FrontTop, false)
                .PlaceOverlay(frontCenterSummon, GridPosition.FrontCenter, false)
                .PlaceOverlay(backBottomSummon, GridPosition.BackBottom, false)
                .PlaceOverlay(backCenterSummon, GridPosition.BackCenter, false)
                .PlaceOverlay(backTopSummon, GridPosition.BackTop, false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder().Place(defender, GridPosition.FrontBottom, false).Build(),
            CurrentUnitTurn: new BattleUnitTurn(livingAlly.Id, BattleUnitTurnType.Normal),
            TurnQueue: [new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal)],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: null,
            Modifiers: BattleModifiers.None)));
    }

    [Test]
    public void AllReachLargeSummonTest()
    {
        UnitDefinition dragonDefinition = TestHelpers.SimpleUnitDefinition("dragon", maxHitpoints: 60, initiative: 35, damage: 20) with
        {
            IsLarge = true
        };
        UnitDefinition summonerDefinition = TestHelpers.SimpleUnitDefinition("summoner", maxHitpoints: 30, initiative: 50, damage: 5) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("summoner"), initiative: 50, damage: 5, accuracy: 100, reach: UnitAttackReach.All) with
            {
                Type = UnitAttackType.Summon,
                SummonUnitIds = [dragonDefinition.Id]
            }
        };
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 40, damage: 5);

        BattleUnit summoner = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, summonerDefinition.Id, BattleSide.Attacker, hitpoints: 30);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(summonerDefinition)
            .WithDefinition(dragonDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(summoner, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        BattleUnit centerSummon = new(new BattleUnitId(2), null, dragonDefinition.Id, BattleSide.Attacker, 60, 0, 0, 1, [], [], summoner.Id, [], null, false);
        BattleUnit topSummon = new(new BattleUnitId(3), null, dragonDefinition.Id, BattleSide.Attacker, 60, 0, 0, 1, [], [], summoner.Id, [], null, false);

        engine.CheckCommand(new BattleAttackCommand(summoner.Id, new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.FrontCenter)), [
            new BattleUnitAttackEvent(summoner.Id, new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.FrontCenter), [new UnitAttackDefinitionId("summoner")]),
            new BattleUnitSummonedEvent(new BattleUnitId(2), dragonDefinition.Id, BattleSide.Attacker, GridPosition.FrontCenter, 60, 1, summoner.Id),
            new BattleUnitSummonedEvent(new BattleUnitId(3), dragonDefinition.Id, BattleSide.Attacker, GridPosition.FrontTop, 60, 1, summoner.Id),
            new BattleUnitTurnEndingEvent(summoner.Id),
            new BattleUnitTurnEndedEvent(summoner.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(new BattleState(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(summoner, GridPosition.FrontBottom, false)
                .PlaceOverlay(centerSummon, GridPosition.FrontCenter, true)
                .PlaceOverlay(topSummon, GridPosition.FrontTop, true)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder().Place(defender, GridPosition.FrontBottom, false).Build(),
            CurrentUnitTurn: new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: null,
            Modifiers: BattleModifiers.None)));
    }

    [Test]
    public void AnyReachTest()
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

        BattleUnit summon = new(new BattleUnitId(2), null, impDefinition.Id, BattleSide.Attacker, 20, 0, 0, 1, [], [], summoner.Id, [], null, false);

        engine.CheckCommand(new BattleAttackCommand(summoner.Id, new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.BackTop)), [
            new BattleUnitAttackEvent(summoner.Id, new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.BackTop), [new UnitAttackDefinitionId("summoner")]),
            new BattleUnitSummonedEvent(new BattleUnitId(2), impDefinition.Id, BattleSide.Attacker, GridPosition.BackTop, 20, 1, summoner.Id),
            new BattleUnitTurnEndingEvent(summoner.Id),
            new BattleUnitTurnEndedEvent(summoner.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(new BattleState(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(summoner, GridPosition.FrontBottom, false)
                .PlaceOverlay(summon, GridPosition.BackTop, false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder().Place(defender, GridPosition.FrontBottom, false).Build(),
            CurrentUnitTurn: new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: null,
            Modifiers: BattleModifiers.None)));
    }

    [Test]
    public void NonInfiniteAttackTest()
    {
        UnitDefinition impDefinition = TestHelpers.SimpleUnitDefinition("imp", maxHitpoints: 20, initiative: 35, damage: 8);
        UnitDefinition summonerDefinition = TestHelpers.SimpleUnitDefinition("summoner", maxHitpoints: 30, initiative: 50, damage: 5) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("summoner"), initiative: 50, damage: 5, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Type = UnitAttackType.Summon,
                SummonUnitIds = [impDefinition.Id],
                IsInfinite = false
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

        BattleUnitId summonId = new(2);
        BattleUnit summon = new(summonId, null, impDefinition.Id, BattleSide.Attacker, 20, 0, 0, 1, [], [], summoner.Id, [], null, false);

        _ = engine.DispatchCommand(new BattleAttackCommand(summoner.Id, new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.FrontCenter)));

        _ = engine.DispatchCommand(new BattleDefendCommand(defender.Id));
        _ = engine.DispatchCommand(new BattleDefendCommand(summonId));

        _ = engine.DispatchCommand(new BattleDefendCommand(summoner.Id));
        _ = engine.DispatchCommand(new BattleDefendCommand(defender.Id));
        _ = engine.DispatchCommand(new BattleDefendCommand(summonId));

        _ = engine.DispatchCommand(new BattleDefendCommand(summoner.Id));
        _ = engine.DispatchCommand(new BattleDefendCommand(defender.Id));

        Assert.That(engine.State, Is.EqualTo(new BattleState(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(summoner with { StatusEffects = [new BattleDefendStatusEffect()] }, GridPosition.FrontBottom, false)
                .PlaceOverlay(summon, GridPosition.FrontCenter, false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(defender with { StatusEffects = [new BattleDefendStatusEffect()] }, GridPosition.FrontBottom, false)
                .Build(),
            CurrentUnitTurn: new BattleUnitTurn(summonId, BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(3),
            Outcome: null,
            Modifiers: BattleModifiers.None)));
    }

    [Test]
    public void OverUnitKilledThisRoundTest()
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
        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ally", maxHitpoints: 10, initiative: 45, damage: 5);
        UnitDefinition killerDefinition = TestHelpers.SimpleUnitDefinition("killer", maxHitpoints: 30, initiative: 60, damage: 10);

        BattleUnit summoner = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, summonerDefinition.Id, BattleSide.Attacker, hitpoints: 30);
        BattleUnit ally = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, allyDefinition.Id, BattleSide.Attacker, hitpoints: 10);
        BattleUnit killer = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, killerDefinition.Id, BattleSide.Defender, hitpoints: 30);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(summonerDefinition)
            .WithDefinition(impDefinition)
            .WithDefinition(allyDefinition)
            .WithDefinition(killerDefinition)
            .WithAttacker(summoner, GridPosition.FrontBottom)
            .WithAttacker(ally, GridPosition.FrontCenter)
            .WithDefender(killer, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(killer.Id, new BattleAttackUnitTarget(ally.Id)), [
            new BattleUnitAttackEvent(killer.Id, new BattleAttackUnitTarget(ally.Id), [new UnitAttackDefinitionId("killer")]),
            new BattleUnitAttackedEvent(killer.Id, ally.Id, new UnitAttackDefinitionId("killer")),
            new BattleUnitAttackHitEvent(killer.Id, ally.Id, new UnitAttackDefinitionId("killer"), 10),
            new BattleUnitDiedEvent(ally.Id),
            new BattleUnitTurnEndingEvent(killer.Id),
            new BattleUnitTurnEndedEvent(killer.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(summoner.Id, BattleUnitTurnType.Normal))
        ]);

        BattleUnitId summonId = new(3);

        engine.CheckCommand(new BattleAttackCommand(summoner.Id, new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.FrontCenter)), [
            new BattleUnitAttackEvent(summoner.Id, new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.FrontCenter), [new UnitAttackDefinitionId("summoner")]),
            new BattleUnitSummonedEvent(summonId, impDefinition.Id, BattleSide.Attacker, GridPosition.FrontCenter, 20, 1, summoner.Id),
            new BattleUnitTurnEndingEvent(summoner.Id),
            new BattleUnitTurnEndedEvent(summoner.Id),
            new BattleRoundEndedEvent(),
            new BattleRoundStartedEvent(BattleRound.Normal(2), [new BattleUnitTurn(killer.Id, BattleUnitTurnType.Normal), new BattleUnitTurn(summoner.Id, BattleUnitTurnType.Normal), new BattleUnitTurn(summonId, BattleUnitTurnType.Normal)]),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(killer.Id, BattleUnitTurnType.Normal))
        ]);

        BattleUnit summon = new(summonId, null, impDefinition.Id, BattleSide.Attacker, 20, 0, 0, 1, [], [], summoner.Id, [], null, false);

        Assert.That(engine.State, Is.EqualTo(new BattleState(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(summoner, GridPosition.FrontBottom, false)
                .Place(ally with { Hitpoints = 0 }, GridPosition.FrontCenter, false)
                .PlaceOverlay(summon, GridPosition.FrontCenter, false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder().Place(killer, GridPosition.FrontBottom, false).Build(),
            CurrentUnitTurn: new BattleUnitTurn(killer.Id, BattleUnitTurnType.Normal),
            TurnQueue:
            [
                new BattleUnitTurn(summoner.Id, BattleUnitTurnType.Normal),
                new BattleUnitTurn(summonId, BattleUnitTurnType.Normal)
            ],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(2),
            Outcome: null,
            Modifiers: BattleModifiers.None)));
    }

    [Test]
    public void SecondaryAttackAfterUnsummonTest()
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
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 30, initiative: 40, damage: 30) with
        {
            SecondaryAttack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("attacker-paralyze"), initiative: 40, damage: 0, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Type = UnitAttackType.Paralyze
            }
        };

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 30);
        BattleUnit summoner = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, summonerDefinition.Id, BattleSide.Defender, hitpoints: 30);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(impDefinition)
            .WithDefinition(summonerDefinition)
            .WithAttacker(attacker, GridPosition.FrontBottom)
            .WithDefender(summoner, GridPosition.FrontBottom)
            .Build();

        BattleUnitId summonId = new(2);

        engine.CheckCommand(new BattleAttackCommand(summoner.Id, new BattleAttackTileTarget(BattleSide.Defender, GridPosition.FrontCenter)), [
            new BattleUnitAttackEvent(summoner.Id, new BattleAttackTileTarget(BattleSide.Defender, GridPosition.FrontCenter), [new UnitAttackDefinitionId("summoner")]),
            new BattleUnitSummonedEvent(summonId, impDefinition.Id, BattleSide.Defender, GridPosition.FrontCenter, 20, 1, summoner.Id),
            new BattleUnitTurnEndingEvent(summoner.Id),
            new BattleUnitTurnEndedEvent(summoner.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(attacker.Id, BattleUnitTurnType.Normal))
        ]);

        engine.CheckCommand(new BattleAttackCommand(attacker.Id, new BattleAttackUnitTarget(summonId)), [
            new BattleUnitAttackEvent(attacker.Id, new BattleAttackUnitTarget(summonId), [new UnitAttackDefinitionId("attacker"), new UnitAttackDefinitionId("attacker-paralyze")]),
            new BattleUnitAttackedEvent(attacker.Id, summonId, new UnitAttackDefinitionId("attacker")),
            new BattleUnitAttackHitEvent(attacker.Id, summonId, new UnitAttackDefinitionId("attacker"), 30),
            new BattleUnitDiedEvent(summonId),
            new BattleUnitTurnEndingEvent(attacker.Id),
            new BattleUnitUnsummonedEvent(summonId),
            new BattleUnitTurnEndedEvent(attacker.Id),
            new BattleRoundEndedEvent(),
            new BattleRoundStartedEvent(BattleRound.Normal(2), [new BattleUnitTurn(summoner.Id, BattleUnitTurnType.Normal), new BattleUnitTurn(attacker.Id, BattleUnitTurnType.Normal)]),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(summoner.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(new BattleState(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder().Place(attacker, GridPosition.FrontBottom, false).Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder().Place(summoner, GridPosition.FrontBottom, false).Build(),
            CurrentUnitTurn: new BattleUnitTurn(summoner.Id, BattleUnitTurnType.Normal),
            TurnQueue: [new BattleUnitTurn(attacker.Id, BattleUnitTurnType.Normal)],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(2),
            Outcome: null,
            Modifiers: BattleModifiers.None)));
    }

    [Test]
    public void SummonerRetreatTest()
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
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 100, initiative: 40, damage: 1);

        BattleUnit summoner = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, summonerDefinition.Id, BattleSide.Attacker, hitpoints: 30);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(summonerDefinition)
            .WithDefinition(impDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(summoner, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        BattleUnitId summonId = new(2);

        engine.CheckCommand(new BattleAttackCommand(summoner.Id, new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.FrontCenter)), [
            new BattleUnitAttackEvent(summoner.Id, new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.FrontCenter), [new UnitAttackDefinitionId("summoner")]),
            new BattleUnitSummonedEvent(summonId, impDefinition.Id, BattleSide.Attacker, GridPosition.FrontCenter, 20, 1, summoner.Id),
            new BattleUnitTurnEndingEvent(summoner.Id),
            new BattleUnitTurnEndedEvent(summoner.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal))
        ]);

        engine.CheckCommand(new BattleDefendCommand(defender.Id), [
            new BattleUnitDefendedEvent(defender.Id),
            new BattleUnitTurnEndingEvent(defender.Id),
            new BattleUnitTurnEndedEvent(defender.Id),
            new BattleRoundEndedEvent(),
            new BattleRoundStartedEvent(BattleRound.Normal(2), [
                new BattleUnitTurn(summoner.Id, BattleUnitTurnType.Normal),
                new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal),
                new BattleUnitTurn(summonId, BattleUnitTurnType.Normal)
            ]),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(summoner.Id, BattleUnitTurnType.Normal))
        ]);

        engine.CheckCommand(new BattleRetreatCommand(summoner.Id), [
            new BattleUnitRetreatingEvent(summoner.Id),
            new BattleUnitTurnEndingEvent(summoner.Id),
            new BattleUnitTurnEndedEvent(summoner.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal))
        ]);

        engine.CheckCommand(new BattleDefendCommand(defender.Id), [
            new BattleUnitDefendedEvent(defender.Id),
            new BattleUnitTurnEndingEvent(defender.Id),
            new BattleUnitTurnEndedEvent(defender.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(summonId, BattleUnitTurnType.Normal))
        ]);

        engine.CheckCommand(new BattleDefendCommand(summonId), [
            new BattleUnitDefendedEvent(summonId),
            new BattleUnitTurnEndingEvent(summonId),
            new BattleUnitTurnEndedEvent(summonId),
            new BattleRoundEndedEvent(),
            new BattleRoundStartedEvent(BattleRound.Normal(3), [
                new BattleUnitTurn(summoner.Id, BattleUnitTurnType.Normal),
                new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal),
                new BattleUnitTurn(summonId, BattleUnitTurnType.Normal)
            ]),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(summoner.Id, BattleUnitTurnType.Normal)),
            new BattleUnitRetreatedEvent(summoner.Id),
            new BattleUnitTurnEndingEvent(summoner.Id),
            new BattleUnitUnsummonedEvent(summonId),
            new BattleRoundEndedEvent(),
            new BattleEndedEvent(BattleSide.Defender),
            new BattleUnitExperienceGainedEvent(defender.Id, 1)
        ]);

        Assert.That(engine.State, Is.EqualTo(new BattleState(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(summoner with { StatusEffects = [new BattleRetreatedStatusEffect()] }, GridPosition.FrontBottom, false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(defender with { ExperienceGained = 1 }, GridPosition.FrontBottom, false)
                .Build(),
            CurrentUnitTurn: new BattleUnitTurn(summoner.Id, BattleUnitTurnType.Normal),
            TurnQueue: [new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal)],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(3),
            Outcome: new BattleOutcome(BattleSide.Defender),
            Modifiers: BattleModifiers.None)));
    }

    [Test]
    public void SummonerDeathTest()
    {
        UnitDefinition impDefinition = TestHelpers.SimpleUnitDefinition("imp", maxHitpoints: 20, initiative: 55, damage: 8);
        UnitDefinition summonerDefinition = TestHelpers.SimpleUnitDefinition("summoner", maxHitpoints: 30, initiative: 50, damage: 5) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("summoner"), initiative: 50, damage: 5, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Type = UnitAttackType.Summon,
                SummonUnitIds = [impDefinition.Id]
            }
        };
        UnitDefinition killerDefinition = TestHelpers.SimpleUnitDefinition("killer", maxHitpoints: 60, initiative: 45, damage: 30);

        BattleUnit killer = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, killerDefinition.Id, BattleSide.Attacker, hitpoints: 60);
        BattleUnit summoner = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, summonerDefinition.Id, BattleSide.Defender, hitpoints: 30);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(killerDefinition)
            .WithDefinition(summonerDefinition)
            .WithDefinition(impDefinition)
            .WithAttacker(killer, GridPosition.FrontBottom)
            .WithDefender(summoner, GridPosition.FrontBottom)
            .Build();

        BattleUnitId summonId = new(2);

        engine.CheckCommand(new BattleAttackCommand(summoner.Id, new BattleAttackTileTarget(BattleSide.Defender, GridPosition.FrontCenter)), [
            new BattleUnitAttackEvent(summoner.Id, new BattleAttackTileTarget(BattleSide.Defender, GridPosition.FrontCenter), [new UnitAttackDefinitionId("summoner")]),
            new BattleUnitSummonedEvent(summonId, impDefinition.Id, BattleSide.Defender, GridPosition.FrontCenter, 20, 1, summoner.Id),
            new BattleUnitTurnEndingEvent(summoner.Id),
            new BattleUnitTurnEndedEvent(summoner.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(killer.Id, BattleUnitTurnType.Normal))
        ]);

        engine.CheckCommand(new BattleAttackCommand(killer.Id, new BattleAttackUnitTarget(summoner.Id)), [
            new BattleUnitAttackEvent(killer.Id, new BattleAttackUnitTarget(summoner.Id), [new UnitAttackDefinitionId("killer")]),
            new BattleUnitAttackedEvent(killer.Id, summoner.Id, new UnitAttackDefinitionId("killer")),
            new BattleUnitAttackHitEvent(killer.Id, summoner.Id, new UnitAttackDefinitionId("killer"), 30),
            new BattleUnitDiedEvent(summoner.Id),
            new BattleUnitTurnEndingEvent(killer.Id),
            new BattleUnitUnsummonedEvent(summonId),
            new BattleRoundEndedEvent(),
            new BattleEndedEvent(BattleSide.Attacker),
            new BattleUnitExperienceGainedEvent(killer.Id, 1)
        ]);

        Assert.That(engine.State, Is.EqualTo(new BattleState(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder().Place(killer with { ExperienceGained = 1 }, GridPosition.FrontBottom, false).Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder().Place(summoner with { Hitpoints = 0 }, GridPosition.FrontBottom, false).Build(),
            CurrentUnitTurn: new BattleUnitTurn(killer.Id, BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: new BattleOutcome(BattleSide.Attacker),
            Modifiers: BattleModifiers.None)));
    }

    [Test]
    public void SummonerKilledByAllReachTest()
    {
        UnitDefinition impDefinition = TestHelpers.SimpleUnitDefinition("imp", maxHitpoints: 20, initiative: 35, damage: 8);
        UnitDefinition summonerDefinition = TestHelpers.SimpleUnitDefinition("summoner", maxHitpoints: 30, initiative: 60, damage: 5) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("summoner"), initiative: 60, damage: 5, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Type = UnitAttackType.Summon,
                SummonUnitIds = [impDefinition.Id]
            }
        };
        UnitDefinition mageDefinition = TestHelpers.SimpleUnitDefinition("mage", maxHitpoints: 40, initiative: 50, damage: 15) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("mage"), initiative: 50, damage: 15, accuracy: 100, reach: UnitAttackReach.All)
        };

        BattleUnit mage = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, mageDefinition.Id, BattleSide.Attacker, hitpoints: 40);
        BattleUnit summoner = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, summonerDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(mageDefinition)
            .WithDefinition(summonerDefinition)
            .WithDefinition(impDefinition)
            .WithAttacker(mage, GridPosition.FrontBottom)
            .WithDefender(summoner, GridPosition.FrontBottom)
            .Build();

        BattleUnitId summonId = new(2);

        engine.CheckCommand(new BattleAttackCommand(summoner.Id, new BattleAttackTileTarget(BattleSide.Defender, GridPosition.FrontCenter)), [
            new BattleUnitAttackEvent(summoner.Id, new BattleAttackTileTarget(BattleSide.Defender, GridPosition.FrontCenter), [new UnitAttackDefinitionId("summoner")]),
            new BattleUnitSummonedEvent(summonId, impDefinition.Id, BattleSide.Defender, GridPosition.FrontCenter, 20, 1, summoner.Id),
            new BattleUnitTurnEndingEvent(summoner.Id),
            new BattleUnitTurnEndedEvent(summoner.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(mage.Id, BattleUnitTurnType.Normal))
        ]);

        engine.CheckCommand(new BattleAttackCommand(mage.Id, new BattleAttackUnitTarget(summoner.Id)), [
            new BattleUnitAttackEvent(mage.Id, new BattleAttackUnitTarget(summoner.Id), [new UnitAttackDefinitionId("mage")]),
            new BattleUnitAttackedEvent(mage.Id, summoner.Id, new UnitAttackDefinitionId("mage")),
            new BattleUnitAttackHitEvent(mage.Id, summoner.Id, new UnitAttackDefinitionId("mage"), 15),
            new BattleUnitDiedEvent(summoner.Id),
            new BattleUnitAttackedEvent(mage.Id, summonId, new UnitAttackDefinitionId("mage")),
            new BattleUnitAttackHitEvent(mage.Id, summonId, new UnitAttackDefinitionId("mage"), 15),
            new BattleUnitTurnEndingEvent(mage.Id),
            new BattleUnitUnsummonedEvent(summonId),
            new BattleRoundEndedEvent(),
            new BattleEndedEvent(BattleSide.Attacker),
            new BattleUnitExperienceGainedEvent(mage.Id, 1)
        ]);

        Assert.That(engine.State, Is.EqualTo(new BattleState(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder().Place(mage with { ExperienceGained = 1 }, GridPosition.FrontBottom, false).Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder().Place(summoner with { Hitpoints = 0 }, GridPosition.FrontBottom, false).Build(),
            CurrentUnitTurn: new BattleUnitTurn(mage.Id, BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: new BattleOutcome(BattleSide.Attacker),
            Modifiers: BattleModifiers.None)));
    }

    [Test]
    public void ResummonsIntoFreedSlotTest()
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
        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ally", maxHitpoints: 10, initiative: 45, damage: 5);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 100, initiative: 40, damage: 30);

        BattleUnit summoner = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, summonerDefinition.Id, BattleSide.Attacker, hitpoints: 30);
        BattleUnit ally = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, allyDefinition.Id, BattleSide.Attacker, hitpoints: 0);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(summonerDefinition)
            .WithDefinition(impDefinition)
            .WithDefinition(allyDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(summoner, GridPosition.FrontBottom)
            .WithAttacker(ally, GridPosition.FrontCenter)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        BattleUnitId summonId = new(3);
        BattleUnit summon = new(summonId, null, impDefinition.Id, BattleSide.Attacker, 20, 0, 0, 1, [], [], summoner.Id, [], null, false);

        _ = engine.DispatchCommand(new BattleAttackCommand(summoner.Id, new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.FrontCenter)));
        _ = engine.DispatchCommand(new BattleAttackCommand(defender.Id, new BattleAttackUnitTarget(summonId)));

        engine.CheckCommand(new BattleAttackCommand(summoner.Id, new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.FrontCenter)), [
            new BattleUnitAttackEvent(summoner.Id, new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.FrontCenter), [new UnitAttackDefinitionId("summoner")]),
            new BattleUnitSummonedEvent(summonId, impDefinition.Id, BattleSide.Attacker, GridPosition.FrontCenter, 20, 1, summoner.Id),
            new BattleUnitTurnEndingEvent(summoner.Id),
            new BattleUnitTurnEndedEvent(summoner.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(new BattleState(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(summoner, GridPosition.FrontBottom, false)
                .Place(ally, GridPosition.FrontCenter, false)
                .PlaceOverlay(summon, GridPosition.FrontCenter, false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder().Place(defender, GridPosition.FrontBottom, false).Build(),
            CurrentUnitTurn: new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(2),
            Outcome: null,
            Modifiers: BattleModifiers.None)));
    }

    [Test]
    public void SummonKillsLastEnemyTest()
    {
        UnitDefinition impDefinition = TestHelpers.SimpleUnitDefinition("imp", maxHitpoints: 20, initiative: 55, damage: 25);
        UnitDefinition summonerDefinition = TestHelpers.SimpleUnitDefinition("summoner", maxHitpoints: 30, initiative: 50, damage: 5) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("summoner"), initiative: 50, damage: 5, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Type = UnitAttackType.Summon,
                SummonUnitIds = [impDefinition.Id]
            }
        };
        UnitDefinition victimDefinition = TestHelpers.SimpleUnitDefinition("victim", maxHitpoints: 20, initiative: 40, damage: 1);

        BattleUnit summoner = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, summonerDefinition.Id, BattleSide.Attacker, hitpoints: 30);
        BattleUnit victim = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, victimDefinition.Id, BattleSide.Defender, hitpoints: 20);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(summonerDefinition)
            .WithDefinition(impDefinition)
            .WithDefinition(victimDefinition)
            .WithAttacker(summoner, GridPosition.FrontBottom)
            .WithDefender(victim, GridPosition.FrontBottom)
            .Build();

        BattleUnitId summonId = new(2);

        engine.CheckCommand(new BattleAttackCommand(summoner.Id, new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.FrontCenter)), [
            new BattleUnitAttackEvent(summoner.Id, new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.FrontCenter), [new UnitAttackDefinitionId("summoner")]),
            new BattleUnitSummonedEvent(summonId, impDefinition.Id, BattleSide.Attacker, GridPosition.FrontCenter, 20, 1, summoner.Id),
            new BattleUnitTurnEndingEvent(summoner.Id),
            new BattleUnitTurnEndedEvent(summoner.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(victim.Id, BattleUnitTurnType.Normal))
        ]);

        engine.CheckCommand(new BattleSkipCommand(victim.Id), [
            new BattleUnitTurnSkippedEvent(victim.Id),
            new BattleUnitTurnEndingEvent(victim.Id),
            new BattleUnitTurnEndedEvent(victim.Id),
            new BattleRoundEndedEvent(),
            new BattleRoundStartedEvent(BattleRound.Normal(2), [
                new BattleUnitTurn(summonId, BattleUnitTurnType.Normal),
                new BattleUnitTurn(summoner.Id, BattleUnitTurnType.Normal),
                new BattleUnitTurn(victim.Id, BattleUnitTurnType.Normal)
            ]),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(summonId, BattleUnitTurnType.Normal))
        ]);

        engine.CheckCommand(new BattleAttackCommand(summonId, new BattleAttackUnitTarget(victim.Id)), [
            new BattleUnitAttackEvent(summonId, new BattleAttackUnitTarget(victim.Id), [new UnitAttackDefinitionId("imp")]),
            new BattleUnitAttackedEvent(summonId, victim.Id, new UnitAttackDefinitionId("imp")),
            new BattleUnitAttackHitEvent(summonId, victim.Id, new UnitAttackDefinitionId("imp"), 25),
            new BattleUnitDiedEvent(victim.Id),
            new BattleUnitTurnEndingEvent(summonId),
            new BattleUnitUnsummonedEvent(summonId),
            new BattleRoundEndedEvent(),
            new BattleEndedEvent(BattleSide.Attacker),
            new BattleUnitExperienceGainedEvent(summoner.Id, 1)
        ]);

        Assert.That(engine.State, Is.EqualTo(new BattleState(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(summoner with { ExperienceGained = 1 }, GridPosition.FrontBottom, false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(victim with { Hitpoints = 0 }, GridPosition.FrontBottom, false)
                .Build(),
            CurrentUnitTurn: new BattleUnitTurn(summonId, BattleUnitTurnType.Normal),
            TurnQueue: [new BattleUnitTurn(summoner.Id, BattleUnitTurnType.Normal), new BattleUnitTurn(victim.Id, BattleUnitTurnType.Normal)],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(2),
            Outcome: new BattleOutcome(BattleSide.Attacker),
            Modifiers: BattleModifiers.None)));
    }

    [Test]
    public void SummonerDeathWithPoisonedKillerTest()
    {
        UnitDefinition impDefinition = TestHelpers.SimpleUnitDefinition("imp", maxHitpoints: 20, initiative: 55, damage: 8);
        UnitDefinition summonerDefinition = TestHelpers.SimpleUnitDefinition("summoner", maxHitpoints: 30, initiative: 50, damage: 5) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("summoner"), initiative: 50, damage: 5, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Type = UnitAttackType.Summon,
                SummonUnitIds = [impDefinition.Id]
            }
        };
        UnitDefinition killerDefinition = TestHelpers.SimpleUnitDefinition("killer", maxHitpoints: 60, initiative: 45, damage: 30);

        BattleUnit killer = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, killerDefinition.Id, BattleSide.Attacker, hitpoints: 60) with
        {
            StatusEffects = [new BattlePoisonStatusEffect(3, 5)]
        };
        BattleUnit summoner = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, summonerDefinition.Id, BattleSide.Defender, hitpoints: 30);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(killerDefinition)
            .WithDefinition(summonerDefinition)
            .WithDefinition(impDefinition)
            .WithAttacker(killer, GridPosition.FrontBottom)
            .WithDefender(summoner, GridPosition.FrontBottom)
            .Build();

        BattleUnitId summonId = new(2);

        _ = engine.DispatchCommand(new BattleAttackCommand(summoner.Id, new BattleAttackTileTarget(BattleSide.Defender, GridPosition.FrontCenter)));

        engine.CheckCommand(new BattleAttackCommand(killer.Id, new BattleAttackUnitTarget(summoner.Id)), [
            new BattleUnitAttackEvent(killer.Id, new BattleAttackUnitTarget(summoner.Id), [new UnitAttackDefinitionId("killer")]),
            new BattleUnitAttackedEvent(killer.Id, summoner.Id, new UnitAttackDefinitionId("killer")),
            new BattleUnitAttackHitEvent(killer.Id, summoner.Id, new UnitAttackDefinitionId("killer"), 30),
            new BattleUnitDiedEvent(summoner.Id),
            new BattleUnitTurnEndingEvent(killer.Id),
            new BattleUnitUnsummonedEvent(summonId),
            new BattleRoundEndedEvent(),
            new BattleEndedEvent(BattleSide.Attacker),
            new BattleUnitExperienceGainedEvent(killer.Id, 1)
        ]);

        Assert.That(engine.State, Is.EqualTo(new BattleState(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(killer with { Hitpoints = 55, StatusEffects = [], ExperienceGained = 1 }, GridPosition.FrontBottom, false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder().Place(summoner with { Hitpoints = 0 }, GridPosition.FrontBottom, false).Build(),
            CurrentUnitTurn: new BattleUnitTurn(killer.Id, BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: new BattleOutcome(BattleSide.Attacker),
            Modifiers: BattleModifiers.None)));
    }

    [Test]
    public void SummonerDeathWithRetreatingUnitTest()
    {
        UnitDefinition impDefinition = TestHelpers.SimpleUnitDefinition("imp", maxHitpoints: 20, initiative: 35, damage: 8);
        UnitDefinition summonerDefinition = TestHelpers.SimpleUnitDefinition("summoner", maxHitpoints: 30, initiative: 60, damage: 5) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("summoner"), initiative: 60, damage: 5, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Type = UnitAttackType.Summon,
                SummonUnitIds = [impDefinition.Id]
            }
        };
        UnitDefinition runnerDefinition = TestHelpers.SimpleUnitDefinition("runner", maxHitpoints: 40, initiative: 50, damage: 5);
        UnitDefinition killerDefinition = TestHelpers.SimpleUnitDefinition("killer", maxHitpoints: 60, initiative: 45, damage: 30);

        BattleUnit runner = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, runnerDefinition.Id, BattleSide.Attacker, hitpoints: 40);
        BattleUnit killer = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, killerDefinition.Id, BattleSide.Attacker, hitpoints: 60);
        BattleUnit summoner = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, summonerDefinition.Id, BattleSide.Defender, hitpoints: 30);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(runnerDefinition)
            .WithDefinition(killerDefinition)
            .WithDefinition(summonerDefinition)
            .WithDefinition(impDefinition)
            .WithAttacker(runner, GridPosition.BackBottom)
            .WithAttacker(killer, GridPosition.FrontBottom)
            .WithDefender(summoner, GridPosition.FrontBottom)
            .Build();

        BattleUnitId summonId = new(3);

        _ = engine.DispatchCommand(new BattleAttackCommand(summoner.Id, new BattleAttackTileTarget(BattleSide.Defender, GridPosition.FrontCenter)));
        _ = engine.DispatchCommand(new BattleRetreatCommand(runner.Id));

        engine.CheckCommand(new BattleAttackCommand(killer.Id, new BattleAttackUnitTarget(summoner.Id)), [
            new BattleUnitAttackEvent(killer.Id, new BattleAttackUnitTarget(summoner.Id), [new UnitAttackDefinitionId("killer")]),
            new BattleUnitAttackedEvent(killer.Id, summoner.Id, new UnitAttackDefinitionId("killer")),
            new BattleUnitAttackHitEvent(killer.Id, summoner.Id, new UnitAttackDefinitionId("killer"), 30),
            new BattleUnitDiedEvent(summoner.Id),
            new BattleUnitTurnEndingEvent(killer.Id),
            new BattleUnitUnsummonedEvent(summonId),
            new BattleRoundEndedEvent(),
            new BattleEndedEvent(BattleSide.Attacker),
            new BattleUnitExperienceGainedEvent(killer.Id, 1),
            new BattleUnitExperienceGainedEvent(runner.Id, 1)
        ]);

        Assert.That(engine.State, Is.EqualTo(new BattleState(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(runner with { ExperienceGained = 1 }, GridPosition.BackBottom, false)
                .Place(killer with { ExperienceGained = 1 }, GridPosition.FrontBottom, false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder().Place(summoner with { Hitpoints = 0 }, GridPosition.FrontBottom, false).Build(),
            CurrentUnitTurn: new BattleUnitTurn(killer.Id, BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: new BattleOutcome(BattleSide.Attacker),
            Modifiers: BattleModifiers.None)));
    }

    [Test]
    public void SummonerDeathWithRecoveryRoundTest()
    {
        UnitDefinition impDefinition = TestHelpers.SimpleUnitDefinition("imp", maxHitpoints: 20, initiative: 55, damage: 8);
        UnitDefinition summonerDefinition = TestHelpers.SimpleUnitDefinition("summoner", maxHitpoints: 30, initiative: 50, damage: 5) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("summoner"), initiative: 50, damage: 5, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Type = UnitAttackType.Summon,
                SummonUnitIds = [impDefinition.Id]
            }
        };
        UnitDefinition clericDefinition = TestHelpers.SimpleUnitDefinition("cleric", maxHitpoints: 100, initiative: 47, damage: 0) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("cleric"), initiative: 47, damage: 0, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Type = UnitAttackType.Heal,
                Heal = 30
            }
        };
        UnitDefinition killerDefinition = TestHelpers.SimpleUnitDefinition("killer", maxHitpoints: 60, initiative: 45, damage: 30);

        BattleUnit cleric = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, clericDefinition.Id, BattleSide.Attacker, hitpoints: 100) with
        {
            StatusEffects = [new BattlePoisonStatusEffect(3, 5)]
        };
        BattleUnit killer = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, killerDefinition.Id, BattleSide.Attacker, hitpoints: 60);
        BattleUnit summoner = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, summonerDefinition.Id, BattleSide.Defender, hitpoints: 30);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(clericDefinition)
            .WithDefinition(killerDefinition)
            .WithDefinition(summonerDefinition)
            .WithDefinition(impDefinition)
            .WithAttacker(cleric, GridPosition.BackBottom)
            .WithAttacker(killer, GridPosition.FrontBottom)
            .WithDefender(summoner, GridPosition.FrontBottom)
            .Build();

        BattleUnitId summonId = new(3);

        _ = engine.DispatchCommand(new BattleAttackCommand(summoner.Id, new BattleAttackTileTarget(BattleSide.Defender, GridPosition.FrontCenter)));
        _ = engine.DispatchCommand(new BattleDefendCommand(cleric.Id));

        engine.CheckCommand(new BattleAttackCommand(killer.Id, new BattleAttackUnitTarget(summoner.Id)), [
            new BattleUnitAttackEvent(killer.Id, new BattleAttackUnitTarget(summoner.Id), [new UnitAttackDefinitionId("killer")]),
            new BattleUnitAttackedEvent(killer.Id, summoner.Id, new UnitAttackDefinitionId("killer")),
            new BattleUnitAttackHitEvent(killer.Id, summoner.Id, new UnitAttackDefinitionId("killer"), 30),
            new BattleUnitDiedEvent(summoner.Id),
            new BattleUnitTurnEndingEvent(killer.Id),
            new BattleUnitUnsummonedEvent(summonId),
            new BattleRoundEndedEvent(),
            new BattleRoundStartedEvent(BattleRound.Recovery(1), [new BattleUnitTurn(cleric.Id, BattleUnitTurnType.Normal)]),
            new BattleUnitTurnEndedEvent(killer.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(cleric.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(new BattleState(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(cleric with { Hitpoints = 95, StatusEffects = [] }, GridPosition.BackBottom, false)
                .Place(killer, GridPosition.FrontBottom, false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder().Place(summoner with { Hitpoints = 0 }, GridPosition.FrontBottom, false).Build(),
            CurrentUnitTurn: new BattleUnitTurn(cleric.Id, BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Recovery(1),
            Outcome: null,
            Modifiers: BattleModifiers.None)));
    }

    [Test]
    public void AllReachMixedSizesTest()
    {
        UnitDefinition impDefinition = TestHelpers.SimpleUnitDefinition("imp", maxHitpoints: 20, initiative: 35, damage: 8);
        UnitDefinition dragonDefinition = TestHelpers.SimpleUnitDefinition("dragon", maxHitpoints: 60, initiative: 35, damage: 20) with
        {
            IsLarge = true
        };
        UnitDefinition summonerDefinition = TestHelpers.SimpleUnitDefinition("summoner", maxHitpoints: 30, initiative: 50, damage: 5) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("summoner"), initiative: 50, damage: 5, accuracy: 100, reach: UnitAttackReach.All) with
            {
                Type = UnitAttackType.Summon,
                SummonUnitIds = [impDefinition.Id, dragonDefinition.Id]
            }
        };
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 40, damage: 5);

        BattleUnit summoner = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, summonerDefinition.Id, BattleSide.Attacker, hitpoints: 30);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(summonerDefinition)
            .WithDefinition(impDefinition)
            .WithDefinition(dragonDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(summoner, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        BattleUnit centerSummon = new(new BattleUnitId(2), null, dragonDefinition.Id, BattleSide.Attacker, 60, 0, 0, 1, [], [], summoner.Id, [], null, false);
        BattleUnit topSummon = new(new BattleUnitId(3), null, dragonDefinition.Id, BattleSide.Attacker, 60, 0, 0, 1, [], [], summoner.Id, [], null, false);
        BattleUnit backSummon = new(new BattleUnitId(4), null, impDefinition.Id, BattleSide.Attacker, 20, 0, 0, 1, [], [], summoner.Id, [], null, false);

        engine.CheckCommand(new BattleAttackCommand(summoner.Id, new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.FrontCenter)), [
            new BattleUnitAttackEvent(summoner.Id, new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.FrontCenter), [new UnitAttackDefinitionId("summoner")]),
            new BattleUnitSummonedEvent(new BattleUnitId(2), dragonDefinition.Id, BattleSide.Attacker, GridPosition.FrontCenter, 60, 1, summoner.Id),
            new BattleUnitSummonedEvent(new BattleUnitId(3), dragonDefinition.Id, BattleSide.Attacker, GridPosition.FrontTop, 60, 1, summoner.Id),
            new BattleUnitSummonedEvent(new BattleUnitId(4), impDefinition.Id, BattleSide.Attacker, GridPosition.BackBottom, 20, 1, summoner.Id),
            new BattleUnitTurnEndingEvent(summoner.Id),
            new BattleUnitTurnEndedEvent(summoner.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(new BattleState(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(summoner, GridPosition.FrontBottom, false)
                .PlaceOverlay(centerSummon, GridPosition.FrontCenter, true)
                .PlaceOverlay(topSummon, GridPosition.FrontTop, true)
                .PlaceOverlay(backSummon, GridPosition.BackBottom, false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder().Place(defender, GridPosition.FrontBottom, false).Build(),
            CurrentUnitTurn: new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: null,
            Modifiers: BattleModifiers.None)));
    }
}
