namespace Gallean.Engine.Tests;

public sealed class BattleEngineTransformSelfAttackTests
{
    private static UnitDefinition TransformSelfDefinition(
        string id,
        int maxHitpoints,
        UnitDefinitionId? smallTransformUnitId,
        UnitDefinitionId? largeTransformUnitId)
    {
        return TestHelpers.SimpleUnitDefinition(id, maxHitpoints, initiative: 50, damage: 0) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId(id + "-transform"), initiative: 50, damage: 0, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Type = UnitAttackType.TransformSelf,
                Source = UnitAttackSource.Mind,
                IsInfinite = true,
                SmallTransformUnitId = smallTransformUnitId,
                LargeTransformUnitId = largeTransformUnitId
            }
        };
    }

    [Test]
    public void TransformTest()
    {
        UnitDefinition beastDefinition = TestHelpers.SimpleUnitDefinition("beast", maxHitpoints: 50, initiative: 50, damage: 9);
        UnitDefinition attackerDefinition = TransformSelfDefinition("attacker", maxHitpoints: 100, beastDefinition.Id, null);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 100, initiative: 40, damage: 5);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 80);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(beastDefinition)
            .WithAttacker(attacker, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(attacker.Id, new BattleAttackUnitTarget(attacker.Id)),
        [
            new BattleUnitAttackEvent(attacker.Id, new BattleAttackUnitTarget(attacker.Id), [attackerDefinition.Attack.Id]),
            new BattleUnitTransformAppliedEvent(attacker.Id, attacker.Id, BattleTransformType.TransformSelf, beastDefinition.Id, 1, 40, 50, null)
        ]);

        BattleUnit transformed = attacker with
        {
            DefinitionId = beastDefinition.Id,
            Hitpoints = 40,
            StatusEffects = [new BattleTransformStatusEffect(BattleTransformType.TransformSelf, attackerDefinition.Id, 1, [], null)]
        };

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(transformed, GridPosition.FrontBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(defender, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(attacker.Id, BattleUnitTurnType.Normal)
            }));
    }

    [Test]
    public void EnemyTargetTest()
    {
        UnitDefinition beastDefinition = TestHelpers.SimpleUnitDefinition("beast", maxHitpoints: 50, initiative: 50, damage: 9);
        UnitDefinition attackerDefinition = TransformSelfDefinition("attacker", maxHitpoints: 100, beastDefinition.Id, null);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 100, initiative: 40, damage: 5);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 80);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(beastDefinition)
            .WithAttacker(attacker, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommandError(
            new BattleAttackCommand(attacker.Id, new BattleAttackUnitTarget(defender.Id)),
            BattleCommandError.CanOnlyTransformSelf
        );
    }

    [Test]
    public void AllyTargetTest()
    {
        UnitDefinition beastDefinition = TestHelpers.SimpleUnitDefinition("beast", maxHitpoints: 50, initiative: 50, damage: 9);
        UnitDefinition attackerDefinition = TransformSelfDefinition("attacker", maxHitpoints: 100, beastDefinition.Id, null);
        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ally", maxHitpoints: 100, initiative: 30, damage: 5);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 100, initiative: 40, damage: 5);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 80);
        BattleUnit ally = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, allyDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(allyDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(beastDefinition)
            .WithAttacker(attacker, GridPosition.FrontBottom)
            .WithAttacker(ally, GridPosition.FrontCenter)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommandError(
            new BattleAttackCommand(attacker.Id, new BattleAttackUnitTarget(ally.Id)),
            BattleCommandError.CanOnlyTransformSelf
        );
    }

    [Test]
    public void DurationTest()
    {
        UnitDefinition beastDefinition = TestHelpers.SimpleUnitDefinition("beast", maxHitpoints: 50, initiative: 50, damage: 9);
        UnitDefinition attackerDefinition = TransformSelfDefinition("attacker", maxHitpoints: 100, beastDefinition.Id, null);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 100, initiative: 40, damage: 5);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 80);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(beastDefinition)
            .WithAttacker(attacker, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        _ = engine.DispatchCommand(new BattleAttackCommand(attacker.Id, new BattleAttackUnitTarget(attacker.Id)));
        _ = engine.DispatchCommand(new BattleSkipCommand(attacker.Id));
        _ = engine.DispatchCommand(new BattleSkipCommand(defender.Id));
        _ = engine.DispatchCommand(new BattleSkipCommand(attacker.Id));

        engine.CheckCommand(new BattleSkipCommand(defender.Id),
        [
            new BattleUnitTurnSkippedEvent(defender.Id),
            new BattleUnitTurnEndingEvent(defender.Id),
            new BattleUnitTurnEndedEvent(defender.Id),
            new BattleRoundEndedEvent(),
            new BattleRoundStartedEvent(BattleRound.Normal(3), [new BattleUnitTurn(attacker.Id, BattleUnitTurnType.Normal), new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal)]),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(attacker.Id, BattleUnitTurnType.Normal))
        ]);

        BattleUnit transformed = attacker with
        {
            DefinitionId = beastDefinition.Id,
            Hitpoints = 40,
            StatusEffects = [new BattleTransformStatusEffect(BattleTransformType.TransformSelf, attackerDefinition.Id, 1, [], null)]
        };

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(transformed, GridPosition.FrontBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(defender, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(attacker.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(3)
            }));
    }

    [Test]
    public void TransformedAttackTest()
    {
        UnitDefinition beastDefinition = TestHelpers.SimpleUnitDefinition("beast", maxHitpoints: 50, initiative: 50, damage: 9);
        UnitDefinition attackerDefinition = TransformSelfDefinition("attacker", maxHitpoints: 100, beastDefinition.Id, null);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 100, initiative: 40, damage: 5);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 40);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(beastDefinition)
            .WithAttacker(attacker, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        _ = engine.DispatchCommand(new BattleAttackCommand(attacker.Id, new BattleAttackUnitTarget(attacker.Id)));

        engine.CheckCommand(new BattleAttackCommand(attacker.Id, new BattleAttackUnitTarget(defender.Id)),
        [
            new BattleUnitAttackEvent(attacker.Id, new BattleAttackUnitTarget(defender.Id), [beastDefinition.Attack.Id]),
            new BattleUnitAttackedEvent(attacker.Id, defender.Id, beastDefinition.Attack.Id),
            new BattleUnitAttackHitEvent(attacker.Id, defender.Id, beastDefinition.Attack.Id, 9),
            new BattleUnitTurnEndingEvent(attacker.Id),
            new BattleUnitTurnEndedEvent(attacker.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal))
        ]);

        BattleUnit transformed = attacker with
        {
            DefinitionId = beastDefinition.Id,
            Hitpoints = 20,
            StatusEffects = [new BattleTransformStatusEffect(BattleTransformType.TransformSelf, attackerDefinition.Id, 1, [], null)]
        };

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(transformed, GridPosition.FrontBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(defender with { Hitpoints = 91 }, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal),
                TurnQueue = [],
                CurrentRound = BattleRound.Normal(1)
            }));
    }

    [Test]
    public void AttacksTwiceFormTest()
    {
        UnitDefinition beastDefinition = TestHelpers.SimpleUnitDefinition("beast", maxHitpoints: 50, initiative: 50, damage: 9) with
        {
            AttacksTwice = true
        };
        UnitDefinition attackerDefinition = TransformSelfDefinition("attacker", maxHitpoints: 100, beastDefinition.Id, null);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 100, initiative: 40, damage: 5);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 40);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(beastDefinition)
            .WithAttacker(attacker, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        _ = engine.DispatchCommand(new BattleAttackCommand(attacker.Id, new BattleAttackUnitTarget(attacker.Id)));

        engine.CheckCommand(new BattleAttackCommand(attacker.Id, new BattleAttackUnitTarget(defender.Id)),
        [
            new BattleUnitAttackEvent(attacker.Id, new BattleAttackUnitTarget(defender.Id), [beastDefinition.Attack.Id]),
            new BattleUnitAttackedEvent(attacker.Id, defender.Id, beastDefinition.Attack.Id),
            new BattleUnitAttackHitEvent(attacker.Id, defender.Id, beastDefinition.Attack.Id, 9),
            new BattleUnitTurnEndingEvent(attacker.Id),
            new BattleUnitTurnEndedEvent(attacker.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(attacker.Id, BattleUnitTurnType.SecondAttack))
        ]);

        engine.CheckCommand(new BattleAttackCommand(attacker.Id, new BattleAttackUnitTarget(defender.Id)),
        [
            new BattleUnitAttackEvent(attacker.Id, new BattleAttackUnitTarget(defender.Id), [beastDefinition.Attack.Id]),
            new BattleUnitAttackedEvent(attacker.Id, defender.Id, beastDefinition.Attack.Id),
            new BattleUnitAttackHitEvent(attacker.Id, defender.Id, beastDefinition.Attack.Id, 9),
            new BattleUnitTurnEndingEvent(attacker.Id),
            new BattleUnitTurnEndedEvent(attacker.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal))
        ]);

        _ = engine.DispatchCommand(new BattleSkipCommand(defender.Id));

        BattleUnit transformed = attacker with
        {
            DefinitionId = beastDefinition.Id,
            Hitpoints = 20,
            StatusEffects = [new BattleTransformStatusEffect(BattleTransformType.TransformSelf, attackerDefinition.Id, 1, [], null)]
        };

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(transformed, GridPosition.FrontBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(defender with { Hitpoints = 82 }, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(attacker.Id, BattleUnitTurnType.Normal),
                TurnQueue = [new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal)],
                CurrentRound = BattleRound.Normal(2)
            }));
    }

    [Test]
    public void LargeAttackerTest()
    {
        UnitDefinition smallBeastDefinition = TestHelpers.SimpleUnitDefinition("small-beast", maxHitpoints: 50, initiative: 50, damage: 9);
        UnitDefinition largeBeastDefinition = TestHelpers.SimpleUnitDefinition("large-beast", maxHitpoints: 150, initiative: 50, damage: 15) with
        {
            IsLarge = true
        };
        UnitDefinition attackerDefinition = TransformSelfDefinition(
                "attacker",
                maxHitpoints: 100,
                smallBeastDefinition.Id,
                largeBeastDefinition.Id) with
            {
                IsLarge = true
            };
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 100, initiative: 40, damage: 5);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 80);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(smallBeastDefinition)
            .WithDefinition(largeBeastDefinition)
            .WithAttacker(attacker, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(attacker.Id, new BattleAttackUnitTarget(attacker.Id)),
        [
            new BattleUnitAttackEvent(attacker.Id, new BattleAttackUnitTarget(attacker.Id), [attackerDefinition.Attack.Id]),
            new BattleUnitTransformAppliedEvent(attacker.Id, attacker.Id, BattleTransformType.TransformSelf, largeBeastDefinition.Id, 1, 120, 150, null)
        ]);

        BattleUnit transformed = attacker with
        {
            DefinitionId = largeBeastDefinition.Id,
            Hitpoints = 120,
            StatusEffects = [new BattleTransformStatusEffect(BattleTransformType.TransformSelf, attackerDefinition.Id, 1, [], null)]
        };

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(transformed, GridPosition.FrontBottom, true)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(defender, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(attacker.Id, BattleUnitTurnType.Normal)
            }));
    }

    [Test]
    public void BattleEndRestoresTest()
    {
        UnitDefinition beastDefinition = TestHelpers.SimpleUnitDefinition("beast", maxHitpoints: 50, initiative: 50, damage: 9);
        UnitDefinition attackerDefinition = TransformSelfDefinition("attacker", maxHitpoints: 100, beastDefinition.Id, null);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 5, initiative: 40, damage: 1);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 80);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 5);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(beastDefinition)
            .WithAttacker(attacker, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        _ = engine.DispatchCommand(new BattleAttackCommand(attacker.Id, new BattleAttackUnitTarget(attacker.Id)));

        engine.CheckCommand(new BattleAttackCommand(attacker.Id, new BattleAttackUnitTarget(defender.Id)),
        [
            new BattleUnitAttackEvent(attacker.Id, new BattleAttackUnitTarget(defender.Id), [beastDefinition.Attack.Id]),
            new BattleUnitAttackedEvent(attacker.Id, defender.Id, beastDefinition.Attack.Id),
            new BattleUnitAttackHitEvent(attacker.Id, defender.Id, beastDefinition.Attack.Id, 9),
            new BattleUnitDiedEvent(defender.Id),
            new BattleUnitTurnEndingEvent(attacker.Id),
            new BattleRoundEndedEvent(),
            new BattleUnitTransformRestoredEvent(attacker.Id, BattleTransformType.TransformSelf, attackerDefinition.Id, 80, 100),
            new BattleEndedEvent(BattleSide.Attacker),
            new BattleUnitExperienceGainedEvent(attacker.Id, 1)
        ]);

        BattleUnit restored = attacker with
        {
            Hitpoints = 80,
            StatusEffects = [],
            ExperienceGained = 1
        };

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(restored, GridPosition.FrontBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(defender with { Hitpoints = 0 }, GridPosition.FrontBottom, false)
                    .Build(),
                Outcome = new BattleOutcome(BattleSide.Attacker)
            }));
    }

    [Test]
    public void GetAttackInfoTest()
    {
        UnitDefinition beastDefinition = TestHelpers.SimpleUnitDefinition("beast", maxHitpoints: 50, initiative: 50, damage: 9);
        UnitDefinition attackerDefinition = TransformSelfDefinition("attacker", maxHitpoints: 100, beastDefinition.Id, null);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 100, initiative: 40, damage: 5);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 80);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(beastDefinition)
            .WithAttacker(attacker, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        BattleGetAttackInfoCommand.Result result = engine.CheckCommand(new BattleGetAttackInfoCommand(attacker.Id), []);

        Assert.That(result, Is.EqualTo(new BattleGetAttackInfoCommand.Result(
        [
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackUnitTarget(attacker.Id), [new BattleAttackUnitTarget(attacker.Id)], null),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackUnitTarget(defender.Id), [], BattleCommandError.CanOnlyTransformSelf),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.FrontBottom), [], BattleCommandError.AttackRequiresUnitTarget),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.FrontCenter), [], BattleCommandError.AttackRequiresUnitTarget),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.FrontTop), [], BattleCommandError.AttackRequiresUnitTarget),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.BackBottom), [], BattleCommandError.AttackRequiresUnitTarget),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.BackCenter), [], BattleCommandError.AttackRequiresUnitTarget),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.BackTop), [], BattleCommandError.AttackRequiresUnitTarget),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackTileTarget(BattleSide.Defender, GridPosition.FrontBottom), [], BattleCommandError.AttackRequiresUnitTarget),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackTileTarget(BattleSide.Defender, GridPosition.FrontCenter), [], BattleCommandError.AttackRequiresUnitTarget),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackTileTarget(BattleSide.Defender, GridPosition.FrontTop), [], BattleCommandError.AttackRequiresUnitTarget),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackTileTarget(BattleSide.Defender, GridPosition.BackBottom), [], BattleCommandError.AttackRequiresUnitTarget),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackTileTarget(BattleSide.Defender, GridPosition.BackCenter), [], BattleCommandError.AttackRequiresUnitTarget),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackTileTarget(BattleSide.Defender, GridPosition.BackTop), [], BattleCommandError.AttackRequiresUnitTarget)
        ])));
    }

    [Test]
    public void AlreadyTransformedTest()
    {
        UnitDefinition greaterBeastDefinition = TestHelpers.SimpleUnitDefinition("greater-beast", maxHitpoints: 70, initiative: 50, damage: 12);
        UnitDefinition beastDefinition = TransformSelfDefinition("beast", maxHitpoints: 50, greaterBeastDefinition.Id, null);
        UnitDefinition attackerDefinition = TransformSelfDefinition("attacker", maxHitpoints: 100, beastDefinition.Id, null);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 100, initiative: 40, damage: 5);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 80);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(beastDefinition)
            .WithDefinition(greaterBeastDefinition)
            .WithAttacker(attacker, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        _ = engine.DispatchCommand(new BattleAttackCommand(attacker.Id, new BattleAttackUnitTarget(attacker.Id)));

        engine.CheckCommandError(
            new BattleAttackCommand(attacker.Id, new BattleAttackUnitTarget(attacker.Id)),
            BattleCommandError.UnitAlreadyTransformed
        );
    }

    [Test]
    public void CureTest()
    {
        UnitDefinition cureDefinition = TestHelpers.SimpleUnitDefinition("cure", maxHitpoints: 10, initiative: 60, damage: 0) with
        {
            Attack = TestHelpers.SimpleAttack(
                    new UnitAttackDefinitionId("cure"),
                    initiative: 60,
                    damage: 0,
                    accuracy: 100,
                    reach: UnitAttackReach.Any) with
                {
                    Type = UnitAttackType.Cure
                }
        };
        UnitDefinition beastDefinition = TestHelpers.SimpleUnitDefinition("beast", maxHitpoints: 50, initiative: 50, damage: 9);
        UnitDefinition attackerDefinition = TransformSelfDefinition("attacker", maxHitpoints: 100, beastDefinition.Id, null);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 100, initiative: 40, damage: 5);

        BattleTransformStatusEffect transformEffect = new(BattleTransformType.TransformSelf, attackerDefinition.Id, 1, [], null);
        BattleUnit cure = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, cureDefinition.Id, BattleSide.Attacker, hitpoints: 10);
        BattleUnit transformed = new(
            new BattleUnitId(1),
            null,
            beastDefinition.Id,
            BattleSide.Attacker,
            40,
            0,
            0,
            1,
            [],
            [transformEffect, new BattlePoisonStatusEffect(3, 10)],
            null,
            [],
            Name: null,
            IsTypeLocked: false);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(cureDefinition)
            .WithDefinition(attackerDefinition)
            .WithDefinition(beastDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(cure, GridPosition.FrontBottom)
            .WithAttacker(transformed, GridPosition.FrontCenter)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(cure.Id, new BattleAttackUnitTarget(transformed.Id)),
        [
            new BattleUnitAttackEvent(cure.Id, new BattleAttackUnitTarget(transformed.Id), [cureDefinition.Attack.Id]),
            new BattleUnitAttackedEvent(cure.Id, transformed.Id, cureDefinition.Attack.Id),
            new BattleUnitAttackHitEvent(cure.Id, transformed.Id, cureDefinition.Attack.Id, 0),
            new BattleUnitCuredEvent(transformed.Id),
            new BattleUnitTurnEndingEvent(cure.Id),
            new BattleUnitTurnEndedEvent(cure.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(transformed.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(cure, GridPosition.FrontBottom, false)
                    .Place(transformed with { StatusEffects = [transformEffect, new BattleCureStatusEffect()] }, GridPosition.FrontCenter, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(defender, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(transformed.Id, BattleUnitTurnType.Normal)
            }));
    }

    [Test]
    public void AttackerLevelTest()
    {
        UnitDefinition beastDefinition = TestHelpers.SimpleUnitDefinition("beast", maxHitpoints: 50, initiative: 50, damage: 9);
        UnitDefinition attackerDefinition = TransformSelfDefinition("attacker", maxHitpoints: 100, beastDefinition.Id, null);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 100, initiative: 40, damage: 5);
        UnitDynamicUpgradeDefinition beastUpgrade = new(
            beastDefinition.Id,
            UpgradeChangeLevel: 10,
            UnitDynamicUpgrade.Empty with { MaxHitpoints = 10 },
            UnitDynamicUpgrade.Empty with { MaxHitpoints = 5 });
        UnitDynamicUpgradeDefinition attackerUpgrade = new(
            attackerDefinition.Id,
            UpgradeChangeLevel: 10,
            UnitDynamicUpgrade.Empty with { MaxHitpoints = 10 },
            UnitDynamicUpgrade.Empty with { MaxHitpoints = 5 });

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 90) with { Level = 3 };
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(beastDefinition)
            .WithDefinition(beastUpgrade)
            .WithDefinition(attackerUpgrade)
            .WithAttacker(attacker, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(attacker.Id, new BattleAttackUnitTarget(attacker.Id)),
        [
            new BattleUnitAttackEvent(attacker.Id, new BattleAttackUnitTarget(attacker.Id), [attackerDefinition.Attack.Id]),
            new BattleUnitTransformAppliedEvent(attacker.Id, attacker.Id, BattleTransformType.TransformSelf, beastDefinition.Id, 1, 37, 50, null)
        ]);

        BattleUnit transformed = attacker with
        {
            DefinitionId = beastDefinition.Id,
            Level = 1,
            Hitpoints = 37,
            StatusEffects = [new BattleTransformStatusEffect(BattleTransformType.TransformSelf, attackerDefinition.Id, 3, [], null)]
        };

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(transformed, GridPosition.FrontBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(defender, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(attacker.Id, BattleUnitTurnType.Normal)
            }));
    }
}
