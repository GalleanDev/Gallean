namespace Gallean.Engine.Tests;

public sealed class BattleEngineSecondaryAttackTests
{
    private static UnitDefinition HealerWithReviveDefinition()
    {
        return TestHelpers.SimpleUnitDefinition("priest", maxHitpoints: 100, initiative: 60, damage: 0) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("priest-heal"), initiative: 60, damage: 0, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Type = UnitAttackType.Heal,
                Source = UnitAttackSource.Life,
                Heal = 120,
                Level = 1
            },
            SecondaryAttack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("priest-revive"), initiative: 60, damage: 0, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Type = UnitAttackType.Revive,
                Source = UnitAttackSource.Life,
                Heal = 50,
                Level = 1,
                IsInfinite = true
            }
        };
    }

    [Test]
    public void PrimaryAndSecondaryAttacksTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("bethrezen", maxHitpoints: 50, initiative: 50, damage: 5) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("bethrezen-fire"), initiative: 50, damage: 5, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Source = UnitAttackSource.Fire,
                Level = 1
            },
            SecondaryAttack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("bethrezen-paralyze"), initiative: 50, damage: 0, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Type = UnitAttackType.Paralyze,
                Source = UnitAttackSource.Mind,
                Level = 1
            }
        };
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 40, damage: 5);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 50);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(attacker, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .WithRandom(new TestSequenceRandomProvider(1, 1, 1, 1, 1, 1, 1, 1))
            .Build();

        engine.CheckCommand(new BattleAttackCommand(attacker.Id, new BattleAttackUnitTarget(defender.Id)), [
            new BattleUnitAttackEvent(attacker.Id, new BattleAttackUnitTarget(defender.Id), [new UnitAttackDefinitionId("bethrezen-fire"), new UnitAttackDefinitionId("bethrezen-paralyze")]),
            new BattleUnitAttackedEvent(attacker.Id, defender.Id, new UnitAttackDefinitionId("bethrezen-fire")),
            new BattleUnitAttackHitEvent(attacker.Id, defender.Id, new UnitAttackDefinitionId("bethrezen-fire"), 5),
            new BattleUnitAttackedEvent(attacker.Id, defender.Id, new UnitAttackDefinitionId("bethrezen-paralyze")),
            new BattleUnitAttackHitEvent(attacker.Id, defender.Id, new UnitAttackDefinitionId("bethrezen-paralyze"), 0),
            new BattleUnitEffectAppliedEvent(attacker.Id, defender.Id, new BattleParalyzeStatusEffect(1)),
            new BattleUnitTurnEndingEvent(attacker.Id),
            new BattleUnitTurnEndedEvent(attacker.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal)),
            new BattleUnitEffectDurationDecreasedEvent(defender.Id),
            new BattleUnitEffectExpiredEvent(defender.Id, new BattleParalyzeStatusEffect(0)),
            new BattleUnitTurnSkippedEvent(defender.Id),
            new BattleUnitTurnEndingEvent(defender.Id),
            new BattleUnitTurnEndedEvent(defender.Id),
            new BattleRoundEndedEvent(),
            new BattleRoundStartedEvent(BattleRound.Normal(2), [new BattleUnitTurn(attacker.Id, BattleUnitTurnType.Normal), new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal)]),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(attacker.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(attacker, GridPosition.FrontBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(defender with { Hitpoints = 5 }, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(attacker.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(2)
            }));
    }

    [Test]
    public void AttacksTwiceTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("bethrezen", maxHitpoints: 50, initiative: 50, damage: 5) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("bethrezen-fire"), initiative: 50, damage: 5, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Source = UnitAttackSource.Fire,
                Level = 1
            },
            SecondaryAttack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("bethrezen-paralyze"), initiative: 50, damage: 0, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Type = UnitAttackType.Paralyze,
                Source = UnitAttackSource.Mind,
                Level = 1
            },
            AttacksTwice = true
        };
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 20, initiative: 40, damage: 5);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 50);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 20);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(attacker, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .WithRandom(new TestSequenceRandomProvider(1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1))
            .Build();

        engine.CheckCommand(new BattleAttackCommand(attacker.Id, new BattleAttackUnitTarget(defender.Id)), [
            new BattleUnitAttackEvent(attacker.Id, new BattleAttackUnitTarget(defender.Id), [new UnitAttackDefinitionId("bethrezen-fire"), new UnitAttackDefinitionId("bethrezen-paralyze")]),
            new BattleUnitAttackedEvent(attacker.Id, defender.Id, new UnitAttackDefinitionId("bethrezen-fire")),
            new BattleUnitAttackHitEvent(attacker.Id, defender.Id, new UnitAttackDefinitionId("bethrezen-fire"), 5),
            new BattleUnitAttackedEvent(attacker.Id, defender.Id, new UnitAttackDefinitionId("bethrezen-paralyze")),
            new BattleUnitAttackHitEvent(attacker.Id, defender.Id, new UnitAttackDefinitionId("bethrezen-paralyze"), 0),
            new BattleUnitEffectAppliedEvent(attacker.Id, defender.Id, new BattleParalyzeStatusEffect(1)),
            new BattleUnitTurnEndingEvent(attacker.Id),
            new BattleUnitTurnEndedEvent(attacker.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(attacker.Id, BattleUnitTurnType.SecondAttack))
        ]);

        engine.CheckCommand(new BattleAttackCommand(attacker.Id, new BattleAttackUnitTarget(defender.Id)), [
            new BattleUnitAttackEvent(attacker.Id, new BattleAttackUnitTarget(defender.Id), [new UnitAttackDefinitionId("bethrezen-fire"), new UnitAttackDefinitionId("bethrezen-paralyze")]),
            new BattleUnitAttackedEvent(attacker.Id, defender.Id, new UnitAttackDefinitionId("bethrezen-fire")),
            new BattleUnitAttackHitEvent(attacker.Id, defender.Id, new UnitAttackDefinitionId("bethrezen-fire"), 5),
            new BattleUnitTurnEndingEvent(attacker.Id),
            new BattleUnitTurnEndedEvent(attacker.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal)),
            new BattleUnitEffectDurationDecreasedEvent(defender.Id),
            new BattleUnitEffectExpiredEvent(defender.Id, new BattleParalyzeStatusEffect(0)),
            new BattleUnitTurnSkippedEvent(defender.Id),
            new BattleUnitTurnEndingEvent(defender.Id),
            new BattleUnitTurnEndedEvent(defender.Id),
            new BattleRoundEndedEvent(),
            new BattleRoundStartedEvent(BattleRound.Normal(2), [new BattleUnitTurn(attacker.Id, BattleUnitTurnType.Normal), new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal)]),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(attacker.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(attacker, GridPosition.FrontBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(defender with { Hitpoints = 10 }, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(attacker.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(2)
            }));
    }

    [Test]
    public void NoModifiersTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 50, initiative: 50, damage: 5) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("attacker-main"), initiative: 50, damage: 5, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Level = 1
            },
            SecondaryAttack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("attacker-rider"), initiative: 50, damage: 4, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Source = UnitAttackSource.Death,
                Level = 1
            }
        };
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 20, initiative: 40, damage: 5);

        UnitModifierDefinition damageBonus = TestHelpers.SimpleModifier("damage") with
        {
            Damage = new UnitStatModifier(0, 100)
        };

        BattleUnit attacker = new(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, 50, 0, 0, 1, [UnitModifier.Permanent(damageBonus.Id)], [], null, [], null, false);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 20);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(damageBonus)
            .WithAttacker(attacker, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .WithRandom(new TestSequenceRandomProvider(1, 1, 1, 1, 1, 1, 1, 1))
            .Build();

        engine.CheckCommand(new BattleAttackCommand(attacker.Id, new BattleAttackUnitTarget(defender.Id)), [
            new BattleUnitAttackEvent(attacker.Id, new BattleAttackUnitTarget(defender.Id), [new UnitAttackDefinitionId("attacker-main"), new UnitAttackDefinitionId("attacker-rider")]),
            new BattleUnitAttackedEvent(attacker.Id, defender.Id, new UnitAttackDefinitionId("attacker-main")),
            new BattleUnitAttackHitEvent(attacker.Id, defender.Id, new UnitAttackDefinitionId("attacker-main"), 10),
            new BattleUnitAttackedEvent(attacker.Id, defender.Id, new UnitAttackDefinitionId("attacker-rider")),
            new BattleUnitAttackHitEvent(attacker.Id, defender.Id, new UnitAttackDefinitionId("attacker-rider"), 4),
            new BattleUnitTurnEndingEvent(attacker.Id),
            new BattleUnitTurnEndedEvent(attacker.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(attacker, GridPosition.FrontBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(defender with { Hitpoints = 6 }, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(1)
            }));
    }

    [Test]
    public void RollTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("bethrezen", maxHitpoints: 50, initiative: 50, damage: 5) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("bethrezen-fire"), initiative: 50, damage: 5, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Source = UnitAttackSource.Fire,
                Level = 1
            },
            SecondaryAttack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("bethrezen-paralyze"), initiative: 50, damage: 0, accuracy: 10, reach: UnitAttackReach.Any) with
            {
                Type = UnitAttackType.Paralyze,
                Source = UnitAttackSource.Mind,
                Level = 1
            }
        };
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 40, damage: 5);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 50);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(attacker, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .WithRandom(new TestSequenceRandomProvider(1, 1, 1, 1, 1, 99, 99, 99))
            .Build();

        engine.CheckCommand(new BattleAttackCommand(attacker.Id, new BattleAttackUnitTarget(defender.Id)), [
            new BattleUnitAttackEvent(attacker.Id, new BattleAttackUnitTarget(defender.Id), [new UnitAttackDefinitionId("bethrezen-fire"), new UnitAttackDefinitionId("bethrezen-paralyze")]),
            new BattleUnitAttackedEvent(attacker.Id, defender.Id, new UnitAttackDefinitionId("bethrezen-fire")),
            new BattleUnitAttackHitEvent(attacker.Id, defender.Id, new UnitAttackDefinitionId("bethrezen-fire"), 5),
            new BattleUnitAttackedEvent(attacker.Id, defender.Id, new UnitAttackDefinitionId("bethrezen-paralyze")),
            new BattleUnitAttackMissedEvent(attacker.Id, defender.Id, new UnitAttackDefinitionId("bethrezen-paralyze")),
            new BattleUnitTurnEndingEvent(attacker.Id),
            new BattleUnitTurnEndedEvent(attacker.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(attacker, GridPosition.FrontBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(defender with { Hitpoints = 5 }, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(1)
            }));
    }

    [Test]
    public void SecondaryAfterPrimaryKillTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("bethrezen", maxHitpoints: 50, initiative: 50, damage: 10) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("bethrezen-fire"), initiative: 50, damage: 10, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Source = UnitAttackSource.Fire,
                Level = 1
            },
            SecondaryAttack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("bethrezen-paralyze"), initiative: 50, damage: 0, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Type = UnitAttackType.Paralyze,
                Source = UnitAttackSource.Mind,
                Level = 1
            }
        };
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 6, initiative: 40, damage: 5);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 50);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 6);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(attacker, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .WithRandom(new TestSequenceRandomProvider(1, 1, 1, 1, 1, 1))
            .Build();

        engine.CheckCommand(new BattleAttackCommand(attacker.Id, new BattleAttackUnitTarget(defender.Id)), [
            new BattleUnitAttackEvent(attacker.Id, new BattleAttackUnitTarget(defender.Id), [new UnitAttackDefinitionId("bethrezen-fire"), new UnitAttackDefinitionId("bethrezen-paralyze")]),
            new BattleUnitAttackedEvent(attacker.Id, defender.Id, new UnitAttackDefinitionId("bethrezen-fire")),
            new BattleUnitAttackHitEvent(attacker.Id, defender.Id, new UnitAttackDefinitionId("bethrezen-fire"), 10),
            new BattleUnitDiedEvent(defender.Id),
            new BattleUnitTurnEndingEvent(attacker.Id),
            new BattleRoundEndedEvent(),
            new BattleEndedEvent(BattleSide.Attacker),
            new BattleUnitExperienceGainedEvent(attacker.Id, 1)
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(attacker with { ExperienceGained = 1 }, GridPosition.FrontBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(defender with { Hitpoints = 0 }, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(attacker.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(1),
                Outcome = new BattleOutcome(BattleSide.Attacker)
            }));
    }

    [Test]
    public void TargetAllowedBySecondaryAttackTest()
    {
        UnitDefinition priestDefinition = HealerWithReviveDefinition();
        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ally", maxHitpoints: 100, initiative: 20, damage: 5);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 100, initiative: 40, damage: 5);

        BattleUnit priest = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, priestDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit ally = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, allyDefinition.Id, BattleSide.Attacker, hitpoints: 0);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(priestDefinition)
            .WithDefinition(allyDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(priest, GridPosition.FrontBottom)
            .WithAttacker(ally, GridPosition.BackBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(priest.Id, new BattleAttackUnitTarget(ally.Id)), [
            new BattleUnitAttackEvent(priest.Id, new BattleAttackUnitTarget(ally.Id), [new UnitAttackDefinitionId("priest-heal"), new UnitAttackDefinitionId("priest-revive")]),
            new BattleUnitAttackedEvent(priest.Id, ally.Id, new UnitAttackDefinitionId("priest-revive")),
            new BattleUnitAttackHitEvent(priest.Id, ally.Id, new UnitAttackDefinitionId("priest-revive"), 0),
            new BattleUnitRevivedEvent(ally.Id, 50),
            new BattleUnitTurnEndingEvent(priest.Id),
            new BattleUnitTurnEndedEvent(priest.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(priest, GridPosition.FrontBottom, false)
                    .Place(ally with { Hitpoints = 50, IsRevived = true }, GridPosition.BackBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(defender, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(1)
            }));
    }

    [Test]
    public void TargetRejectedByBothAttacksTest()
    {
        UnitDefinition priestDefinition = HealerWithReviveDefinition();
        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ally", maxHitpoints: 100, initiative: 20, damage: 5);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 100, initiative: 40, damage: 5);

        BattleUnit priest = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, priestDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit ally = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, allyDefinition.Id, BattleSide.Attacker, hitpoints: 0) with
        {
            IsRevived = true
        };
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(priestDefinition)
            .WithDefinition(allyDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(priest, GridPosition.FrontBottom)
            .WithAttacker(ally, GridPosition.BackBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommandError(
            new BattleAttackCommand(priest.Id, new BattleAttackUnitTarget(ally.Id)),
            BattleCommandError.TargetDead);
    }

    [Test]
    public void AllReachAfterKillTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("overlord", maxHitpoints: 50, initiative: 50, damage: 10) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("overlord-blade"), initiative: 50, damage: 10, accuracy: 100, reach: UnitAttackReach.Adjacent) with
            {
                Source = UnitAttackSource.Fire,
                Level = 1
            },
            SecondaryAttack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("overlord-blister"), initiative: 50, damage: 0, accuracy: 100, reach: UnitAttackReach.All) with
            {
                Type = UnitAttackType.Blister,
                Source = UnitAttackSource.Fire,
                Level = 5
            }
        };
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 6, initiative: 40, damage: 5);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 50);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 6);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(attacker, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .WithRandom(new TestSequenceRandomProvider(1, 1, 1, 1, 1, 1))
            .Build();

        engine.CheckCommand(new BattleAttackCommand(attacker.Id, new BattleAttackUnitTarget(defender.Id)), [
            new BattleUnitAttackEvent(attacker.Id, new BattleAttackUnitTarget(defender.Id), [new UnitAttackDefinitionId("overlord-blade"), new UnitAttackDefinitionId("overlord-blister")]),
            new BattleUnitAttackedEvent(attacker.Id, defender.Id, new UnitAttackDefinitionId("overlord-blade")),
            new BattleUnitAttackHitEvent(attacker.Id, defender.Id, new UnitAttackDefinitionId("overlord-blade"), 10),
            new BattleUnitDiedEvent(defender.Id),
            new BattleUnitTurnEndingEvent(attacker.Id),
            new BattleRoundEndedEvent(),
            new BattleEndedEvent(BattleSide.Attacker),
            new BattleUnitExperienceGainedEvent(attacker.Id, 1)
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(attacker with { ExperienceGained = 1 }, GridPosition.FrontBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(defender with { Hitpoints = 0 }, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(attacker.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(1),
                Outcome = new BattleOutcome(BattleSide.Attacker)
            }));
    }

    [Test]
    public void WardedPrimaryTest()
    {
        UnitWard ward = new UnitAttackSourceWard(UnitAttackSource.Water, UnitWardCategory.Once);
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("ymir", maxHitpoints: 50, initiative: 50, damage: 15) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("ymir-water"), initiative: 50, damage: 15, accuracy: 100, reach: UnitAttackReach.Adjacent) with
            {
                Source = UnitAttackSource.Water,
                Level = 1
            },
            SecondaryAttack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("ymir-frostbite"), initiative: 50, damage: 3, accuracy: 100, reach: UnitAttackReach.Adjacent) with
            {
                Type = UnitAttackType.Frostbite,
                Source = UnitAttackSource.Water,
                Level = 1
            }
        };
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("templar", maxHitpoints: 20, initiative: 40, damage: 5) with
        {
            Wards = [ward]
        };

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 50);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 20);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(attacker, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .WithRandom(new TestSequenceRandomProvider(1, 1, 1, 1, 1, 1))
            .Build();

        engine.CheckCommand(new BattleAttackCommand(attacker.Id, new BattleAttackUnitTarget(defender.Id)), [
            new BattleUnitAttackEvent(attacker.Id, new BattleAttackUnitTarget(defender.Id), [new UnitAttackDefinitionId("ymir-water"), new UnitAttackDefinitionId("ymir-frostbite")]),
            new BattleUnitAttackedEvent(attacker.Id, defender.Id, new UnitAttackDefinitionId("ymir-water")),
            new BattleUnitAttackBlockedEvent(attacker.Id, defender.Id, new UnitAttackDefinitionId("ymir-water"), ward),
            new BattleUnitWardConsumedEvent(defender.Id, ward),
            new BattleUnitTurnEndingEvent(attacker.Id),
            new BattleUnitTurnEndedEvent(attacker.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(attacker, GridPosition.FrontBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(defender, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(1)
            }));
    }

    [Test]
    public void MissedPrimaryTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("bethrezen", maxHitpoints: 50, initiative: 50, damage: 5) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("bethrezen-fire"), initiative: 50, damage: 5, accuracy: 10, reach: UnitAttackReach.Any) with
            {
                Source = UnitAttackSource.Fire,
                Level = 1
            },
            SecondaryAttack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("bethrezen-paralyze"), initiative: 50, damage: 0, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Type = UnitAttackType.Paralyze,
                Source = UnitAttackSource.Mind,
                Level = 1
            }
        };
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 40, damage: 5);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 50);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(attacker, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .WithRandom(new TestSequenceRandomProvider(1, 1, 1, 99, 99, 1, 1, 1))
            .Build();

        engine.CheckCommand(new BattleAttackCommand(attacker.Id, new BattleAttackUnitTarget(defender.Id)), [
            new BattleUnitAttackEvent(attacker.Id, new BattleAttackUnitTarget(defender.Id), [new UnitAttackDefinitionId("bethrezen-fire"), new UnitAttackDefinitionId("bethrezen-paralyze")]),
            new BattleUnitAttackedEvent(attacker.Id, defender.Id, new UnitAttackDefinitionId("bethrezen-fire")),
            new BattleUnitAttackMissedEvent(attacker.Id, defender.Id, new UnitAttackDefinitionId("bethrezen-fire")),
            new BattleUnitTurnEndingEvent(attacker.Id),
            new BattleUnitTurnEndedEvent(attacker.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(attacker, GridPosition.FrontBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(defender, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(1)
            }));
    }

    [Test]
    public void AllReachWardedPrimaryTest()
    {
        UnitWard ward = new UnitAttackSourceWard(UnitAttackSource.Water, UnitWardCategory.Once);
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("mermaid", maxHitpoints: 50, initiative: 50, damage: 5) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("mermaid-water"), initiative: 50, damage: 5, accuracy: 100, reach: UnitAttackReach.All) with
            {
                Source = UnitAttackSource.Water,
                Level = 1
            },
            SecondaryAttack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("mermaid-paralyze"), initiative: 50, damage: 0, accuracy: 100, reach: UnitAttackReach.All) with
            {
                Type = UnitAttackType.Paralyze,
                Source = UnitAttackSource.Mind,
                Level = 1
            }
        };
        UnitDefinition wardedDefinition = TestHelpers.SimpleUnitDefinition("templar", maxHitpoints: 20, initiative: 40, damage: 5) with
        {
            Wards = [ward]
        };
        UnitDefinition plainDefinition = TestHelpers.SimpleUnitDefinition("fighter", maxHitpoints: 20, initiative: 30, damage: 5);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 50);
        BattleUnit warded = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, wardedDefinition.Id, BattleSide.Defender, hitpoints: 20);
        BattleUnit plain = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, plainDefinition.Id, BattleSide.Defender, hitpoints: 20);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(wardedDefinition)
            .WithDefinition(plainDefinition)
            .WithAttacker(attacker, GridPosition.FrontBottom)
            .WithDefender(warded, GridPosition.FrontBottom)
            .WithDefender(plain, GridPosition.FrontTop)
            .WithRandom(new TestSequenceRandomProvider(1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1))
            .Build();

        engine.CheckCommand(new BattleAttackCommand(attacker.Id, new BattleAttackUnitTarget(warded.Id)), [
            new BattleUnitAttackEvent(attacker.Id, new BattleAttackUnitTarget(warded.Id), [new UnitAttackDefinitionId("mermaid-water"), new UnitAttackDefinitionId("mermaid-paralyze")]),
            new BattleUnitAttackedEvent(attacker.Id, warded.Id, new UnitAttackDefinitionId("mermaid-water")),
            new BattleUnitAttackBlockedEvent(attacker.Id, warded.Id, new UnitAttackDefinitionId("mermaid-water"), ward),
            new BattleUnitWardConsumedEvent(warded.Id, ward),
            new BattleUnitAttackedEvent(attacker.Id, plain.Id, new UnitAttackDefinitionId("mermaid-water")),
            new BattleUnitAttackHitEvent(attacker.Id, plain.Id, new UnitAttackDefinitionId("mermaid-water"), 5),
            new BattleUnitAttackedEvent(attacker.Id, plain.Id, new UnitAttackDefinitionId("mermaid-paralyze")),
            new BattleUnitAttackHitEvent(attacker.Id, plain.Id, new UnitAttackDefinitionId("mermaid-paralyze"), 0),
            new BattleUnitEffectAppliedEvent(attacker.Id, plain.Id, new BattleParalyzeStatusEffect(1)),
            new BattleUnitTurnEndingEvent(attacker.Id),
            new BattleUnitTurnEndedEvent(attacker.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(warded.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(attacker, GridPosition.FrontBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(warded, GridPosition.FrontBottom, false)
                    .Place(plain with { Hitpoints = 15, StatusEffects = [new BattleParalyzeStatusEffect(1)] }, GridPosition.FrontTop, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(warded.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(1)
            }));
    }

    [Test]
    public void SecondaryReachTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("overlord", maxHitpoints: 50, initiative: 50, damage: 5) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("overlord-blade"), initiative: 50, damage: 5, accuracy: 100, reach: UnitAttackReach.Adjacent) with
            {
                Level = 1
            },
            SecondaryAttack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("overlord-blister"), initiative: 50, damage: 3, accuracy: 100, reach: UnitAttackReach.All) with
            {
                Type = UnitAttackType.Blister,
                Source = UnitAttackSource.Fire,
                Level = 1
            }
        };
        UnitDefinition targetDefinition = TestHelpers.SimpleUnitDefinition("target", maxHitpoints: 20, initiative: 10, damage: 5);
        UnitDefinition bystanderDefinition = TestHelpers.SimpleUnitDefinition("bystander", maxHitpoints: 20, initiative: 40, damage: 5);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 50);
        BattleUnit target = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, targetDefinition.Id, BattleSide.Defender, hitpoints: 20);
        BattleUnit bystander = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, bystanderDefinition.Id, BattleSide.Defender, hitpoints: 20);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(targetDefinition)
            .WithDefinition(bystanderDefinition)
            .WithAttacker(attacker, GridPosition.FrontBottom)
            .WithDefender(target, GridPosition.FrontBottom)
            .WithDefender(bystander, GridPosition.BackBottom)
            .WithRandom(new TestSequenceRandomProvider(1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1))
            .Build();

        engine.CheckCommand(new BattleAttackCommand(attacker.Id, new BattleAttackUnitTarget(target.Id)), [
            new BattleUnitAttackEvent(attacker.Id, new BattleAttackUnitTarget(target.Id), [new UnitAttackDefinitionId("overlord-blade"), new UnitAttackDefinitionId("overlord-blister")]),
            new BattleUnitAttackedEvent(attacker.Id, target.Id, new UnitAttackDefinitionId("overlord-blade")),
            new BattleUnitAttackHitEvent(attacker.Id, target.Id, new UnitAttackDefinitionId("overlord-blade"), 5),
            new BattleUnitAttackedEvent(attacker.Id, target.Id, new UnitAttackDefinitionId("overlord-blister")),
            new BattleUnitAttackHitEvent(attacker.Id, target.Id, new UnitAttackDefinitionId("overlord-blister"), 0),
            new BattleUnitEffectAppliedEvent(attacker.Id, target.Id, new BattleBlisterStatusEffect(1, 3)),
            new BattleUnitTurnEndingEvent(attacker.Id),
            new BattleUnitTurnEndedEvent(attacker.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(bystander.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(attacker, GridPosition.FrontBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(target with { Hitpoints = 15, StatusEffects = [new BattleBlisterStatusEffect(1, 3)] }, GridPosition.FrontBottom, false)
                    .Place(bystander, GridPosition.BackBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(bystander.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(1)
            }));
    }

    [Test]
    public void UnreachableTargetTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("overlord", maxHitpoints: 50, initiative: 50, damage: 5) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("overlord-blade"), initiative: 50, damage: 5, accuracy: 100, reach: UnitAttackReach.Adjacent) with
            {
                Level = 1
            },
            SecondaryAttack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("overlord-blister"), initiative: 50, damage: 3, accuracy: 100, reach: UnitAttackReach.All) with
            {
                Type = UnitAttackType.Blister,
                Source = UnitAttackSource.Fire,
                Level = 1
            }
        };
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 20, initiative: 10, damage: 5);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 50);
        BattleUnit front = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 20);
        BattleUnit back = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 20);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(attacker, GridPosition.FrontBottom)
            .WithDefender(front, GridPosition.FrontBottom)
            .WithDefender(back, GridPosition.BackBottom)
            .Build();

        engine.CheckCommandError(
            new BattleAttackCommand(attacker.Id, new BattleAttackUnitTarget(back.Id)),
            BattleCommandError.TargetNotReachable);
    }
}
