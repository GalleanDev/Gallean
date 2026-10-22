namespace Gallean.Engine.Tests;

public sealed class BattleEngineCombatTests
{
    [Test]
    public void ArmorCapTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("warrior", maxHitpoints: 50, initiative: 50, damage: 100);

        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("golem", maxHitpoints: 200, initiative: 40, damage: 10) with
        {
            Armor = 99
        };

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 50);

        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 200);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(attacker, new GridPosition(0, 0))
            .WithDefender(defender, new GridPosition(0, 0))
            .Build();

        engine.CheckCommand(
            new BattleAttackCommand(new BattleUnitId(0), new BattleAttackUnitTarget(new BattleUnitId(1))),
            [
                new BattleUnitAttackEvent(new BattleUnitId(0), new BattleAttackUnitTarget(new BattleUnitId(1)), [new UnitAttackDefinitionId("warrior")]),
                new BattleUnitAttackedEvent(new BattleUnitId(0), new BattleUnitId(1), new UnitAttackDefinitionId("warrior")),
                new BattleUnitAttackHitEvent(new BattleUnitId(0), new BattleUnitId(1), new UnitAttackDefinitionId("warrior"), 10),
                new BattleUnitTurnEndingEvent(new BattleUnitId(0)),
                new BattleUnitTurnEndedEvent(new BattleUnitId(0)),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(1), BattleUnitTurnType.Normal))
            ]
        );

        UnitGrid<BattleUnit> expectedAttackerGrid = new UnitGrid<BattleUnit>.Builder()
            .Place(attacker, new GridPosition(0, 0), false)
            .Build();

        UnitGrid<BattleUnit> expectedDefenderGrid = new UnitGrid<BattleUnit>.Builder()
            .Place(defender with { Hitpoints = 190 }, new GridPosition(0, 0), false)
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
    public void ArmorTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("warrior", maxHitpoints: 50, initiative: 50, damage: 100);

        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("knight", maxHitpoints: 200, initiative: 40, damage: 10) with
        {
            Armor = 50
        };

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 50);

        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 200);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(attacker, new GridPosition(0, 0))
            .WithDefender(defender, new GridPosition(0, 0))
            .Build();

        engine.CheckCommand(
            new BattleAttackCommand(new BattleUnitId(0), new BattleAttackUnitTarget(new BattleUnitId(1))),
            [
                new BattleUnitAttackEvent(new BattleUnitId(0), new BattleAttackUnitTarget(new BattleUnitId(1)), [new UnitAttackDefinitionId("warrior")]),
                new BattleUnitAttackedEvent(new BattleUnitId(0), new BattleUnitId(1), new UnitAttackDefinitionId("warrior")),
                new BattleUnitAttackHitEvent(new BattleUnitId(0), new BattleUnitId(1), new UnitAttackDefinitionId("warrior"), 50),
                new BattleUnitTurnEndingEvent(new BattleUnitId(0)),
                new BattleUnitTurnEndedEvent(new BattleUnitId(0)),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(1), BattleUnitTurnType.Normal))
            ]
        );

        UnitGrid<BattleUnit> expectedAttackerGrid = new UnitGrid<BattleUnit>.Builder()
            .Place(attacker, new GridPosition(0, 0), false)
            .Build();

        UnitGrid<BattleUnit> expectedDefenderGrid = new UnitGrid<BattleUnit>.Builder()
            .Place(defender with { Hitpoints = 150 }, new GridPosition(0, 0), false)
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
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("unit0", maxHitpoints: 20, initiative: 50, damage: 100) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("unit0"), initiative: 50, damage: 100, accuracy: 0, reach: UnitAttackReach.Any)
        };

        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("unit1", maxHitpoints: 20, initiative: 40, damage: 5);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 20);

        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 20);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(attacker, new GridPosition(0, 0))
            .WithDefender(defender, new GridPosition(0, 0))
            .Build();

        engine.CheckCommand(
            new BattleAttackCommand(new BattleUnitId(0), new BattleAttackUnitTarget(new BattleUnitId(1))),
            [
                new BattleUnitAttackEvent(new BattleUnitId(0), new BattleAttackUnitTarget(new BattleUnitId(1)), [new UnitAttackDefinitionId("unit0")]),
                new BattleUnitAttackedEvent(new BattleUnitId(0), new BattleUnitId(1), new UnitAttackDefinitionId("unit0")),
                new BattleUnitAttackMissedEvent(new BattleUnitId(0), new BattleUnitId(1), new UnitAttackDefinitionId("unit0")),
                new BattleUnitTurnEndingEvent(new BattleUnitId(0)),
                new BattleUnitTurnEndedEvent(new BattleUnitId(0)),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(1), BattleUnitTurnType.Normal))
            ]
        );

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
    public void MinimumOneDamageTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("unit0", maxHitpoints: 20, initiative: 50, damage: 1);

        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("unit1", maxHitpoints: 200, initiative: 40, damage: 5) with
        {
            Armor = 90
        };

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 20);

        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 200);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(attacker, new GridPosition(0, 0))
            .WithDefender(defender, new GridPosition(0, 0))
            .Build();

        engine.CheckCommand(
            new BattleAttackCommand(new BattleUnitId(0), new BattleAttackUnitTarget(new BattleUnitId(1))),
            [
                new BattleUnitAttackEvent(new BattleUnitId(0), new BattleAttackUnitTarget(new BattleUnitId(1)), [new UnitAttackDefinitionId("unit0")]),
                new BattleUnitAttackedEvent(new BattleUnitId(0), new BattleUnitId(1), new UnitAttackDefinitionId("unit0")),
                new BattleUnitAttackHitEvent(new BattleUnitId(0), new BattleUnitId(1), new UnitAttackDefinitionId("unit0"), 1),
                new BattleUnitTurnEndingEvent(new BattleUnitId(0)),
                new BattleUnitTurnEndedEvent(new BattleUnitId(0)),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(1), BattleUnitTurnType.Normal))
            ]
        );

        UnitGrid<BattleUnit> expectedAttackerGrid = new UnitGrid<BattleUnit>.Builder()
            .Place(attacker, new GridPosition(0, 0), false)
            .Build();

        UnitGrid<BattleUnit> expectedDefenderGrid = new UnitGrid<BattleUnit>.Builder()
            .Place(defender with { Hitpoints = 199 }, new GridPosition(0, 0), false)
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
    public void MultiRoundCombatTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("warrior", maxHitpoints: 100, initiative: 50, damage: 5);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("scout", maxHitpoints: 100, initiative: 40, damage: 5);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 100);

        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(attacker, new GridPosition(0, 0))
            .WithDefender(defender, new GridPosition(0, 0))
            .Build();

        engine.CheckCommand(
            new BattleAttackCommand(new BattleUnitId(0), new BattleAttackUnitTarget(new BattleUnitId(1))),
            [
                new BattleUnitAttackEvent(new BattleUnitId(0), new BattleAttackUnitTarget(new BattleUnitId(1)), [new UnitAttackDefinitionId("warrior")]),
                new BattleUnitAttackedEvent(new BattleUnitId(0), new BattleUnitId(1), new UnitAttackDefinitionId("warrior")),
                new BattleUnitAttackHitEvent(new BattleUnitId(0), new BattleUnitId(1), new UnitAttackDefinitionId("warrior"), 5),
                new BattleUnitTurnEndingEvent(new BattleUnitId(0)),
                new BattleUnitTurnEndedEvent(new BattleUnitId(0)),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(1), BattleUnitTurnType.Normal))
            ]
        );

        engine.CheckCommand(
            new BattleAttackCommand(new BattleUnitId(1), new BattleAttackUnitTarget(new BattleUnitId(0))),
            [
                new BattleUnitAttackEvent(new BattleUnitId(1), new BattleAttackUnitTarget(new BattleUnitId(0)), [new UnitAttackDefinitionId("scout")]),
                new BattleUnitAttackedEvent(new BattleUnitId(1), new BattleUnitId(0), new UnitAttackDefinitionId("scout")),
                new BattleUnitAttackHitEvent(new BattleUnitId(1), new BattleUnitId(0), new UnitAttackDefinitionId("scout"), 5),
                new BattleUnitTurnEndingEvent(new BattleUnitId(1)),
                new BattleUnitTurnEndedEvent(new BattleUnitId(1)),
                new BattleRoundEndedEvent(),
                new BattleRoundStartedEvent(BattleRound.Normal(2), [new BattleUnitTurn(new BattleUnitId(0), BattleUnitTurnType.Normal), new BattleUnitTurn(new BattleUnitId(1), BattleUnitTurnType.Normal)]),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(0), BattleUnitTurnType.Normal))
            ]
        );

        engine.CheckCommand(
            new BattleAttackCommand(new BattleUnitId(0), new BattleAttackUnitTarget(new BattleUnitId(1))),
            [
                new BattleUnitAttackEvent(new BattleUnitId(0), new BattleAttackUnitTarget(new BattleUnitId(1)), [new UnitAttackDefinitionId("warrior")]),
                new BattleUnitAttackedEvent(new BattleUnitId(0), new BattleUnitId(1), new UnitAttackDefinitionId("warrior")),
                new BattleUnitAttackHitEvent(new BattleUnitId(0), new BattleUnitId(1), new UnitAttackDefinitionId("warrior"), 5),
                new BattleUnitTurnEndingEvent(new BattleUnitId(0)),
                new BattleUnitTurnEndedEvent(new BattleUnitId(0)),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(1), BattleUnitTurnType.Normal))
            ]
        );

        UnitGrid<BattleUnit> expectedAttackerGrid = new UnitGrid<BattleUnit>.Builder()
            .Place(attacker with { Hitpoints = 95 }, new GridPosition(0, 0), false)
            .Build();

        UnitGrid<BattleUnit> expectedDefenderGrid = new UnitGrid<BattleUnit>.Builder()
            .Place(defender with { Hitpoints = 90 }, new GridPosition(0, 0), false)
            .Build();

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = expectedAttackerGrid,
                DefenderGrid = expectedDefenderGrid,
                CurrentUnitTurn = new BattleUnitTurn(new BattleUnitId(1), BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(2)
            }));
    }

    [Test]
    public void RandomDamageTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("warrior", maxHitpoints: 100, initiative: 50, damage: 10);

        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("scout", maxHitpoints: 100, initiative: 40, damage: 5) with
        {
            Armor = 50
        };

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 100);

        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(attacker, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .WithGlobals(new GlobalVariables { BattleInitiativeVariance = 0, BattleDamageVariance = 6 })
            .WithRandom(new FixedRandomProvider())
            .Build();

        engine.CheckCommand(
            new BattleAttackCommand(attacker.Id, new BattleAttackUnitTarget(defender.Id)),
            [
                new BattleUnitAttackEvent(attacker.Id, new BattleAttackUnitTarget(defender.Id), [new UnitAttackDefinitionId("warrior")]),
                new BattleUnitAttackedEvent(attacker.Id, defender.Id, new UnitAttackDefinitionId("warrior")),
                new BattleUnitAttackHitEvent(attacker.Id, defender.Id, new UnitAttackDefinitionId("warrior"), 7),
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
                    .Place(defender with { Hitpoints = 93 }, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(1)
            }));
    }

    [Test]
    public void AttackStateTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 20, initiative: 50, damage: 5);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 20, initiative: 40, damage: 5);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 20);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 20);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(attacker, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(attacker.Id, new BattleAttackUnitTarget(defender.Id)), [
            new BattleUnitAttackEvent(attacker.Id, new BattleAttackUnitTarget(defender.Id), [new UnitAttackDefinitionId("attacker")]),
            new BattleUnitAttackedEvent(attacker.Id, defender.Id, new UnitAttackDefinitionId("attacker")),
            new BattleUnitAttackHitEvent(attacker.Id, defender.Id, new UnitAttackDefinitionId("attacker"), 5),
            new BattleUnitTurnEndingEvent(attacker.Id),
            new BattleUnitTurnEndedEvent(attacker.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(new BattleState(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(attacker, GridPosition.FrontBottom, false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(defender with { Hitpoints = 15 }, GridPosition.FrontBottom, false)
                .Build(),
            CurrentUnitTurn: new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: null,
            Modifiers: BattleModifiers.None)));
    }

    [Test]
    public void NegativeArmorTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("warrior", maxHitpoints: 50, initiative: 50, damage: 100);

        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("knight", maxHitpoints: 200, initiative: 40, damage: 10) with
        {
            Armor = -10
        };

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 50);

        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 200);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(attacker, new GridPosition(0, 0))
            .WithDefender(defender, new GridPosition(0, 0))
            .Build();

        engine.CheckCommand(
            new BattleAttackCommand(new BattleUnitId(0), new BattleAttackUnitTarget(new BattleUnitId(1))),
            [
                new BattleUnitAttackEvent(new BattleUnitId(0), new BattleAttackUnitTarget(new BattleUnitId(1)), [new UnitAttackDefinitionId("warrior")]),
                new BattleUnitAttackedEvent(new BattleUnitId(0), new BattleUnitId(1), new UnitAttackDefinitionId("warrior")),
                new BattleUnitAttackHitEvent(new BattleUnitId(0), new BattleUnitId(1), new UnitAttackDefinitionId("warrior"), 100),
                new BattleUnitTurnEndingEvent(new BattleUnitId(0)),
                new BattleUnitTurnEndedEvent(new BattleUnitId(0)),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(1), BattleUnitTurnType.Normal))
            ]
        );

        UnitGrid<BattleUnit> expectedAttackerGrid = new UnitGrid<BattleUnit>.Builder()
            .Place(attacker, new GridPosition(0, 0), false)
            .Build();

        UnitGrid<BattleUnit> expectedDefenderGrid = new UnitGrid<BattleUnit>.Builder()
            .Place(defender with { Hitpoints = 100 }, new GridPosition(0, 0), false)
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
    public void LeveledUnitTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 100, initiative: 50, damage: 20);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 100, initiative: 40, damage: 10);

        BattleUnit attacker = new(
            Id: new BattleUnitId(1),
            WorldUnitId: null,
            DefinitionId: attackerDefinition.Id,
            Side: BattleSide.Attacker,
            Hitpoints: attackerDefinition.MaxHitpoints,
            Experience: 0,
            ExperienceGained: 0,
            Level: 3,
            Modifiers: [],
            StatusEffects: [],
            SummonerId: null,
            EquipmentAttackIds: [],
            Name: null,
            IsTypeLocked: false);

        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: defenderDefinition.MaxHitpoints);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(new UnitDynamicUpgradeDefinition(
                attackerDefinition.Id,
                UpgradeChangeLevel: 2,
                UnitDynamicUpgrade.Empty with { MaxHitpoints = 10, Damage = 5 },
                UnitDynamicUpgrade.Empty with { MaxHitpoints = 5, Damage = 2 }))
            .WithAttacker(attacker, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(
            new BattleAttackCommand(attacker.Id, new BattleAttackUnitTarget(defender.Id)),
            [
                new BattleUnitAttackEvent(attacker.Id, new BattleAttackUnitTarget(defender.Id), [new UnitAttackDefinitionId("attacker")]),
                new BattleUnitAttackedEvent(attacker.Id, defender.Id, new UnitAttackDefinitionId("attacker")),
                new BattleUnitAttackHitEvent(attacker.Id, defender.Id, new UnitAttackDefinitionId("attacker"), 27),
                new BattleUnitTurnEndingEvent(attacker.Id),
                new BattleUnitTurnEndedEvent(attacker.Id),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal))
            ]
        );

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(attacker, GridPosition.FrontBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(defender with { Hitpoints = 73 }, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(1)
            }));
    }
}
