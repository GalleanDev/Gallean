namespace Gallean.Engine.Tests;

public sealed class BattleEngineWardTests
{
    [Test]
    public void SourceWardTest()
    {
        UnitWard ward = new UnitAttackSourceWard(UnitAttackSource.Fire, UnitWardCategory.Once);
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 10, initiative: 50, damage: 5) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("fire"), initiative: 50, damage: 5, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Source = UnitAttackSource.Fire
            }
        };
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 40, damage: 5) with
        {
            Wards = [ward]
        };

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 10);

        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(attacker, new GridPosition(0, 0))
            .WithDefender(defender, new GridPosition(0, 0))
            .Build();

        engine.CheckCommand(
            new BattleAttackCommand(new BattleUnitId(0), new BattleAttackUnitTarget(new BattleUnitId(1))),
            [
                new BattleUnitAttackEvent(new BattleUnitId(0), new BattleAttackUnitTarget(new BattleUnitId(1)), [new UnitAttackDefinitionId("fire")]),
                new BattleUnitAttackedEvent(new BattleUnitId(0), new BattleUnitId(1), new UnitAttackDefinitionId("fire")),
                new BattleUnitAttackBlockedEvent(new BattleUnitId(0), new BattleUnitId(1), new UnitAttackDefinitionId("fire"), ward),
                new BattleUnitWardConsumedEvent(new BattleUnitId(1), ward),
                new BattleUnitTurnEndingEvent(new BattleUnitId(0)),
                new BattleUnitTurnEndedEvent(new BattleUnitId(0)),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(1), BattleUnitTurnType.Normal))
            ]);

        UnitGrid<BattleUnit> expectedAttackerGrid = new UnitGrid<BattleUnit>.Builder()
            .Place(attacker, new GridPosition(0, 0), false)
            .Build();

        UnitGrid<BattleUnit> expectedDefenderGrid = new UnitGrid<BattleUnit>.Builder()
            .Place(defender, new GridPosition(0, 0), false)
            .Build();

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = expectedAttackerGrid,
                DefenderGrid = expectedDefenderGrid,
                CurrentUnitTurn = new BattleUnitTurn(new BattleUnitId(1), BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(1)
            }));
    }

    [Test]
    public void ClassImmunityTest()
    {
        UnitWard ward = new UnitAttackClassWard(UnitAttackType.Poison, UnitWardCategory.Always);
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 10, initiative: 50, damage: 5) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("poison"), initiative: 50, damage: 5, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Type = UnitAttackType.Poison,
                Source = UnitAttackSource.Death,
                IsInfinite = false
            }
        };
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 40, damage: 5) with
        {
            Wards = [ward]
        };

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 10);

        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(attacker, new GridPosition(0, 0))
            .WithDefender(defender, new GridPosition(0, 0))
            .Build();

        engine.CheckCommand(
            new BattleAttackCommand(new BattleUnitId(0), new BattleAttackUnitTarget(new BattleUnitId(1))),
            [
                new BattleUnitAttackEvent(new BattleUnitId(0), new BattleAttackUnitTarget(new BattleUnitId(1)), [new UnitAttackDefinitionId("poison")]),
                new BattleUnitAttackedEvent(new BattleUnitId(0), new BattleUnitId(1), new UnitAttackDefinitionId("poison")),
                new BattleUnitAttackBlockedEvent(new BattleUnitId(0), new BattleUnitId(1), new UnitAttackDefinitionId("poison"), ward),
                new BattleUnitTurnEndingEvent(new BattleUnitId(0)),
                new BattleUnitTurnEndedEvent(new BattleUnitId(0)),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(1), BattleUnitTurnType.Normal))
            ]);

        UnitGrid<BattleUnit> expectedAttackerGrid = new UnitGrid<BattleUnit>.Builder()
            .Place(attacker, new GridPosition(0, 0), false)
            .Build();

        UnitGrid<BattleUnit> expectedDefenderGrid = new UnitGrid<BattleUnit>.Builder()
            .Place(defender with { Wards = [ward] }, new GridPosition(0, 0), false)
            .Build();

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = expectedAttackerGrid,
                DefenderGrid = expectedDefenderGrid,
                CurrentUnitTurn = new BattleUnitTurn(new BattleUnitId(1), BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(1)
            }));
    }

    [Test]
    public void MissTest()
    {
        UnitWard ward = new UnitAttackSourceWard(UnitAttackSource.Fire, UnitWardCategory.Once);
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 10, initiative: 50, damage: 5) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("fire"), initiative: 50, damage: 5, accuracy: 0, reach: UnitAttackReach.Any) with
            {
                Source = UnitAttackSource.Fire
            }
        };
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 40, damage: 5) with
        {
            Wards = [ward]
        };

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 10);

        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(attacker, new GridPosition(0, 0))
            .WithDefender(defender, new GridPosition(0, 0))
            .Build();

        engine.CheckCommand(
            new BattleAttackCommand(new BattleUnitId(0), new BattleAttackUnitTarget(new BattleUnitId(1))),
            [
                new BattleUnitAttackEvent(new BattleUnitId(0), new BattleAttackUnitTarget(new BattleUnitId(1)), [new UnitAttackDefinitionId("fire")]),
                new BattleUnitAttackedEvent(new BattleUnitId(0), new BattleUnitId(1), new UnitAttackDefinitionId("fire")),
                new BattleUnitAttackMissedEvent(new BattleUnitId(0), new BattleUnitId(1), new UnitAttackDefinitionId("fire")),
                new BattleUnitTurnEndingEvent(new BattleUnitId(0)),
                new BattleUnitTurnEndedEvent(new BattleUnitId(0)),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(1), BattleUnitTurnType.Normal))
            ]);

        UnitGrid<BattleUnit> expectedAttackerGrid = new UnitGrid<BattleUnit>.Builder()
            .Place(attacker, new GridPosition(0, 0), false)
            .Build();

        UnitGrid<BattleUnit> expectedDefenderGrid = new UnitGrid<BattleUnit>.Builder()
            .Place(defender with { Wards = [ward] }, new GridPosition(0, 0), false)
            .Build();

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = expectedAttackerGrid,
                DefenderGrid = expectedDefenderGrid,
                CurrentUnitTurn = new BattleUnitTurn(new BattleUnitId(1), BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(1)
            }));
    }

    [Test]
    public void ModifierWardTest()
    {
        UnitModifierDefinition modifier = TestHelpers.SimpleModifier("modifier") with
        {
            Wards = [new UnitAttackSourceWard(UnitAttackSource.Fire, UnitWardCategory.Once)]
        };
        UnitWard ward = modifier.Wards[0];
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 10, initiative: 50, damage: 5) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("fire"), initiative: 50, damage: 5, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Source = UnitAttackSource.Fire
            }
        };
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 40, damage: 5);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 10);

        BattleUnit defender = new(
            Id: new BattleUnitId(1),
            WorldUnitId: null,
            DefinitionId: defenderDefinition.Id,
            Side: BattleSide.Defender,
            Hitpoints: 10,
            Experience: 0,
            ExperienceGained: 0,
            Level: 1,
            Modifiers: [UnitModifier.Permanent(modifier.Id)],
            StatusEffects: [],
            SummonerId: null,
            EquipmentAttackIds: [],
            Name: null,
            IsTypeLocked: false);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(modifier)
            .WithAttacker(attacker, new GridPosition(0, 0))
            .WithDefender(defender, new GridPosition(0, 0))
            .Build();

        engine.CheckCommand(
            new BattleAttackCommand(new BattleUnitId(0), new BattleAttackUnitTarget(new BattleUnitId(1))),
            [
                new BattleUnitAttackEvent(new BattleUnitId(0), new BattleAttackUnitTarget(new BattleUnitId(1)), [new UnitAttackDefinitionId("fire")]),
                new BattleUnitAttackedEvent(new BattleUnitId(0), new BattleUnitId(1), new UnitAttackDefinitionId("fire")),
                new BattleUnitAttackBlockedEvent(new BattleUnitId(0), new BattleUnitId(1), new UnitAttackDefinitionId("fire"), ward),
                new BattleUnitWardConsumedEvent(new BattleUnitId(1), ward),
                new BattleUnitTurnEndingEvent(new BattleUnitId(0)),
                new BattleUnitTurnEndedEvent(new BattleUnitId(0)),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(1), BattleUnitTurnType.Normal))
            ]);

        UnitGrid<BattleUnit> expectedDefenderGrid = new UnitGrid<BattleUnit>.Builder()
            .Place(defender, new GridPosition(0, 0), false)
            .Build();

        UnitGrid<BattleUnit> expectedAttackerGrid = new UnitGrid<BattleUnit>.Builder()
            .Place(attacker, new GridPosition(0, 0), false)
            .Build();

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = expectedAttackerGrid,
                DefenderGrid = expectedDefenderGrid,
                CurrentUnitTurn = new BattleUnitTurn(new BattleUnitId(1), BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(1)
            }));
    }
}
