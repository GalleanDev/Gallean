namespace Gallean.Engine.Tests;

public sealed class BattleEngineExperienceTests
{
    private static UnitDefinition Fighter(string id, int maxHitpoints, int initiative, int damage, int experienceNextLevel, int experienceKilled)
    {
        return TestHelpers.SimpleUnitDefinition(id, maxHitpoints, initiative, damage) with
        {
            ExperienceNextLevel = experienceNextLevel,
            ExperienceKilled = experienceKilled
        };
    }

    [Test]
    public void DistributeExperienceTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 10, initiative: 50, damage: 10) with
        {
            PurchaseCost = WorldResourceSet.Empty
        };

        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 40, damage: 1) with
        {
            PurchaseCost = WorldResourceSet.Empty,
            ExperienceKilled = 5
        };

        BattleUnit attacker1 = new(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, 10, 7, 0, 1, [], [], null, [], null, false);
        BattleUnit attacker2 = new(new BattleUnitId(1), null, attackerDefinition.Id, BattleSide.Attacker, 10, 11, 0, 1, [], [], null, [], null, false);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(attacker1, GridPosition.FrontBottom)
            .WithAttacker(attacker2, GridPosition.FrontTop)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(attacker1.Id, new BattleAttackUnitTarget(defender.Id)), [
            new BattleUnitAttackEvent(attacker1.Id, new BattleAttackUnitTarget(defender.Id), [new UnitAttackDefinitionId("attacker")]),
            new BattleUnitAttackedEvent(attacker1.Id, defender.Id, new UnitAttackDefinitionId("attacker")),
            new BattleUnitAttackHitEvent(attacker1.Id, defender.Id, new UnitAttackDefinitionId("attacker"), 10),
            new BattleUnitDiedEvent(defender.Id),
            new BattleUnitExperienceGainedEvent(attacker1.Id, 2),
            new BattleUnitExperienceGainedEvent(attacker2.Id, 2),
            new BattleUnitTurnEndingEvent(attacker1.Id),
            new BattleRoundEndedEvent(),
            new BattleEndedEvent(BattleSide.Attacker),
            new BattleUnitExperienceGainedEvent(attacker1.Id, 2),
            new BattleUnitExperienceGainedEvent(attacker2.Id, 2)
        ]);

        UnitGrid<BattleUnit> expectedAttackerGrid = new UnitGrid<BattleUnit>.Builder()
            .Place(attacker1 with { ExperienceGained = 2 }, GridPosition.FrontBottom, false)
            .Place(attacker2 with { ExperienceGained = 2 }, GridPosition.FrontTop, false)
            .Build();
        UnitGrid<BattleUnit> expectedDefenderGrid = new UnitGrid<BattleUnit>.Builder()
            .Place(defender with { Hitpoints = 0 }, GridPosition.FrontBottom, false)
            .Build();

        Assert.That(engine.State, Is.EqualTo(new BattleState(
            expectedAttackerGrid,
            expectedDefenderGrid,
            new BattleUnitTurn(attacker1.Id, BattleUnitTurnType.Normal),
            [new BattleUnitTurn(attacker2.Id, BattleUnitTurnType.Normal), new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal)],
            [],
            [],
            BattleRound.Normal(1),
            new BattleOutcome(BattleSide.Attacker),
            BattleModifiers.None)));
    }

    [Test]
    public void DeadUnitsTest()
    {
        UnitDefinition survivorDefinition = TestHelpers.SimpleUnitDefinition("survivor", maxHitpoints: 10, initiative: 50, damage: 10) with
        {
            PurchaseCost = WorldResourceSet.Empty
        };

        UnitDefinition doomedDefinition = TestHelpers.SimpleUnitDefinition("doomed", maxHitpoints: 10, initiative: 40, damage: 1) with
        {
            PurchaseCost = WorldResourceSet.Empty
        };

        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 60, damage: 10) with
        {
            PurchaseCost = WorldResourceSet.Empty,
            ExperienceKilled = 6
        };

        BattleUnit survivor = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, survivorDefinition.Id, BattleSide.Attacker, hitpoints: 10);
        BattleUnit doomed = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, doomedDefinition.Id, BattleSide.Attacker, hitpoints: 10);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(survivorDefinition)
            .WithDefinition(doomedDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(survivor, GridPosition.FrontBottom)
            .WithAttacker(doomed, GridPosition.FrontTop)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(defender.Id, new BattleAttackUnitTarget(doomed.Id)), [
            new BattleUnitAttackEvent(defender.Id, new BattleAttackUnitTarget(doomed.Id), [new UnitAttackDefinitionId("defender")]),
            new BattleUnitAttackedEvent(defender.Id, doomed.Id, new UnitAttackDefinitionId("defender")),
            new BattleUnitAttackHitEvent(defender.Id, doomed.Id, new UnitAttackDefinitionId("defender"), 10),
            new BattleUnitDiedEvent(doomed.Id),
            new BattleUnitTurnEndingEvent(defender.Id),
            new BattleUnitTurnEndedEvent(defender.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(survivor.Id, BattleUnitTurnType.Normal))
        ]);

        engine.CheckCommand(new BattleAttackCommand(survivor.Id, new BattleAttackUnitTarget(defender.Id)), [
            new BattleUnitAttackEvent(survivor.Id, new BattleAttackUnitTarget(defender.Id), [new UnitAttackDefinitionId("survivor")]),
            new BattleUnitAttackedEvent(survivor.Id, defender.Id, new UnitAttackDefinitionId("survivor")),
            new BattleUnitAttackHitEvent(survivor.Id, defender.Id, new UnitAttackDefinitionId("survivor"), 10),
            new BattleUnitDiedEvent(defender.Id),
            new BattleUnitExperienceGainedEvent(survivor.Id, 6),
            new BattleUnitTurnEndingEvent(survivor.Id),
            new BattleRoundEndedEvent(),
            new BattleEndedEvent(BattleSide.Attacker),
            new BattleUnitExperienceGainedEvent(survivor.Id, 6)
        ]);

        UnitGrid<BattleUnit> expectedAttackerGrid = new UnitGrid<BattleUnit>.Builder()
            .Place(survivor with { ExperienceGained = 6 }, GridPosition.FrontBottom, false)
            .Place(doomed with { Hitpoints = 0 }, GridPosition.FrontTop, false)
            .Build();
        UnitGrid<BattleUnit> expectedDefenderGrid = new UnitGrid<BattleUnit>.Builder()
            .Place(defender with { Hitpoints = 0 }, GridPosition.FrontBottom, false)
            .Build();

        Assert.That(engine.State, Is.EqualTo(new BattleState(
            expectedAttackerGrid,
            expectedDefenderGrid,
            new BattleUnitTurn(survivor.Id, BattleUnitTurnType.Normal),
            [new BattleUnitTurn(doomed.Id, BattleUnitTurnType.Normal)],
            [],
            [],
            BattleRound.Normal(1),
            new BattleOutcome(BattleSide.Attacker),
            BattleModifiers.None)));
    }

    [Test]
    public void MultipleKillsTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 10, initiative: 50, damage: 10) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("attacker"), 50, 10, accuracy: 100, reach: UnitAttackReach.All),
            PurchaseCost = WorldResourceSet.Empty
        };

        UnitDefinition firstDefenderDefinition = TestHelpers.SimpleUnitDefinition("first", maxHitpoints: 10, initiative: 40, damage: 1) with
        {
            PurchaseCost = WorldResourceSet.Empty,
            ExperienceKilled = 2
        };

        UnitDefinition secondDefenderDefinition = TestHelpers.SimpleUnitDefinition("second", maxHitpoints: 10, initiative: 30, damage: 1) with
        {
            PurchaseCost = WorldResourceSet.Empty,
            ExperienceKilled = 3
        };

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 10);
        BattleUnit firstDefender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, firstDefenderDefinition.Id, BattleSide.Defender, hitpoints: 10);
        BattleUnit secondDefender = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, secondDefenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(firstDefenderDefinition)
            .WithDefinition(secondDefenderDefinition)
            .WithAttacker(attacker, GridPosition.FrontBottom)
            .WithDefender(firstDefender, GridPosition.FrontBottom)
            .WithDefender(secondDefender, GridPosition.FrontCenter)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(attacker.Id, new BattleAttackUnitTarget(firstDefender.Id)), [
            new BattleUnitAttackEvent(attacker.Id, new BattleAttackUnitTarget(firstDefender.Id), [new UnitAttackDefinitionId("attacker")]),
            new BattleUnitAttackedEvent(attacker.Id, firstDefender.Id, new UnitAttackDefinitionId("attacker")),
            new BattleUnitAttackHitEvent(attacker.Id, firstDefender.Id, new UnitAttackDefinitionId("attacker"), 10),
            new BattleUnitDiedEvent(firstDefender.Id),
            new BattleUnitExperienceGainedEvent(attacker.Id, 2),
            new BattleUnitAttackedEvent(attacker.Id, secondDefender.Id, new UnitAttackDefinitionId("attacker")),
            new BattleUnitAttackHitEvent(attacker.Id, secondDefender.Id, new UnitAttackDefinitionId("attacker"), 10),
            new BattleUnitDiedEvent(secondDefender.Id),
            new BattleUnitExperienceGainedEvent(attacker.Id, 5),
            new BattleUnitTurnEndingEvent(attacker.Id),
            new BattleRoundEndedEvent(),
            new BattleEndedEvent(BattleSide.Attacker),
            new BattleUnitExperienceGainedEvent(attacker.Id, 5)

        ]);

        UnitGrid<BattleUnit> expectedAttackerGrid = new UnitGrid<BattleUnit>.Builder()
            .Place(attacker with { ExperienceGained = 5 }, GridPosition.FrontBottom, false)
            .Build();
        UnitGrid<BattleUnit> expectedDefenderGrid = new UnitGrid<BattleUnit>.Builder()
            .Place(firstDefender with { Hitpoints = 0 }, GridPosition.FrontBottom, false)
            .Place(secondDefender with { Hitpoints = 0 }, GridPosition.FrontCenter, false)
            .Build();

        Assert.That(engine.State, Is.EqualTo(new BattleState(
            expectedAttackerGrid,
            expectedDefenderGrid,
            new BattleUnitTurn(attacker.Id, BattleUnitTurnType.Normal),
            [new BattleUnitTurn(firstDefender.Id, BattleUnitTurnType.Normal), new BattleUnitTurn(secondDefender.Id, BattleUnitTurnType.Normal)],
            [],
            [],
            BattleRound.Normal(1),
            new BattleOutcome(BattleSide.Attacker),
            BattleModifiers.None)));
    }

    [Test]
    public void SummonedUnitsTest()
    {
        UnitDefinition summonDefinition = TestHelpers.SimpleUnitDefinition("summon", maxHitpoints: 10, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Summon,
            PurchaseCost = WorldResourceSet.Empty
        };

        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 10, initiative: 45, damage: 10) with
        {
            PurchaseCost = WorldResourceSet.Empty
        };

        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 40, damage: 1) with
        {
            PurchaseCost = WorldResourceSet.Empty,
            ExperienceKilled = 6
        };

        BattleUnit summon = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, summonDefinition.Id, BattleSide.Attacker, hitpoints: 10);
        BattleUnit soldier = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, soldierDefinition.Id, BattleSide.Attacker, hitpoints: 10);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(summonDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(summon, GridPosition.FrontBottom)
            .WithAttacker(soldier, GridPosition.FrontTop)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(summon.Id, new BattleAttackUnitTarget(defender.Id)), [
            new BattleUnitAttackEvent(summon.Id, new BattleAttackUnitTarget(defender.Id), [new UnitAttackDefinitionId("summon")]),
            new BattleUnitAttackedEvent(summon.Id, defender.Id, new UnitAttackDefinitionId("summon")),
            new BattleUnitAttackHitEvent(summon.Id, defender.Id, new UnitAttackDefinitionId("summon"), 10),
            new BattleUnitDiedEvent(defender.Id),
            new BattleUnitExperienceGainedEvent(soldier.Id, 3),
            new BattleUnitTurnEndingEvent(summon.Id),
            new BattleRoundEndedEvent(),
            new BattleEndedEvent(BattleSide.Attacker),
            new BattleUnitExperienceGainedEvent(soldier.Id, 3)
        ]);

        UnitGrid<BattleUnit> expectedAttackerGrid = new UnitGrid<BattleUnit>.Builder()
            .Place(summon, GridPosition.FrontBottom, false)
            .Place(soldier with { ExperienceGained = 3 }, GridPosition.FrontTop, false)
            .Build();
        UnitGrid<BattleUnit> expectedDefenderGrid = new UnitGrid<BattleUnit>.Builder()
            .Place(defender with { Hitpoints = 0 }, GridPosition.FrontBottom, false)
            .Build();

        Assert.That(engine.State, Is.EqualTo(new BattleState(
            expectedAttackerGrid,
            expectedDefenderGrid,
            new BattleUnitTurn(summon.Id, BattleUnitTurnType.Normal),
            [new BattleUnitTurn(soldier.Id, BattleUnitTurnType.Normal), new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal)],
            [],
            [],
            BattleRound.Normal(1),
            new BattleOutcome(BattleSide.Attacker),
            BattleModifiers.None)));
    }

    [Test]
    public void ExperienceBonusesCompoundTest()
    {
        UnitDefinition leaderDefinition = Fighter("leader", maxHitpoints: 100, initiative: 50, damage: 10, experienceNextLevel: 1000, experienceKilled: 0) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Leadership = 6,
            LeaderAbilities = LeaderAbility.WeaponMaster
        };
        UnitDefinition defenderDefinition = Fighter("defender", maxHitpoints: 10, initiative: 40, damage: 1, experienceNextLevel: 10, experienceKilled: 10);

        BattleUnit leader = new(new BattleUnitId(0), null, leaderDefinition.Id, BattleSide.Attacker, 100, 3, 0, 1, [], [], null, [], null, false);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(leaderDefinition)
            .WithDefinition(defenderDefinition)
            .WithModifiers(new BattleModifiers(new BattleSideModifiers(ExperiencePercent: 20, AccuracyBonus: 0, RandomDamageBonus: 0, CanRetreat: true), BattleSideModifiers.None))
            .WithAttacker(leader, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(leader.Id, new BattleAttackUnitTarget(defender.Id)), [
            new BattleUnitAttackEvent(leader.Id, new BattleAttackUnitTarget(defender.Id), [new UnitAttackDefinitionId("leader")]),
            new BattleUnitAttackedEvent(leader.Id, defender.Id, new UnitAttackDefinitionId("leader")),
            new BattleUnitAttackHitEvent(leader.Id, defender.Id, new UnitAttackDefinitionId("leader"), 10),
            new BattleUnitDiedEvent(defender.Id),
            new BattleUnitExperienceGainedEvent(leader.Id, 10),
            new BattleUnitTurnEndingEvent(leader.Id),
            new BattleRoundEndedEvent(),
            new BattleEndedEvent(BattleSide.Attacker),
            new BattleUnitExperienceGainedEvent(leader.Id, 14)
        ]);

        Assert.That(engine.State.GetUnit(leader.Id), Is.EqualTo(leader with { ExperienceGained = 14 }));
    }

    [Test]
    public void ParticipationFloorTest()
    {
        UnitDefinition attackerDefinition = Fighter("attacker", maxHitpoints: 10, initiative: 50, damage: 10, experienceNextLevel: 1000, experienceKilled: 0);
        UnitDefinition allyDefinition = Fighter("ally", maxHitpoints: 10, initiative: 30, damage: 1, experienceNextLevel: 1000, experienceKilled: 0);
        UnitDefinition defenderDefinition = Fighter("defender", maxHitpoints: 10, initiative: 40, damage: 1, experienceNextLevel: 10, experienceKilled: 0);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 10);
        BattleUnit ally = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, allyDefinition.Id, BattleSide.Attacker, hitpoints: 10);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(allyDefinition)
            .WithDefinition(defenderDefinition)
            .WithUpgradeResolver(new TestBattleUnitUpgradeResolver())
            .WithAttacker(attacker, GridPosition.FrontBottom)
            .WithAttacker(ally, GridPosition.FrontTop)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(attacker.Id, new BattleAttackUnitTarget(defender.Id)), [
            new BattleUnitAttackEvent(attacker.Id, new BattleAttackUnitTarget(defender.Id), [new UnitAttackDefinitionId("attacker")]),
            new BattleUnitAttackedEvent(attacker.Id, defender.Id, new UnitAttackDefinitionId("attacker")),
            new BattleUnitAttackHitEvent(attacker.Id, defender.Id, new UnitAttackDefinitionId("attacker"), 10),
            new BattleUnitDiedEvent(defender.Id),
            new BattleUnitTurnEndingEvent(attacker.Id),
            new BattleRoundEndedEvent(),
            new BattleEndedEvent(BattleSide.Attacker),
            new BattleUnitExperienceGainedEvent(attacker.Id, 1),
            new BattleUnitExperienceGainedEvent(ally.Id, 1)
        ]);

        Assert.That(engine.State.GetUnit(attacker.Id), Is.EqualTo(attacker with { ExperienceGained = 1 }));
        Assert.That(engine.State.GetUnit(ally.Id), Is.EqualTo(ally with { ExperienceGained = 1 }));
    }
}
