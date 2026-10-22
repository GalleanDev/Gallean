namespace Gallean.Engine.Tests;

public sealed class BattleEngineFearAttackTests
{
    private static UnitDefinition FearDefinition(string id, int initiative)
    {
        return TestHelpers.SimpleUnitDefinition(id, maxHitpoints: 100, initiative: initiative, damage: 0) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId(id), initiative: initiative, damage: 0, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Type = UnitAttackType.Fear
            }
        };
    }

    [Test]
    public void SimpleTest()
    {
        UnitDefinition warriorDefinition = TestHelpers.SimpleUnitDefinition("warrior", maxHitpoints: 100, initiative: 60, damage: 5);
        UnitDefinition bansheeDefinition = FearDefinition("banshee", initiative: 50);

        BattleUnit warrior = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, warriorDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit banshee = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, bansheeDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(warriorDefinition)
            .WithDefinition(bansheeDefinition)
            .WithAttacker(warrior, GridPosition.FrontBottom)
            .WithDefender(banshee, GridPosition.FrontBottom)
            .Build();

        _ = engine.DispatchCommand(new BattleAttackCommand(warrior.Id, new BattleAttackUnitTarget(banshee.Id)));

        engine.CheckCommand(new BattleAttackCommand(banshee.Id, new BattleAttackUnitTarget(warrior.Id)), [
            new BattleUnitAttackEvent(banshee.Id, new BattleAttackUnitTarget(warrior.Id), [new UnitAttackDefinitionId("banshee")]),
            new BattleUnitAttackedEvent(banshee.Id, warrior.Id, new UnitAttackDefinitionId("banshee")),
            new BattleUnitAttackHitEvent(banshee.Id, warrior.Id, new UnitAttackDefinitionId("banshee"), 0),
            new BattleUnitRetreatingEvent(warrior.Id),
            new BattleUnitTurnEndingEvent(banshee.Id),
            new BattleUnitTurnEndedEvent(banshee.Id),
            new BattleRoundEndedEvent(),
            new BattleRoundStartedEvent(BattleRound.Normal(2), [new BattleUnitTurn(warrior.Id, BattleUnitTurnType.Normal), new BattleUnitTurn(banshee.Id, BattleUnitTurnType.Normal)]),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(warrior.Id, BattleUnitTurnType.Normal)),
            new BattleUnitRetreatedEvent(warrior.Id),
            new BattleUnitTurnEndingEvent(warrior.Id),
            new BattleRoundEndedEvent(),
            new BattleEndedEvent(BattleSide.Defender),
            new BattleUnitExperienceGainedEvent(banshee.Id, 1)
        ]);

        BattleState expected = engine.State
                .WithUnit(warrior with { StatusEffects = [new BattleRetreatedStatusEffect()] })
                .WithUnit(banshee with { Hitpoints = 95, ExperienceGained = 1 })
            with
            {
                CurrentRound = BattleRound.Normal(2),
                Outcome = new BattleOutcome(BattleSide.Defender)
            };

        Assert.That(engine.State, Is.EqualTo(expected));
    }

    [Test]
    public void DefenderTest()
    {
        UnitDefinition dreadlordDefinition = FearDefinition("dreadlord", initiative: 60);
        UnitDefinition golemDefinition = TestHelpers.SimpleUnitDefinition("golem", maxHitpoints: 100, initiative: 40, damage: 2);

        BattleUnit dreadlord = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, dreadlordDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit golem = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, golemDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(dreadlordDefinition)
            .WithDefinition(golemDefinition)
            .WithAttacker(dreadlord, GridPosition.FrontBottom)
            .WithDefender(golem, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(dreadlord.Id, new BattleAttackUnitTarget(golem.Id)), [
            new BattleUnitAttackEvent(dreadlord.Id, new BattleAttackUnitTarget(golem.Id), [new UnitAttackDefinitionId("dreadlord")]),
            new BattleUnitAttackedEvent(dreadlord.Id, golem.Id, new UnitAttackDefinitionId("dreadlord")),
            new BattleUnitAttackHitEvent(dreadlord.Id, golem.Id, new UnitAttackDefinitionId("dreadlord"), 0),
            new BattleUnitTurnEndingEvent(dreadlord.Id),
            new BattleUnitTurnEndedEvent(dreadlord.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(golem.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(dreadlord, GridPosition.FrontBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(golem, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(golem.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(1)
            }));
    }

    [Test]
    public void AlreadyRetreatingTest()
    {
        UnitDefinition warriorDefinition = TestHelpers.SimpleUnitDefinition("warrior", maxHitpoints: 100, initiative: 60, damage: 5);
        UnitDefinition bansheeDefinition = FearDefinition("banshee", initiative: 50);

        BattleUnit warrior = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, warriorDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit banshee = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, bansheeDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(warriorDefinition)
            .WithDefinition(bansheeDefinition)
            .WithAttacker(warrior, GridPosition.FrontBottom)
            .WithDefender(banshee, GridPosition.FrontBottom)
            .Build();

        _ = engine.DispatchCommand(new BattleRetreatCommand(warrior.Id));

        engine.CheckCommandError(
            new BattleAttackCommand(banshee.Id, new BattleAttackUnitTarget(warrior.Id)),
            BattleCommandError.TargetIncorrect);
    }

    [Test]
    public void SummonTest()
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
        UnitDefinition bansheeDefinition = FearDefinition("banshee", initiative: 50);

        BattleUnit summoner = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, summonerDefinition.Id, BattleSide.Attacker, hitpoints: 30);
        BattleUnit banshee = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, bansheeDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(summonerDefinition)
            .WithDefinition(impDefinition)
            .WithDefinition(bansheeDefinition)
            .WithAttacker(summoner, GridPosition.FrontBottom)
            .WithDefender(banshee, GridPosition.FrontBottom)
            .Build();

        BattleUnitId summonId = new(2);

        _ = engine.DispatchCommand(new BattleAttackCommand(summoner.Id, new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.FrontCenter)));

        engine.CheckCommand(new BattleAttackCommand(banshee.Id, new BattleAttackUnitTarget(summonId)), [
            new BattleUnitAttackEvent(banshee.Id, new BattleAttackUnitTarget(summonId), [new UnitAttackDefinitionId("banshee")]),
            new BattleUnitAttackedEvent(banshee.Id, summonId, new UnitAttackDefinitionId("banshee")),
            new BattleUnitAttackHitEvent(banshee.Id, summonId, new UnitAttackDefinitionId("banshee"), 0),
            new BattleUnitRetreatingEvent(summonId),
            new BattleUnitTurnEndingEvent(banshee.Id),
            new BattleUnitTurnEndedEvent(banshee.Id),
            new BattleRoundEndedEvent(),
            new BattleRoundStartedEvent(BattleRound.Normal(2), [
                new BattleUnitTurn(summoner.Id, BattleUnitTurnType.Normal),
                new BattleUnitTurn(banshee.Id, BattleUnitTurnType.Normal),
                new BattleUnitTurn(summonId, BattleUnitTurnType.Normal)
            ]),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(summoner.Id, BattleUnitTurnType.Normal))
        ]);

        _ = engine.DispatchCommand(new BattleDefendCommand(summoner.Id));

        engine.CheckCommand(new BattleDefendCommand(banshee.Id), [
            new BattleUnitDefendedEvent(banshee.Id),
            new BattleUnitTurnEndingEvent(banshee.Id),
            new BattleUnitTurnEndedEvent(banshee.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(summonId, BattleUnitTurnType.Normal)),
            new BattleUnitUnsummonedEvent(summonId),
            new BattleUnitTurnEndingEvent(summonId),
            new BattleUnitTurnEndedEvent(summonId),
            new BattleRoundEndedEvent(),
            new BattleRoundStartedEvent(BattleRound.Normal(3), [
                new BattleUnitTurn(summoner.Id, BattleUnitTurnType.Normal),
                new BattleUnitTurn(banshee.Id, BattleUnitTurnType.Normal)
            ]),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(summoner.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(new BattleState(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(summoner, GridPosition.FrontBottom, false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(banshee with { StatusEffects = [new BattleDefendStatusEffect()] }, GridPosition.FrontBottom, false)
                .Build(),
            CurrentUnitTurn: new BattleUnitTurn(summoner.Id, BattleUnitTurnType.Normal),
            TurnQueue: [new BattleUnitTurn(banshee.Id, BattleUnitTurnType.Normal)],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(3),
            Outcome: null,
            Modifiers: BattleModifiers.None)));
    }

    [Test]
    public void SummonerDeathTest()
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
        UnitDefinition bansheeDefinition = FearDefinition("banshee", initiative: 50);
        UnitDefinition killerDefinition = TestHelpers.SimpleUnitDefinition("killer", maxHitpoints: 60, initiative: 40, damage: 30);

        BattleUnit summoner = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, summonerDefinition.Id, BattleSide.Attacker, hitpoints: 30);
        BattleUnit banshee = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, bansheeDefinition.Id, BattleSide.Defender, hitpoints: 100);
        BattleUnit killer = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, killerDefinition.Id, BattleSide.Defender, hitpoints: 60);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(summonerDefinition)
            .WithDefinition(impDefinition)
            .WithDefinition(bansheeDefinition)
            .WithDefinition(killerDefinition)
            .WithAttacker(summoner, GridPosition.FrontBottom)
            .WithDefender(banshee, GridPosition.FrontBottom)
            .WithDefender(killer, GridPosition.FrontTop)
            .Build();

        BattleUnitId summonId = new(3);

        _ = engine.DispatchCommand(new BattleAttackCommand(summoner.Id, new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.FrontCenter)));
        _ = engine.DispatchCommand(new BattleAttackCommand(banshee.Id, new BattleAttackUnitTarget(summonId)));

        engine.CheckCommand(new BattleAttackCommand(killer.Id, new BattleAttackUnitTarget(summoner.Id)), [
            new BattleUnitAttackEvent(killer.Id, new BattleAttackUnitTarget(summoner.Id), [new UnitAttackDefinitionId("killer")]),
            new BattleUnitAttackedEvent(killer.Id, summoner.Id, new UnitAttackDefinitionId("killer")),
            new BattleUnitAttackHitEvent(killer.Id, summoner.Id, new UnitAttackDefinitionId("killer"), 30),
            new BattleUnitDiedEvent(summoner.Id),
            new BattleUnitTurnEndingEvent(killer.Id),
            new BattleUnitUnsummonedEvent(summonId),
            new BattleRoundEndedEvent(),
            new BattleEndedEvent(BattleSide.Defender),
            new BattleUnitExperienceGainedEvent(banshee.Id, 1),
            new BattleUnitExperienceGainedEvent(killer.Id, 1)
        ]);

        Assert.That(engine.State, Is.EqualTo(new BattleState(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(summoner with { Hitpoints = 0 }, GridPosition.FrontBottom, false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(banshee with { ExperienceGained = 1 }, GridPosition.FrontBottom, false)
                .Place(killer with { ExperienceGained = 1 }, GridPosition.FrontTop, false)
                .Build(),
            CurrentUnitTurn: new BattleUnitTurn(killer.Id, BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: new BattleOutcome(BattleSide.Defender),
            Modifiers: BattleModifiers.None)));
    }
}
