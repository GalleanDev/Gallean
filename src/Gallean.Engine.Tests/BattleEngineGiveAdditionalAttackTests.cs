namespace Gallean.Engine.Tests;

public sealed class BattleEngineGiveAdditionalAttackTests
{
    private static UnitDefinition AlchemistDefinition(UnitAttackReach reach)
    {
        return TestHelpers.SimpleUnitDefinition("alchemist", maxHitpoints: 100, initiative: 60, damage: 0) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("alchemist"), initiative: 60, damage: 0, accuracy: 100, reach: reach) with
            {
                Type = UnitAttackType.GiveAdditionalAttack
            }
        };
    }

    [Test]
    public void GrantsExtraAttackTest()
    {
        UnitDefinition alchemistDefinition = AlchemistDefinition(UnitAttackReach.Any);
        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ally", maxHitpoints: 100, initiative: 20, damage: 5);
        UnitDefinition enemyDefinition = TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 100, initiative: 40, damage: 5);

        BattleUnit alchemist = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, alchemistDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit ally = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, allyDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit enemy = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, enemyDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(alchemistDefinition)
            .WithDefinition(allyDefinition)
            .WithDefinition(enemyDefinition)
            .WithAttacker(alchemist, GridPosition.FrontBottom)
            .WithAttacker(ally, GridPosition.BackBottom)
            .WithDefender(enemy, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(alchemist.Id, new BattleAttackUnitTarget(ally.Id)), [
            new BattleUnitAttackEvent(alchemist.Id, new BattleAttackUnitTarget(ally.Id), [new UnitAttackDefinitionId("alchemist")]),
            new BattleUnitAttackedEvent(alchemist.Id, ally.Id, new UnitAttackDefinitionId("alchemist")),
            new BattleUnitAttackHitEvent(alchemist.Id, ally.Id, new UnitAttackDefinitionId("alchemist"), 0),
            new BattleUnitAttackGrantedEvent(ally.Id),
            new BattleUnitTurnEndingEvent(alchemist.Id),
            new BattleUnitTurnEndedEvent(alchemist.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(ally.Id, BattleUnitTurnType.GrantedAttack))
        ]);

        BattleState expected = engine.State with
        {
            CurrentUnitTurn = new BattleUnitTurn(ally.Id, BattleUnitTurnType.GrantedAttack),
            TurnQueue = [new BattleUnitTurn(enemy.Id, BattleUnitTurnType.Normal), new BattleUnitTurn(ally.Id, BattleUnitTurnType.Normal)]
        };

        Assert.That(engine.State, Is.EqualTo(expected));
    }

    [Test]
    public void ExtraAttackActsThisRoundTest()
    {
        UnitDefinition alchemistDefinition = AlchemistDefinition(UnitAttackReach.Any);
        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ally", maxHitpoints: 100, initiative: 20, damage: 5) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("ally"), initiative: 20, damage: 5, accuracy: 100, reach: UnitAttackReach.Any)
        };
        UnitDefinition enemyDefinition = TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 100, initiative: 40, damage: 5);

        BattleUnit alchemist = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, alchemistDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit ally = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, allyDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit enemy = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, enemyDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(alchemistDefinition)
            .WithDefinition(allyDefinition)
            .WithDefinition(enemyDefinition)
            .WithAttacker(alchemist, GridPosition.FrontBottom)
            .WithAttacker(ally, GridPosition.BackBottom)
            .WithDefender(enemy, GridPosition.FrontBottom)
            .Build();

        _ = engine.DispatchCommand(new BattleAttackCommand(alchemist.Id, new BattleAttackUnitTarget(ally.Id)));
        _ = engine.DispatchCommand(new BattleAttackCommand(ally.Id, new BattleAttackUnitTarget(enemy.Id)));

        BattleState expected = engine.State
                .WithUnit(ally)
                .WithUnit(enemy with { Hitpoints = 95 })
            with
            {
                CurrentUnitTurn = new BattleUnitTurn(enemy.Id, BattleUnitTurnType.Normal),
                TurnQueue = [new BattleUnitTurn(ally.Id, BattleUnitTurnType.Normal)]
            };

        Assert.That(engine.State, Is.EqualTo(expected));
    }

    [Test]
    public void RetreatingAllyTest()
    {
        UnitDefinition alchemistDefinition = AlchemistDefinition(UnitAttackReach.Any);
        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ally", maxHitpoints: 100, initiative: 20, damage: 5);
        UnitDefinition enemyDefinition = TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 100, initiative: 40, damage: 5);

        BattleUnit alchemist = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, alchemistDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit ally = new(new BattleUnitId(1), null, allyDefinition.Id, BattleSide.Attacker, 100, 0, 0, 1, [], [new BattleRetreatStatusEffect()], null, [], null, false);
        BattleUnit enemy = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, enemyDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(alchemistDefinition)
            .WithDefinition(allyDefinition)
            .WithDefinition(enemyDefinition)
            .WithAttacker(alchemist, GridPosition.FrontBottom)
            .WithAttacker(ally, GridPosition.BackBottom)
            .WithDefender(enemy, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommandError(
            new BattleAttackCommand(alchemist.Id, new BattleAttackUnitTarget(ally.Id)),
            BattleCommandError.TargetIncorrect);
    }

    [Test]
    public void AllReachGrantsEveryAllyTest()
    {
        UnitDefinition alchemistDefinition = AlchemistDefinition(UnitAttackReach.All);
        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ally", maxHitpoints: 100, initiative: 20, damage: 5);
        UnitDefinition enemyDefinition = TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 100, initiative: 40, damage: 5);

        BattleUnit alchemist = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, alchemistDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit ally = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, allyDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit enemy = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, enemyDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(alchemistDefinition)
            .WithDefinition(allyDefinition)
            .WithDefinition(enemyDefinition)
            .WithAttacker(alchemist, GridPosition.FrontBottom)
            .WithAttacker(ally, GridPosition.BackBottom)
            .WithDefender(enemy, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(alchemist.Id, new BattleAttackUnitTarget(ally.Id)), [
            new BattleUnitAttackEvent(alchemist.Id, new BattleAttackUnitTarget(ally.Id), [new UnitAttackDefinitionId("alchemist")]),
            new BattleUnitAttackedEvent(alchemist.Id, ally.Id, new UnitAttackDefinitionId("alchemist")),
            new BattleUnitAttackHitEvent(alchemist.Id, ally.Id, new UnitAttackDefinitionId("alchemist"), 0),
            new BattleUnitAttackGrantedEvent(ally.Id),
            new BattleUnitTurnEndingEvent(alchemist.Id),
            new BattleUnitTurnEndedEvent(alchemist.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(ally.Id, BattleUnitTurnType.GrantedAttack))
        ]);

        BattleState expected = engine.State
                .WithUnit(alchemist)
            with
            {
                CurrentUnitTurn = new BattleUnitTurn(ally.Id, BattleUnitTurnType.GrantedAttack),
                TurnQueue = [new BattleUnitTurn(enemy.Id, BattleUnitTurnType.Normal), new BattleUnitTurn(ally.Id, BattleUnitTurnType.Normal)]
            };

        Assert.That(engine.State, Is.EqualTo(expected));
    }

    [Test]
    public void CannotGiveAttackToEnemyTest()
    {
        UnitDefinition alchemistDefinition = AlchemistDefinition(UnitAttackReach.Any);
        UnitDefinition enemyDefinition = TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 100, initiative: 40, damage: 5);

        BattleUnit alchemist = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, alchemistDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit enemy = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, enemyDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(alchemistDefinition)
            .WithDefinition(enemyDefinition)
            .WithAttacker(alchemist, GridPosition.FrontBottom)
            .WithDefender(enemy, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommandError(
            new BattleAttackCommand(alchemist.Id, new BattleAttackUnitTarget(enemy.Id)),
            BattleCommandError.TargetIncorrect);
    }

    [Test]
    public void CannotGiveAttackToSelfTest()
    {
        UnitDefinition alchemistDefinition = AlchemistDefinition(UnitAttackReach.Any);
        UnitDefinition enemyDefinition = TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 100, initiative: 40, damage: 5);

        BattleUnit alchemist = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, alchemistDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit enemy = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, enemyDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(alchemistDefinition)
            .WithDefinition(enemyDefinition)
            .WithAttacker(alchemist, GridPosition.FrontBottom)
            .WithDefender(enemy, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommandError(
            new BattleAttackCommand(alchemist.Id, new BattleAttackUnitTarget(alchemist.Id)),
            BattleCommandError.CannotGiveAttackToSelf);
    }

    [Test]
    public void CannotGiveAttackToDeadAllyTest()
    {
        UnitDefinition alchemistDefinition = AlchemistDefinition(UnitAttackReach.Any);
        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ally", maxHitpoints: 100, initiative: 20, damage: 5);
        UnitDefinition enemyDefinition = TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 100, initiative: 40, damage: 5);

        BattleUnit alchemist = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, alchemistDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit ally = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, allyDefinition.Id, BattleSide.Attacker, hitpoints: 0);
        BattleUnit enemy = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, enemyDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(alchemistDefinition)
            .WithDefinition(allyDefinition)
            .WithDefinition(enemyDefinition)
            .WithAttacker(alchemist, GridPosition.FrontBottom)
            .WithAttacker(ally, GridPosition.BackBottom)
            .WithDefender(enemy, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommandError(
            new BattleAttackCommand(alchemist.Id, new BattleAttackUnitTarget(ally.Id)),
            BattleCommandError.TargetDead);
    }

    [Test]
    public void CannotGiveAttackToTargetTest()
    {
        UnitDefinition alchemistDefinition = AlchemistDefinition(UnitAttackReach.Any);
        UnitDefinition otherAlchemistDefinition = TestHelpers.SimpleUnitDefinition("alchemist2", maxHitpoints: 100, initiative: 30, damage: 0) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("alchemist2"), initiative: 30, damage: 0, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Type = UnitAttackType.GiveAdditionalAttack
            }
        };
        UnitDefinition enemyDefinition = TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 100, initiative: 40, damage: 5);

        BattleUnit alchemist = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, alchemistDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit otherAlchemist = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, otherAlchemistDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit enemy = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, enemyDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(alchemistDefinition)
            .WithDefinition(otherAlchemistDefinition)
            .WithDefinition(enemyDefinition)
            .WithAttacker(alchemist, GridPosition.FrontBottom)
            .WithAttacker(otherAlchemist, GridPosition.BackBottom)
            .WithDefender(enemy, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommandError(
            new BattleAttackCommand(alchemist.Id, new BattleAttackUnitTarget(otherAlchemist.Id)),
            BattleCommandError.CannotGiveAttackToTarget);
    }

    [Test]
    public void AllReachGrantSkipsGiversTest()
    {
        UnitDefinition alchemistDefinition = AlchemistDefinition(UnitAttackReach.All);
        UnitDefinition otherAlchemistDefinition = TestHelpers.SimpleUnitDefinition("alchemist2", maxHitpoints: 100, initiative: 30, damage: 0) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("alchemist2"), initiative: 30, damage: 0, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Type = UnitAttackType.GiveAdditionalAttack
            }
        };
        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ally", maxHitpoints: 100, initiative: 20, damage: 5);
        UnitDefinition enemyDefinition = TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 100, initiative: 40, damage: 5);

        BattleUnit alchemist = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, alchemistDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit otherAlchemist = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, otherAlchemistDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit ally = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, allyDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit enemy = TestHelpers.SimpleBattleUnit(new BattleUnitId(3), null, enemyDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(alchemistDefinition)
            .WithDefinition(otherAlchemistDefinition)
            .WithDefinition(allyDefinition)
            .WithDefinition(enemyDefinition)
            .WithAttacker(alchemist, GridPosition.FrontBottom)
            .WithAttacker(otherAlchemist, GridPosition.BackBottom)
            .WithAttacker(ally, GridPosition.FrontCenter)
            .WithDefender(enemy, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(alchemist.Id, new BattleAttackUnitTarget(ally.Id)), [
            new BattleUnitAttackEvent(alchemist.Id, new BattleAttackUnitTarget(ally.Id), [new UnitAttackDefinitionId("alchemist")]),
            new BattleUnitAttackedEvent(alchemist.Id, ally.Id, new UnitAttackDefinitionId("alchemist")),
            new BattleUnitAttackHitEvent(alchemist.Id, ally.Id, new UnitAttackDefinitionId("alchemist"), 0),
            new BattleUnitAttackGrantedEvent(ally.Id),
            new BattleUnitTurnEndingEvent(alchemist.Id),
            new BattleUnitTurnEndedEvent(alchemist.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(ally.Id, BattleUnitTurnType.GrantedAttack))
        ]);

        BattleState expected = engine.State
                .WithUnit(otherAlchemist)
            with
            {
                CurrentUnitTurn = new BattleUnitTurn(ally.Id, BattleUnitTurnType.GrantedAttack),
                TurnQueue = [new BattleUnitTurn(enemy.Id, BattleUnitTurnType.Normal), new BattleUnitTurn(otherAlchemist.Id, BattleUnitTurnType.Normal), new BattleUnitTurn(ally.Id, BattleUnitTurnType.Normal)]
            };

        Assert.That(engine.State, Is.EqualTo(expected));
    }

    [Test]
    public void PoisonTicksOnceTest()
    {
        UnitDefinition alchemistDefinition = AlchemistDefinition(UnitAttackReach.Any);
        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ally", maxHitpoints: 20, initiative: 20, damage: 5);
        UnitDefinition enemyDefinition = TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 100, initiative: 40, damage: 5);

        BattleUnit alchemist = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, alchemistDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit ally = new(new BattleUnitId(1), null, allyDefinition.Id, BattleSide.Attacker, 20, 0, 0, 1, [], [new BattlePoisonStatusEffect(3, 2)], null, [], null, false);
        BattleUnit enemy = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, enemyDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(alchemistDefinition)
            .WithDefinition(allyDefinition)
            .WithDefinition(enemyDefinition)
            .WithAttacker(alchemist, GridPosition.FrontBottom)
            .WithAttacker(ally, GridPosition.BackBottom)
            .WithDefender(enemy, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(alchemist.Id, new BattleAttackUnitTarget(ally.Id)), [
            new BattleUnitAttackEvent(alchemist.Id, new BattleAttackUnitTarget(ally.Id), [new UnitAttackDefinitionId("alchemist")]),
            new BattleUnitAttackedEvent(alchemist.Id, ally.Id, new UnitAttackDefinitionId("alchemist")),
            new BattleUnitAttackHitEvent(alchemist.Id, ally.Id, new UnitAttackDefinitionId("alchemist"), 0),
            new BattleUnitAttackGrantedEvent(ally.Id),
            new BattleUnitTurnEndingEvent(alchemist.Id),
            new BattleUnitTurnEndedEvent(alchemist.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(ally.Id, BattleUnitTurnType.GrantedAttack))
        ]);

        engine.CheckCommand(new BattleSkipCommand(ally.Id), [
            new BattleUnitTurnEndingEvent(ally.Id),
            new BattleUnitTurnEndedEvent(ally.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(enemy.Id, BattleUnitTurnType.Normal))
        ]);

        engine.CheckCommand(new BattleSkipCommand(enemy.Id), [
            new BattleUnitTurnEndingEvent(enemy.Id),
            new BattleUnitTurnEndedEvent(enemy.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(ally.Id, BattleUnitTurnType.Normal)),
            new BattleUnitDamagedByEffectEvent(ally.Id, new BattlePoisonStatusEffect(3, 2), 2, 18),
            new BattleUnitEffectDurationDecreasedEvent(ally.Id)
        ]);

        BattleState expected = engine.State
                .WithUnit(ally with { Hitpoints = 18, StatusEffects = [new BattlePoisonStatusEffect(2, 2)] })
            with
            {
                CurrentUnitTurn = new BattleUnitTurn(ally.Id, BattleUnitTurnType.Normal),
                TurnQueue = []
            };

        Assert.That(engine.State, Is.EqualTo(expected));
    }

    [Test]
    public void AttacksTwiceAllyTest()
    {
        UnitDefinition alchemistDefinition = AlchemistDefinition(UnitAttackReach.Any);
        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ally", maxHitpoints: 100, initiative: 20, damage: 5) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("ally"), initiative: 20, damage: 5, accuracy: 100, reach: UnitAttackReach.Any),
            AttacksTwice = true
        };
        UnitDefinition enemyDefinition = TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 100, initiative: 40, damage: 5);

        BattleUnit alchemist = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, alchemistDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit ally = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, allyDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit enemy = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, enemyDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(alchemistDefinition)
            .WithDefinition(allyDefinition)
            .WithDefinition(enemyDefinition)
            .WithAttacker(alchemist, GridPosition.FrontBottom)
            .WithAttacker(ally, GridPosition.BackBottom)
            .WithDefender(enemy, GridPosition.FrontBottom)
            .Build();

        _ = engine.DispatchCommand(new BattleAttackCommand(alchemist.Id, new BattleAttackUnitTarget(ally.Id)));

        engine.CheckCommand(new BattleAttackCommand(ally.Id, new BattleAttackUnitTarget(enemy.Id)), [
            new BattleUnitAttackEvent(ally.Id, new BattleAttackUnitTarget(enemy.Id), [new UnitAttackDefinitionId("ally")]),
            new BattleUnitAttackedEvent(ally.Id, enemy.Id, new UnitAttackDefinitionId("ally")),
            new BattleUnitAttackHitEvent(ally.Id, enemy.Id, new UnitAttackDefinitionId("ally"), 5),
            new BattleUnitTurnEndingEvent(ally.Id),
            new BattleUnitTurnEndedEvent(ally.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(ally.Id, BattleUnitTurnType.SecondAttack))
        ]);

        engine.CheckCommand(new BattleAttackCommand(ally.Id, new BattleAttackUnitTarget(enemy.Id)), [
            new BattleUnitAttackEvent(ally.Id, new BattleAttackUnitTarget(enemy.Id), [new UnitAttackDefinitionId("ally")]),
            new BattleUnitAttackedEvent(ally.Id, enemy.Id, new UnitAttackDefinitionId("ally")),
            new BattleUnitAttackHitEvent(ally.Id, enemy.Id, new UnitAttackDefinitionId("ally"), 5),
            new BattleUnitTurnEndingEvent(ally.Id),
            new BattleUnitTurnEndedEvent(ally.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(enemy.Id, BattleUnitTurnType.Normal))
        ]);

        BattleState expected = engine.State
                .WithUnit(ally)
                .WithUnit(enemy with { Hitpoints = 90 })
            with
            {
                CurrentUnitTurn = new BattleUnitTurn(enemy.Id, BattleUnitTurnType.Normal),
                TurnQueue = [new BattleUnitTurn(ally.Id, BattleUnitTurnType.Normal)]
            };

        Assert.That(engine.State, Is.EqualTo(expected));
    }
}
