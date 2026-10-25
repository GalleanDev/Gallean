using System.Collections.Immutable;

namespace Gallean.Engine.Tests;

public sealed class BattleEngineGiveProtectionAttackTests
{
    private static readonly UnitWard FireWard = new UnitAttackSourceWard(UnitAttackSource.Fire, UnitWardCategory.Once);
    private static readonly UnitWard AirWard = new UnitAttackSourceWard(UnitAttackSource.Air, UnitWardCategory.Once);

    private static UnitDefinition ProtectorDefinition(UnitAttackReach reach, ImmutableArray<UnitModifierDefinitionId> wardModifierIds)
    {
        return TestHelpers.SimpleUnitDefinition("protector", maxHitpoints: 100, initiative: 60, damage: 0) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("protector"), initiative: 60, damage: 0, accuracy: 100, reach: reach) with
            {
                Type = UnitAttackType.GiveProtection,
                WardModifierIds = wardModifierIds
            }
        };
    }

    private static UnitModifierDefinition WardModifier(string id, UnitWard ward)
    {
        return TestHelpers.SimpleModifier(id) with { Wards = [ward] };
    }

    [Test]
    public void GrantsWardTest()
    {
        UnitModifierDefinition fireWardModifier = WardModifier("fireward", FireWard);
        UnitDefinition protectorDefinition = ProtectorDefinition(UnitAttackReach.Any, [fireWardModifier.Id]);
        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ally", maxHitpoints: 100, initiative: 20, damage: 5);
        UnitDefinition enemyDefinition = TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 100, initiative: 40, damage: 5);

        BattleUnit protector = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, protectorDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit ally = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, allyDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit enemy = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, enemyDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(protectorDefinition)
            .WithDefinition(allyDefinition)
            .WithDefinition(enemyDefinition)
            .WithDefinition(fireWardModifier)
            .WithAttacker(protector, GridPosition.FrontBottom)
            .WithAttacker(ally, GridPosition.BackBottom)
            .WithDefender(enemy, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(protector.Id, new BattleAttackUnitTarget(ally.Id)), [
            new BattleUnitAttackEvent(protector.Id, new BattleAttackUnitTarget(ally.Id), [new UnitAttackDefinitionId("protector")]),
            new BattleUnitAttackedEvent(protector.Id, ally.Id, new UnitAttackDefinitionId("protector")),
            new BattleUnitAttackHitEvent(protector.Id, ally.Id, new UnitAttackDefinitionId("protector"), 0),
            new BattleUnitWardGrantedEvent(ally.Id, new BattleUnitWardModifier(protector.Id, fireWardModifier.Id, FireWard)),
            new BattleUnitTurnEndingEvent(protector.Id),
            new BattleUnitTurnEndedEvent(protector.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(enemy.Id, BattleUnitTurnType.Normal))
        ]);

        BattleState expected = engine.State
                .WithUnit(ally with
                {
                    Wards = [FireWard],
                    WardModifiers = [new BattleUnitWardModifier(protector.Id, fireWardModifier.Id, FireWard)]
                })
            with
            {
                CurrentUnitTurn = new BattleUnitTurn(enemy.Id, BattleUnitTurnType.Normal),
                TurnQueue = [new BattleUnitTurn(ally.Id, BattleUnitTurnType.Normal)]
            };

        Assert.That(engine.State, Is.EqualTo(expected));
    }

    [Test]
    public void GrantedWardBlocksAttackTest()
    {
        UnitModifierDefinition fireWardModifier = WardModifier("fireward", FireWard);
        UnitDefinition protectorDefinition = ProtectorDefinition(UnitAttackReach.Any, [fireWardModifier.Id]);
        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ally", maxHitpoints: 100, initiative: 20, damage: 5);
        UnitDefinition enemyDefinition = TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 100, initiative: 40, damage: 5) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("enemy"), initiative: 40, damage: 5, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Source = UnitAttackSource.Fire
            }
        };

        BattleUnit protector = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, protectorDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit ally = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, allyDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit enemy = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, enemyDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(protectorDefinition)
            .WithDefinition(allyDefinition)
            .WithDefinition(enemyDefinition)
            .WithDefinition(fireWardModifier)
            .WithAttacker(protector, GridPosition.FrontBottom)
            .WithAttacker(ally, GridPosition.BackBottom)
            .WithDefender(enemy, GridPosition.FrontBottom)
            .Build();

        _ = engine.DispatchCommand(new BattleAttackCommand(protector.Id, new BattleAttackUnitTarget(ally.Id)));

        engine.CheckCommand(new BattleAttackCommand(enemy.Id, new BattleAttackUnitTarget(ally.Id)), [
            new BattleUnitAttackEvent(enemy.Id, new BattleAttackUnitTarget(ally.Id), [new UnitAttackDefinitionId("enemy")]),
            new BattleUnitAttackedEvent(enemy.Id, ally.Id, new UnitAttackDefinitionId("enemy")),
            new BattleUnitAttackBlockedEvent(enemy.Id, ally.Id, new UnitAttackDefinitionId("enemy"), FireWard),
            new BattleUnitWardConsumedEvent(ally.Id, FireWard),
            new BattleUnitTurnEndingEvent(enemy.Id),
            new BattleUnitTurnEndedEvent(enemy.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(ally.Id, BattleUnitTurnType.Normal))
        ]);

        BattleState expected = engine.State
                .WithUnit(protector)
                .WithUnit(ally with
                {
                    WardModifiers = [new BattleUnitWardModifier(protector.Id, fireWardModifier.Id, FireWard)]
                })
                .WithUnit(enemy)
            with
            {
                CurrentUnitTurn = new BattleUnitTurn(ally.Id, BattleUnitTurnType.Normal),
                TurnQueue = []
            };

        Assert.That(engine.State, Is.EqualTo(expected));
    }

    [Test]
    public void SkipsExistingWardTest()
    {
        UnitModifierDefinition fireWardModifier = WardModifier("fireward", FireWard);
        UnitModifierDefinition airWardModifier = WardModifier("airward", AirWard);
        UnitDefinition protectorDefinition = ProtectorDefinition(UnitAttackReach.Any, [fireWardModifier.Id, airWardModifier.Id]);
        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ally", maxHitpoints: 100, initiative: 20, damage: 5) with
        {
            Wards = [FireWard]
        };
        UnitDefinition enemyDefinition = TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 100, initiative: 40, damage: 5);

        BattleUnit protector = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, protectorDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit ally = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, allyDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit enemy = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, enemyDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(protectorDefinition)
            .WithDefinition(allyDefinition)
            .WithDefinition(enemyDefinition)
            .WithDefinition(fireWardModifier)
            .WithDefinition(airWardModifier)
            .WithAttacker(protector, GridPosition.FrontBottom)
            .WithAttacker(ally, GridPosition.BackBottom)
            .WithDefender(enemy, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(protector.Id, new BattleAttackUnitTarget(ally.Id)), [
            new BattleUnitAttackEvent(protector.Id, new BattleAttackUnitTarget(ally.Id), [new UnitAttackDefinitionId("protector")]),
            new BattleUnitAttackedEvent(protector.Id, ally.Id, new UnitAttackDefinitionId("protector")),
            new BattleUnitAttackHitEvent(protector.Id, ally.Id, new UnitAttackDefinitionId("protector"), 0),
            new BattleUnitWardGrantedEvent(ally.Id, new BattleUnitWardModifier(protector.Id, airWardModifier.Id, AirWard)),
            new BattleUnitTurnEndingEvent(protector.Id),
            new BattleUnitTurnEndedEvent(protector.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(enemy.Id, BattleUnitTurnType.Normal))
        ]);

        BattleState expected = engine.State
                .WithUnit(ally with
                {
                    Wards = [FireWard, AirWard],
                    WardModifiers = [new BattleUnitWardModifier(protector.Id, airWardModifier.Id, AirWard)]
                })
            with
            {
                CurrentUnitTurn = new BattleUnitTurn(enemy.Id, BattleUnitTurnType.Normal),
                TurnQueue = [new BattleUnitTurn(ally.Id, BattleUnitTurnType.Normal)]
            };

        Assert.That(engine.State, Is.EqualTo(expected));
    }

    [Test]
    public void ProtectsSelfTest()
    {
        UnitModifierDefinition fireWardModifier = WardModifier("fireward", FireWard);
        UnitDefinition protectorDefinition = ProtectorDefinition(UnitAttackReach.Any, [fireWardModifier.Id]);
        UnitDefinition enemyDefinition = TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 100, initiative: 40, damage: 5);

        BattleUnit protector = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, protectorDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit enemy = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, enemyDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(protectorDefinition)
            .WithDefinition(enemyDefinition)
            .WithDefinition(fireWardModifier)
            .WithAttacker(protector, GridPosition.FrontBottom)
            .WithDefender(enemy, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(protector.Id, new BattleAttackUnitTarget(protector.Id)), [
            new BattleUnitAttackEvent(protector.Id, new BattleAttackUnitTarget(protector.Id), [new UnitAttackDefinitionId("protector")]),
            new BattleUnitAttackedEvent(protector.Id, protector.Id, new UnitAttackDefinitionId("protector")),
            new BattleUnitAttackHitEvent(protector.Id, protector.Id, new UnitAttackDefinitionId("protector"), 0),
            new BattleUnitWardGrantedEvent(protector.Id, new BattleUnitWardModifier(protector.Id, fireWardModifier.Id, FireWard)),
            new BattleUnitTurnEndingEvent(protector.Id),
            new BattleUnitTurnEndedEvent(protector.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(enemy.Id, BattleUnitTurnType.Normal))
        ]);

        BattleState expected = engine.State
                .WithUnit(protector with
                {
                    Wards = [FireWard],
                    WardModifiers = [new BattleUnitWardModifier(protector.Id, fireWardModifier.Id, FireWard)]
                })
            with
            {
                CurrentUnitTurn = new BattleUnitTurn(enemy.Id, BattleUnitTurnType.Normal),
                TurnQueue = []
            };

        Assert.That(engine.State, Is.EqualTo(expected));
    }

    [Test]
    public void AllReachProtectsEveryAllyTest()
    {
        UnitModifierDefinition fireWardModifier = WardModifier("fireward", FireWard);
        UnitDefinition protectorDefinition = ProtectorDefinition(UnitAttackReach.All, [fireWardModifier.Id]);
        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ally", maxHitpoints: 100, initiative: 20, damage: 5);
        UnitDefinition enemyDefinition = TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 100, initiative: 40, damage: 5);

        BattleUnit protector = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, protectorDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit ally = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, allyDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit enemy = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, enemyDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(protectorDefinition)
            .WithDefinition(allyDefinition)
            .WithDefinition(enemyDefinition)
            .WithDefinition(fireWardModifier)
            .WithAttacker(protector, GridPosition.FrontBottom)
            .WithAttacker(ally, GridPosition.BackBottom)
            .WithDefender(enemy, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(protector.Id, new BattleAttackUnitTarget(ally.Id)), [
            new BattleUnitAttackEvent(protector.Id, new BattleAttackUnitTarget(ally.Id), [new UnitAttackDefinitionId("protector")]),
            new BattleUnitAttackedEvent(protector.Id, protector.Id, new UnitAttackDefinitionId("protector")),
            new BattleUnitAttackHitEvent(protector.Id, protector.Id, new UnitAttackDefinitionId("protector"), 0),
            new BattleUnitWardGrantedEvent(protector.Id, new BattleUnitWardModifier(protector.Id, fireWardModifier.Id, FireWard)),
            new BattleUnitAttackedEvent(protector.Id, ally.Id, new UnitAttackDefinitionId("protector")),
            new BattleUnitAttackHitEvent(protector.Id, ally.Id, new UnitAttackDefinitionId("protector"), 0),
            new BattleUnitWardGrantedEvent(ally.Id, new BattleUnitWardModifier(protector.Id, fireWardModifier.Id, FireWard)),
            new BattleUnitTurnEndingEvent(protector.Id),
            new BattleUnitTurnEndedEvent(protector.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(enemy.Id, BattleUnitTurnType.Normal))
        ]);

        BattleState expected = engine.State
                .WithUnit(protector with
                {
                    Wards = [FireWard],
                    WardModifiers = [new BattleUnitWardModifier(protector.Id, fireWardModifier.Id, FireWard)]
                })
                .WithUnit(ally with
                {
                    Wards = [FireWard],
                    WardModifiers = [new BattleUnitWardModifier(protector.Id, fireWardModifier.Id, FireWard)]
                })
                .WithUnit(enemy) with
            {
                CurrentUnitTurn = new BattleUnitTurn(enemy.Id, BattleUnitTurnType.Normal),
                TurnQueue = [new BattleUnitTurn(ally.Id, BattleUnitTurnType.Normal)]
            };

        Assert.That(engine.State, Is.EqualTo(expected));
    }

    [Test]
    public void AlreadyProtectedTest()
    {
        UnitModifierDefinition fireWardModifier = WardModifier("fireward", FireWard);
        UnitDefinition protectorDefinition = ProtectorDefinition(UnitAttackReach.Any, [fireWardModifier.Id]);
        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ally", maxHitpoints: 100, initiative: 20, damage: 5) with
        {
            Wards = [FireWard]
        };
        UnitDefinition enemyDefinition = TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 100, initiative: 40, damage: 5);

        BattleUnit protector = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, protectorDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit ally = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, allyDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit enemy = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, enemyDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(protectorDefinition)
            .WithDefinition(allyDefinition)
            .WithDefinition(enemyDefinition)
            .WithDefinition(fireWardModifier)
            .WithAttacker(protector, GridPosition.FrontBottom)
            .WithAttacker(ally, GridPosition.BackBottom)
            .WithDefender(enemy, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommandError(
            new BattleAttackCommand(protector.Id, new BattleAttackUnitTarget(ally.Id)),
            BattleCommandError.TargetIncorrect);
    }

    [Test]
    public void CannotProtectEnemyTest()
    {
        UnitModifierDefinition fireWardModifier = WardModifier("fireward", FireWard);
        UnitDefinition protectorDefinition = ProtectorDefinition(UnitAttackReach.Any, [fireWardModifier.Id]);
        UnitDefinition enemyDefinition = TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 100, initiative: 40, damage: 5);

        BattleUnit protector = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, protectorDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit enemy = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, enemyDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(protectorDefinition)
            .WithDefinition(enemyDefinition)
            .WithDefinition(fireWardModifier)
            .WithAttacker(protector, GridPosition.FrontBottom)
            .WithDefender(enemy, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommandError(
            new BattleAttackCommand(protector.Id, new BattleAttackUnitTarget(enemy.Id)),
            BattleCommandError.TargetIncorrect);
    }

    [Test]
    public void DeadTargetTest()
    {
        UnitModifierDefinition fireWardModifier = WardModifier("fireward", FireWard);
        UnitDefinition protectorDefinition = ProtectorDefinition(UnitAttackReach.Any, [fireWardModifier.Id]);
        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ally", maxHitpoints: 100, initiative: 20, damage: 5);
        UnitDefinition enemyDefinition = TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 100, initiative: 40, damage: 5);

        BattleUnit protector = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, protectorDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit ally = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, allyDefinition.Id, BattleSide.Attacker, hitpoints: 0);
        BattleUnit enemy = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, enemyDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(protectorDefinition)
            .WithDefinition(allyDefinition)
            .WithDefinition(enemyDefinition)
            .WithDefinition(fireWardModifier)
            .WithAttacker(protector, GridPosition.FrontBottom)
            .WithAttacker(ally, GridPosition.BackBottom)
            .WithDefender(enemy, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommandError(
            new BattleAttackCommand(protector.Id, new BattleAttackUnitTarget(ally.Id)),
            BattleCommandError.TargetDead);
    }

    [Test]
    public void ExpiresAtCasterNextTurnTest()
    {
        UnitModifierDefinition fireWardModifier = WardModifier("fireward", FireWard);
        UnitDefinition protectorDefinition = ProtectorDefinition(UnitAttackReach.Any, [fireWardModifier.Id]);
        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ally", maxHitpoints: 100, initiative: 20, damage: 5);
        UnitDefinition enemyDefinition = TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 100, initiative: 40, damage: 5);

        BattleUnit protector = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, protectorDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit ally = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, allyDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit enemy = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, enemyDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(protectorDefinition)
            .WithDefinition(allyDefinition)
            .WithDefinition(enemyDefinition)
            .WithDefinition(fireWardModifier)
            .WithAttacker(protector, GridPosition.FrontBottom)
            .WithAttacker(ally, GridPosition.BackBottom)
            .WithDefender(enemy, GridPosition.FrontBottom)
            .Build();

        _ = engine.DispatchCommand(new BattleAttackCommand(protector.Id, new BattleAttackUnitTarget(ally.Id)));
        _ = engine.DispatchCommand(new BattleAttackCommand(enemy.Id, new BattleAttackUnitTarget(protector.Id)));

        engine.CheckCommand(new BattleAttackCommand(ally.Id, new BattleAttackUnitTarget(enemy.Id)), [
            new BattleUnitAttackEvent(ally.Id, new BattleAttackUnitTarget(enemy.Id), [new UnitAttackDefinitionId("ally")]),
            new BattleUnitAttackedEvent(ally.Id, enemy.Id, new UnitAttackDefinitionId("ally")),
            new BattleUnitAttackHitEvent(ally.Id, enemy.Id, new UnitAttackDefinitionId("ally"), 5),
            new BattleUnitTurnEndingEvent(ally.Id),
            new BattleUnitTurnEndedEvent(ally.Id),
            new BattleRoundEndedEvent(),
            new BattleRoundStartedEvent(BattleRound.Normal(2), [new BattleUnitTurn(protector.Id, BattleUnitTurnType.Normal), new BattleUnitTurn(enemy.Id, BattleUnitTurnType.Normal), new BattleUnitTurn(ally.Id, BattleUnitTurnType.Normal)]),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(protector.Id, BattleUnitTurnType.Normal)),
            new BattleUnitWardExpiredEvent(ally.Id, new BattleUnitWardModifier(protector.Id, fireWardModifier.Id, FireWard))
        ]);

        BattleState expected = engine.State
                .WithUnit(protector with { Hitpoints = 95 })
                .WithUnit(ally)
                .WithUnit(enemy with { Hitpoints = 95 })
            with
            {
                CurrentUnitTurn = new BattleUnitTurn(protector.Id, BattleUnitTurnType.Normal),
                TurnQueue = [new BattleUnitTurn(enemy.Id, BattleUnitTurnType.Normal), new BattleUnitTurn(ally.Id, BattleUnitTurnType.Normal)],
                CurrentRound = BattleRound.Normal(2)
            };

        Assert.That(engine.State, Is.EqualTo(expected));
    }

    [Test]
    public void ExpiresConsumedWardTest()
    {
        UnitModifierDefinition fireWardModifier = WardModifier("fireward", FireWard);
        UnitDefinition protectorDefinition = ProtectorDefinition(UnitAttackReach.Any, [fireWardModifier.Id]);
        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ally", maxHitpoints: 100, initiative: 20, damage: 5);
        UnitDefinition enemyDefinition = TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 100, initiative: 40, damage: 5) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("enemy"), initiative: 40, damage: 5, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Source = UnitAttackSource.Fire
            }
        };

        BattleUnit protector = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, protectorDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit ally = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, allyDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit enemy = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, enemyDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(protectorDefinition)
            .WithDefinition(allyDefinition)
            .WithDefinition(enemyDefinition)
            .WithDefinition(fireWardModifier)
            .WithAttacker(protector, GridPosition.FrontBottom)
            .WithAttacker(ally, GridPosition.BackBottom)
            .WithDefender(enemy, GridPosition.FrontBottom)
            .Build();

        _ = engine.DispatchCommand(new BattleAttackCommand(protector.Id, new BattleAttackUnitTarget(ally.Id)));
        _ = engine.DispatchCommand(new BattleAttackCommand(enemy.Id, new BattleAttackUnitTarget(ally.Id)));

        engine.CheckCommand(new BattleAttackCommand(ally.Id, new BattleAttackUnitTarget(enemy.Id)), [
            new BattleUnitAttackEvent(ally.Id, new BattleAttackUnitTarget(enemy.Id), [new UnitAttackDefinitionId("ally")]),
            new BattleUnitAttackedEvent(ally.Id, enemy.Id, new UnitAttackDefinitionId("ally")),
            new BattleUnitAttackHitEvent(ally.Id, enemy.Id, new UnitAttackDefinitionId("ally"), 5),
            new BattleUnitTurnEndingEvent(ally.Id),
            new BattleUnitTurnEndedEvent(ally.Id),
            new BattleRoundEndedEvent(),
            new BattleRoundStartedEvent(BattleRound.Normal(2), [new BattleUnitTurn(protector.Id, BattleUnitTurnType.Normal), new BattleUnitTurn(enemy.Id, BattleUnitTurnType.Normal), new BattleUnitTurn(ally.Id, BattleUnitTurnType.Normal)]),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(protector.Id, BattleUnitTurnType.Normal)),
            new BattleUnitWardExpiredEvent(ally.Id, new BattleUnitWardModifier(protector.Id, fireWardModifier.Id, FireWard))
        ]);

        BattleState expected = engine.State
                .WithUnit(protector)
                .WithUnit(ally)
                .WithUnit(enemy with { Hitpoints = 95 })
            with
            {
                CurrentUnitTurn = new BattleUnitTurn(protector.Id, BattleUnitTurnType.Normal),
                TurnQueue = [new BattleUnitTurn(enemy.Id, BattleUnitTurnType.Normal), new BattleUnitTurn(ally.Id, BattleUnitTurnType.Normal)],
                CurrentRound = BattleRound.Normal(2)
            };

        Assert.That(engine.State, Is.EqualTo(expected));
    }

    [Test]
    public void AttacksTwiceCasterTest()
    {
        UnitModifierDefinition fireWardModifier = WardModifier("fireward", FireWard);
        UnitDefinition protectorDefinition = ProtectorDefinition(UnitAttackReach.Any, [fireWardModifier.Id]) with
        {
            AttacksTwice = true
        };
        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ally", maxHitpoints: 100, initiative: 20, damage: 5);
        UnitDefinition enemyDefinition = TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 100, initiative: 40, damage: 5);

        BattleUnit protector = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, protectorDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit ally = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, allyDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit enemy = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, enemyDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(protectorDefinition)
            .WithDefinition(allyDefinition)
            .WithDefinition(enemyDefinition)
            .WithDefinition(fireWardModifier)
            .WithAttacker(protector, GridPosition.FrontBottom)
            .WithAttacker(ally, GridPosition.BackBottom)
            .WithDefender(enemy, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(protector.Id, new BattleAttackUnitTarget(ally.Id)), [
            new BattleUnitAttackEvent(protector.Id, new BattleAttackUnitTarget(ally.Id), [new UnitAttackDefinitionId("protector")]),
            new BattleUnitAttackedEvent(protector.Id, ally.Id, new UnitAttackDefinitionId("protector")),
            new BattleUnitAttackHitEvent(protector.Id, ally.Id, new UnitAttackDefinitionId("protector"), 0),
            new BattleUnitWardGrantedEvent(ally.Id, new BattleUnitWardModifier(protector.Id, fireWardModifier.Id, FireWard)),
            new BattleUnitTurnEndingEvent(protector.Id),
            new BattleUnitTurnEndedEvent(protector.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(protector.Id, BattleUnitTurnType.SecondAttack))
        ]);

        BattleState expected = engine.State
                .WithUnit(ally with
                {
                    Wards = [FireWard],
                    WardModifiers = [new BattleUnitWardModifier(protector.Id, fireWardModifier.Id, FireWard)]
                })
            with
            {
                CurrentUnitTurn = new BattleUnitTurn(protector.Id, BattleUnitTurnType.SecondAttack),
                TurnQueue = [new BattleUnitTurn(enemy.Id, BattleUnitTurnType.Normal), new BattleUnitTurn(ally.Id, BattleUnitTurnType.Normal)]
            };

        Assert.That(engine.State, Is.EqualTo(expected));

        engine.CheckCommand(new BattleSkipCommand(protector.Id), [
            new BattleUnitTurnSkippedEvent(protector.Id),
            new BattleUnitTurnEndingEvent(protector.Id),
            new BattleUnitTurnEndedEvent(protector.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(enemy.Id, BattleUnitTurnType.Normal))
        ]);

        engine.CheckCommand(new BattleSkipCommand(enemy.Id), [
            new BattleUnitTurnSkippedEvent(enemy.Id),
            new BattleUnitTurnEndingEvent(enemy.Id),
            new BattleUnitTurnEndedEvent(enemy.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(ally.Id, BattleUnitTurnType.Normal))
        ]);

        engine.CheckCommand(new BattleSkipCommand(ally.Id), [
            new BattleUnitTurnSkippedEvent(ally.Id),
            new BattleUnitTurnEndingEvent(ally.Id),
            new BattleUnitTurnEndedEvent(ally.Id),
            new BattleRoundEndedEvent(),
            new BattleRoundStartedEvent(BattleRound.Normal(2), [new BattleUnitTurn(protector.Id, BattleUnitTurnType.Normal), new BattleUnitTurn(enemy.Id, BattleUnitTurnType.Normal), new BattleUnitTurn(ally.Id, BattleUnitTurnType.Normal)]),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(protector.Id, BattleUnitTurnType.Normal)),
            new BattleUnitWardExpiredEvent(ally.Id, new BattleUnitWardModifier(protector.Id, fireWardModifier.Id, FireWard))
        ]);

        BattleState expectedAfterRound = engine.State
                .WithUnit(protector)
                .WithUnit(ally)
            with
            {
                CurrentUnitTurn = new BattleUnitTurn(protector.Id, BattleUnitTurnType.Normal),
                TurnQueue = [new BattleUnitTurn(enemy.Id, BattleUnitTurnType.Normal), new BattleUnitTurn(ally.Id, BattleUnitTurnType.Normal)],
                CurrentRound = BattleRound.Normal(2)
            };

        Assert.That(engine.State, Is.EqualTo(expectedAfterRound));
    }

    [Test]
    public void GrantedAttackCasterTest()
    {
        UnitModifierDefinition fireWardModifier = WardModifier("fireward", FireWard);
        UnitDefinition protectorDefinition = ProtectorDefinition(UnitAttackReach.Any, [fireWardModifier.Id]);
        UnitDefinition alchemistDefinition = TestHelpers.SimpleUnitDefinition("alchemist", maxHitpoints: 100, initiative: 50, damage: 0) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("alchemist"), initiative: 50, damage: 0, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Type = UnitAttackType.GiveAdditionalAttack
            }
        };
        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ally", maxHitpoints: 100, initiative: 20, damage: 5);
        UnitDefinition enemyDefinition = TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 100, initiative: 40, damage: 5);

        BattleUnit protector = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, protectorDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit alchemist = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, alchemistDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit ally = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, allyDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit enemy = TestHelpers.SimpleBattleUnit(new BattleUnitId(3), null, enemyDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(protectorDefinition)
            .WithDefinition(alchemistDefinition)
            .WithDefinition(allyDefinition)
            .WithDefinition(enemyDefinition)
            .WithDefinition(fireWardModifier)
            .WithAttacker(protector, GridPosition.FrontBottom)
            .WithAttacker(alchemist, GridPosition.FrontCenter)
            .WithAttacker(ally, GridPosition.BackBottom)
            .WithDefender(enemy, GridPosition.FrontBottom)
            .Build();

        _ = engine.DispatchCommand(new BattleAttackCommand(protector.Id, new BattleAttackUnitTarget(ally.Id)));

        engine.CheckCommand(new BattleAttackCommand(alchemist.Id, new BattleAttackUnitTarget(protector.Id)), [
            new BattleUnitAttackEvent(alchemist.Id, new BattleAttackUnitTarget(protector.Id), [new UnitAttackDefinitionId("alchemist")]),
            new BattleUnitAttackedEvent(alchemist.Id, protector.Id, new UnitAttackDefinitionId("alchemist")),
            new BattleUnitAttackHitEvent(alchemist.Id, protector.Id, new UnitAttackDefinitionId("alchemist"), 0),
            new BattleUnitAttackGrantedEvent(protector.Id),
            new BattleUnitTurnEndingEvent(alchemist.Id),
            new BattleUnitTurnEndedEvent(alchemist.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(protector.Id, BattleUnitTurnType.GrantedAttack))
        ]);

        BattleState expected = engine.State
                .WithUnit(ally with
                {
                    Wards = [FireWard],
                    WardModifiers = [new BattleUnitWardModifier(protector.Id, fireWardModifier.Id, FireWard)]
                })
            with
            {
                CurrentUnitTurn = new BattleUnitTurn(protector.Id, BattleUnitTurnType.GrantedAttack),
                TurnQueue = [new BattleUnitTurn(enemy.Id, BattleUnitTurnType.Normal), new BattleUnitTurn(ally.Id, BattleUnitTurnType.Normal)]
            };

        Assert.That(engine.State, Is.EqualTo(expected));
    }

    [Test]
    public void ModifierLimitTest()
    {
        UnitModifierDefinition fireWardModifier = WardModifier("fireward", FireWard);
        UnitDefinition protectorDefinition = ProtectorDefinition(UnitAttackReach.Any, [fireWardModifier.Id]);
        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ally", maxHitpoints: 100, initiative: 20, damage: 5);
        UnitDefinition enemyDefinition = TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 100, initiative: 40, damage: 5);

        BattleUnit protector = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, protectorDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit ally = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, allyDefinition.Id, BattleSide.Attacker, hitpoints: 100) with
        {
            Wards =
            [
                new UnitAttackSourceWard(UnitAttackSource.Weapon, UnitWardCategory.Once),
                new UnitAttackSourceWard(UnitAttackSource.Mind, UnitWardCategory.Once),
                new UnitAttackSourceWard(UnitAttackSource.Life, UnitWardCategory.Once),
                new UnitAttackSourceWard(UnitAttackSource.Death, UnitWardCategory.Once),
                new UnitAttackSourceWard(UnitAttackSource.Water, UnitWardCategory.Once),
                new UnitAttackSourceWard(UnitAttackSource.Earth, UnitWardCategory.Once),
                new UnitAttackSourceWard(UnitAttackSource.Air, UnitWardCategory.Once),
                new UnitAttackClassWard(UnitAttackType.Paralyze, UnitWardCategory.Once)
            ],
            WardModifiers =
            [
                new BattleUnitWardModifier(new BattleUnitId(1), new UnitModifierDefinitionId("weaponward"), new UnitAttackSourceWard(UnitAttackSource.Weapon, UnitWardCategory.Once)),
                new BattleUnitWardModifier(new BattleUnitId(1), new UnitModifierDefinitionId("mindward"), new UnitAttackSourceWard(UnitAttackSource.Mind, UnitWardCategory.Once)),
                new BattleUnitWardModifier(new BattleUnitId(1), new UnitModifierDefinitionId("lifeward"), new UnitAttackSourceWard(UnitAttackSource.Life, UnitWardCategory.Once)),
                new BattleUnitWardModifier(new BattleUnitId(1), new UnitModifierDefinitionId("deathward"), new UnitAttackSourceWard(UnitAttackSource.Death, UnitWardCategory.Once)),
                new BattleUnitWardModifier(new BattleUnitId(1), new UnitModifierDefinitionId("waterward"), new UnitAttackSourceWard(UnitAttackSource.Water, UnitWardCategory.Once)),
                new BattleUnitWardModifier(new BattleUnitId(1), new UnitModifierDefinitionId("earthward"), new UnitAttackSourceWard(UnitAttackSource.Earth, UnitWardCategory.Once)),
                new BattleUnitWardModifier(new BattleUnitId(1), new UnitModifierDefinitionId("airward"), new UnitAttackSourceWard(UnitAttackSource.Air, UnitWardCategory.Once)),
                new BattleUnitWardModifier(new BattleUnitId(1), new UnitModifierDefinitionId("paralyzeward"), new UnitAttackClassWard(UnitAttackType.Paralyze, UnitWardCategory.Once))
            ]
        };
        BattleUnit enemy = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, enemyDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(protectorDefinition)
            .WithDefinition(allyDefinition)
            .WithDefinition(enemyDefinition)
            .WithDefinition(fireWardModifier)
            .WithAttacker(protector, GridPosition.FrontBottom)
            .WithAttacker(ally, GridPosition.BackBottom)
            .WithDefender(enemy, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(protector.Id, new BattleAttackUnitTarget(ally.Id)), [
            new BattleUnitAttackEvent(protector.Id, new BattleAttackUnitTarget(ally.Id), [new UnitAttackDefinitionId("protector")]),
            new BattleUnitAttackedEvent(protector.Id, ally.Id, new UnitAttackDefinitionId("protector")),
            new BattleUnitAttackHitEvent(protector.Id, ally.Id, new UnitAttackDefinitionId("protector"), 0),
            new BattleUnitWardGrantedEvent(ally.Id, new BattleUnitWardModifier(protector.Id, fireWardModifier.Id, FireWard)),
            new BattleUnitTurnEndingEvent(protector.Id),
            new BattleUnitTurnEndedEvent(protector.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(enemy.Id, BattleUnitTurnType.Normal))
        ]);

        BattleState expected = engine.State
                .WithUnit(protector)
                .WithUnit(ally with
                {
                    Wards =
                    [
                        new UnitAttackSourceWard(UnitAttackSource.Weapon, UnitWardCategory.Once),
                        new UnitAttackSourceWard(UnitAttackSource.Mind, UnitWardCategory.Once),
                        new UnitAttackSourceWard(UnitAttackSource.Life, UnitWardCategory.Once),
                        new UnitAttackSourceWard(UnitAttackSource.Death, UnitWardCategory.Once),
                        new UnitAttackSourceWard(UnitAttackSource.Water, UnitWardCategory.Once),
                        new UnitAttackSourceWard(UnitAttackSource.Earth, UnitWardCategory.Once),
                        new UnitAttackSourceWard(UnitAttackSource.Air, UnitWardCategory.Once),
                        new UnitAttackClassWard(UnitAttackType.Paralyze, UnitWardCategory.Once),
                        FireWard
                    ],
                    WardModifiers =
                    [
                        new BattleUnitWardModifier(new BattleUnitId(1), new UnitModifierDefinitionId("weaponward"), new UnitAttackSourceWard(UnitAttackSource.Weapon, UnitWardCategory.Once)),
                        new BattleUnitWardModifier(new BattleUnitId(1), new UnitModifierDefinitionId("mindward"), new UnitAttackSourceWard(UnitAttackSource.Mind, UnitWardCategory.Once)),
                        new BattleUnitWardModifier(new BattleUnitId(1), new UnitModifierDefinitionId("lifeward"), new UnitAttackSourceWard(UnitAttackSource.Life, UnitWardCategory.Once)),
                        new BattleUnitWardModifier(new BattleUnitId(1), new UnitModifierDefinitionId("deathward"), new UnitAttackSourceWard(UnitAttackSource.Death, UnitWardCategory.Once)),
                        new BattleUnitWardModifier(new BattleUnitId(1), new UnitModifierDefinitionId("waterward"), new UnitAttackSourceWard(UnitAttackSource.Water, UnitWardCategory.Once)),
                        new BattleUnitWardModifier(new BattleUnitId(1), new UnitModifierDefinitionId("earthward"), new UnitAttackSourceWard(UnitAttackSource.Earth, UnitWardCategory.Once)),
                        new BattleUnitWardModifier(new BattleUnitId(1), new UnitModifierDefinitionId("airward"), new UnitAttackSourceWard(UnitAttackSource.Air, UnitWardCategory.Once)),
                        new BattleUnitWardModifier(new BattleUnitId(1), new UnitModifierDefinitionId("paralyzeward"), new UnitAttackClassWard(UnitAttackType.Paralyze, UnitWardCategory.Once)),
                        new BattleUnitWardModifier(protector.Id, fireWardModifier.Id, FireWard)
                    ]
                })
                .WithUnit(enemy)
            with
            {
                CurrentUnitTurn = new BattleUnitTurn(enemy.Id, BattleUnitTurnType.Normal),
                TurnQueue = [new BattleUnitTurn(ally.Id, BattleUnitTurnType.Normal)]
            };

        Assert.That(engine.State, Is.EqualTo(expected));
    }

    [Test]
    public void SkipsGrantedModifierTest()
    {
        UnitModifierDefinition fireWardModifier = WardModifier("fireward", FireWard);
        UnitDefinition firstProtectorDefinition = ProtectorDefinition(UnitAttackReach.Any, [fireWardModifier.Id]);
        UnitDefinition secondProtectorDefinition = firstProtectorDefinition with
        {
            Id = new UnitDefinitionId("protector2"),
            Attack = firstProtectorDefinition.Attack with
            {
                Id = new UnitAttackDefinitionId("protector2"),
                Initiative = 30
            }
        };
        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ally", maxHitpoints: 100, initiative: 20, damage: 5);
        UnitDefinition enemyDefinition = TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 100, initiative: 40, damage: 5) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("enemy"), initiative: 40, damage: 5, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Source = UnitAttackSource.Fire
            }
        };

        BattleUnit firstProtector = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, firstProtectorDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit secondProtector = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, secondProtectorDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit ally = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, allyDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit enemy = TestHelpers.SimpleBattleUnit(new BattleUnitId(3), null, enemyDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(firstProtectorDefinition)
            .WithDefinition(secondProtectorDefinition)
            .WithDefinition(allyDefinition)
            .WithDefinition(enemyDefinition)
            .WithDefinition(fireWardModifier)
            .WithAttacker(firstProtector, GridPosition.FrontBottom)
            .WithAttacker(secondProtector, GridPosition.FrontCenter)
            .WithAttacker(ally, GridPosition.BackBottom)
            .WithDefender(enemy, GridPosition.FrontBottom)
            .Build();

        _ = engine.DispatchCommand(new BattleAttackCommand(firstProtector.Id, new BattleAttackUnitTarget(ally.Id)));
        _ = engine.DispatchCommand(new BattleAttackCommand(enemy.Id, new BattleAttackUnitTarget(ally.Id)));

        engine.CheckCommandError(
            new BattleAttackCommand(secondProtector.Id, new BattleAttackUnitTarget(ally.Id)),
            BattleCommandError.TargetIncorrect);

        BattleState expected = engine.State
                .WithUnit(firstProtector)
                .WithUnit(secondProtector)
                .WithUnit(ally with
                {
                    WardModifiers = [new BattleUnitWardModifier(firstProtector.Id, fireWardModifier.Id, FireWard)]
                })
                .WithUnit(enemy)
            with
            {
                CurrentUnitTurn = new BattleUnitTurn(secondProtector.Id, BattleUnitTurnType.Normal),
                TurnQueue = [new BattleUnitTurn(ally.Id, BattleUnitTurnType.Normal)]
            };

        Assert.That(engine.State, Is.EqualTo(expected));
    }

    [Test]
    public void PermanentModifiersTest()
    {
        UnitModifierDefinition fireWardModifier = WardModifier("fireward", FireWard);
        UnitModifierDefinition modifier1 = TestHelpers.SimpleModifier("modifier1");
        UnitModifierDefinition modifier2 = TestHelpers.SimpleModifier("modifier2");
        UnitModifierDefinition modifier3 = TestHelpers.SimpleModifier("modifier3");
        UnitModifierDefinition modifier4 = TestHelpers.SimpleModifier("modifier4");
        UnitModifierDefinition modifier5 = TestHelpers.SimpleModifier("modifier5");
        UnitModifierDefinition modifier6 = TestHelpers.SimpleModifier("modifier6");
        UnitModifierDefinition modifier7 = TestHelpers.SimpleModifier("modifier7");
        UnitModifierDefinition modifier8 = TestHelpers.SimpleModifier("modifier8");
        UnitDefinition protectorDefinition = ProtectorDefinition(UnitAttackReach.Any, [fireWardModifier.Id]);
        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ally", maxHitpoints: 100, initiative: 20, damage: 5);
        UnitDefinition enemyDefinition = TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 100, initiative: 40, damage: 5);

        BattleUnit protector = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, protectorDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit ally = new(
            new BattleUnitId(1),
            null,
            allyDefinition.Id,
            BattleSide.Attacker,
            100,
            0,
            0,
            1,
            [
                UnitModifier.Permanent(modifier1.Id),
                UnitModifier.Permanent(modifier2.Id),
                UnitModifier.Permanent(modifier3.Id),
                UnitModifier.Permanent(modifier4.Id),
                UnitModifier.Permanent(modifier5.Id),
                UnitModifier.Permanent(modifier6.Id),
                UnitModifier.Permanent(modifier7.Id),
                UnitModifier.Permanent(modifier8.Id)
            ],
            [],
            null,
            [],
            Name: null,
            IsTypeLocked: false);
        BattleUnit enemy = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, enemyDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(protectorDefinition)
            .WithDefinition(allyDefinition)
            .WithDefinition(enemyDefinition)
            .WithDefinition(fireWardModifier)
            .WithDefinition(modifier1)
            .WithDefinition(modifier2)
            .WithDefinition(modifier3)
            .WithDefinition(modifier4)
            .WithDefinition(modifier5)
            .WithDefinition(modifier6)
            .WithDefinition(modifier7)
            .WithDefinition(modifier8)
            .WithAttacker(protector, GridPosition.FrontBottom)
            .WithAttacker(ally, GridPosition.BackBottom)
            .WithDefender(enemy, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(protector.Id, new BattleAttackUnitTarget(ally.Id)), [
            new BattleUnitAttackEvent(protector.Id, new BattleAttackUnitTarget(ally.Id), [new UnitAttackDefinitionId("protector")]),
            new BattleUnitAttackedEvent(protector.Id, ally.Id, new UnitAttackDefinitionId("protector")),
            new BattleUnitAttackHitEvent(protector.Id, ally.Id, new UnitAttackDefinitionId("protector"), 0),
            new BattleUnitWardGrantedEvent(ally.Id, new BattleUnitWardModifier(protector.Id, fireWardModifier.Id, FireWard)),
            new BattleUnitTurnEndingEvent(protector.Id),
            new BattleUnitTurnEndedEvent(protector.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(enemy.Id, BattleUnitTurnType.Normal))
        ]);

        BattleState expected = engine.State
                .WithUnit(protector)
                .WithUnit(ally with
                {
                    Wards = [FireWard],
                    WardModifiers = [new BattleUnitWardModifier(protector.Id, fireWardModifier.Id, FireWard)]
                })
                .WithUnit(enemy)
            with
            {
                CurrentUnitTurn = new BattleUnitTurn(enemy.Id, BattleUnitTurnType.Normal),
                TurnQueue = [new BattleUnitTurn(ally.Id, BattleUnitTurnType.Normal)]
            };

        Assert.That(engine.State, Is.EqualTo(expected));
    }

    [Test]
    public void ExistingWardTest()
    {
        UnitModifierDefinition fireWardModifier = WardModifier("fireward", FireWard);
        UnitDefinition protectorDefinition = ProtectorDefinition(UnitAttackReach.Any, [fireWardModifier.Id]);
        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ally", maxHitpoints: 100, initiative: 20, damage: 5);
        UnitDefinition enemyDefinition = TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 100, initiative: 40, damage: 5);

        BattleUnit protector = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, protectorDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit ally = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, allyDefinition.Id, BattleSide.Attacker, hitpoints: 100) with
        {
            Modifiers = [UnitModifier.Permanent(fireWardModifier.Id)]
        };
        BattleUnit enemy = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, enemyDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(protectorDefinition)
            .WithDefinition(allyDefinition)
            .WithDefinition(enemyDefinition)
            .WithDefinition(fireWardModifier)
            .WithAttacker(protector, GridPosition.FrontBottom)
            .WithAttacker(ally, GridPosition.BackBottom)
            .WithDefender(enemy, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommandError(
            new BattleAttackCommand(protector.Id, new BattleAttackUnitTarget(ally.Id)),
            BattleCommandError.TargetIncorrect);
    }

    [Test]
    public void ConsumedExistingWardTest()
    {
        UnitModifierDefinition fireWardModifier = WardModifier("fireward", FireWard);
        UnitDefinition protectorDefinition = ProtectorDefinition(UnitAttackReach.Any, [fireWardModifier.Id]);
        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ally", maxHitpoints: 100, initiative: 20, damage: 5);
        UnitDefinition enemyDefinition = TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 100, initiative: 70, damage: 5) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("enemy"), initiative: 70, damage: 5, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Source = UnitAttackSource.Fire
            }
        };

        BattleUnit protector = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, protectorDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit ally = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, allyDefinition.Id, BattleSide.Attacker, hitpoints: 100) with
        {
            Modifiers = [UnitModifier.Permanent(fireWardModifier.Id)]
        };
        BattleUnit enemy = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, enemyDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(protectorDefinition)
            .WithDefinition(allyDefinition)
            .WithDefinition(enemyDefinition)
            .WithDefinition(fireWardModifier)
            .WithAttacker(protector, GridPosition.FrontBottom)
            .WithAttacker(ally, GridPosition.BackBottom)
            .WithDefender(enemy, GridPosition.FrontBottom)
            .Build();

        _ = engine.DispatchCommand(new BattleAttackCommand(enemy.Id, new BattleAttackUnitTarget(ally.Id)));

        engine.CheckCommand(new BattleAttackCommand(protector.Id, new BattleAttackUnitTarget(ally.Id)), [
            new BattleUnitAttackEvent(protector.Id, new BattleAttackUnitTarget(ally.Id), [new UnitAttackDefinitionId("protector")]),
            new BattleUnitAttackedEvent(protector.Id, ally.Id, new UnitAttackDefinitionId("protector")),
            new BattleUnitAttackHitEvent(protector.Id, ally.Id, new UnitAttackDefinitionId("protector"), 0),
            new BattleUnitWardGrantedEvent(ally.Id, new BattleUnitWardModifier(protector.Id, fireWardModifier.Id, FireWard)),
            new BattleUnitTurnEndingEvent(protector.Id),
            new BattleUnitTurnEndedEvent(protector.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(ally.Id, BattleUnitTurnType.Normal))
        ]);

        BattleState expected = engine.State
                .WithUnit(protector)
                .WithUnit(ally with
                {
                    Wards = [FireWard],
                    WardModifiers = [new BattleUnitWardModifier(protector.Id, fireWardModifier.Id, FireWard)]
                })
                .WithUnit(enemy)
            with
            {
                CurrentUnitTurn = new BattleUnitTurn(ally.Id, BattleUnitTurnType.Normal),
                TurnQueue = []
            };

        Assert.That(engine.State, Is.EqualTo(expected));
    }

    [Test]
    public void WardedWoundedAllyTest()
    {
        UnitModifierDefinition fireWardModifier = WardModifier("fireward", FireWard);
        UnitDefinition protectorDefinition = ProtectorDefinition(UnitAttackReach.Any, [fireWardModifier.Id]) with
        {
            SecondaryAttack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("protector-heal"), initiative: 60, damage: 0, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Type = UnitAttackType.Heal,
                Source = UnitAttackSource.Life,
                Heal = 30
            }
        };
        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ally", maxHitpoints: 100, initiative: 20, damage: 5) with
        {
            Wards = [FireWard]
        };
        UnitDefinition enemyDefinition = TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 100, initiative: 40, damage: 5);

        BattleUnit protector = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, protectorDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit ally = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, allyDefinition.Id, BattleSide.Attacker, hitpoints: 50);
        BattleUnit enemy = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, enemyDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(protectorDefinition)
            .WithDefinition(allyDefinition)
            .WithDefinition(enemyDefinition)
            .WithDefinition(fireWardModifier)
            .WithAttacker(protector, GridPosition.FrontBottom)
            .WithAttacker(ally, GridPosition.BackBottom)
            .WithDefender(enemy, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(protector.Id, new BattleAttackUnitTarget(ally.Id)), [
            new BattleUnitAttackEvent(protector.Id, new BattleAttackUnitTarget(ally.Id), [new UnitAttackDefinitionId("protector"), new UnitAttackDefinitionId("protector-heal")]),
            new BattleUnitAttackedEvent(protector.Id, ally.Id, new UnitAttackDefinitionId("protector-heal")),
            new BattleUnitAttackHitEvent(protector.Id, ally.Id, new UnitAttackDefinitionId("protector-heal"), 0),
            new BattleUnitHealedEvent(ally.Id, 80),
            new BattleUnitTurnEndingEvent(protector.Id),
            new BattleUnitTurnEndedEvent(protector.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(enemy.Id, BattleUnitTurnType.Normal))
        ]);

        BattleState expected = engine.State
                .WithUnit(ally with
                {
                    Hitpoints = 80,
                    Wards = [FireWard]
                })
            with
            {
                CurrentUnitTurn = new BattleUnitTurn(enemy.Id, BattleUnitTurnType.Normal),
                TurnQueue = [new BattleUnitTurn(ally.Id, BattleUnitTurnType.Normal)]
            };

        Assert.That(engine.State, Is.EqualTo(expected));
    }
}
