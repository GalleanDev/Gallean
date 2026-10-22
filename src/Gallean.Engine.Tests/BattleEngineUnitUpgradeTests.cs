namespace Gallean.Engine.Tests;

public sealed class BattleEngineUnitUpgradeTests
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
    public void SimpleTest()
    {
        UnitDefinition squireDefinition = Fighter("squire", maxHitpoints: 10, initiative: 50, damage: 10, experienceNextLevel: 10, experienceKilled: 0);
        UnitDefinition knightDefinition = Fighter("knight", maxHitpoints: 20, initiative: 60, damage: 15, experienceNextLevel: 10, experienceKilled: 0);
        UnitDefinition defenderDefinition = Fighter("defender", maxHitpoints: 10, initiative: 40, damage: 1, experienceNextLevel: 10, experienceKilled: 5);

        BattleUnit squire = new(new BattleUnitId(0), null, squireDefinition.Id, BattleSide.Attacker, 10, 7, 0, 1, [], [], null, [], null, false);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(squireDefinition)
            .WithDefinition(knightDefinition)
            .WithDefinition(defenderDefinition)
            .WithUpgradeResolver(new TestBattleUnitUpgradeResolver().WithUpgrade(squireDefinition.Id, new BattleUnitUpgrade(knightDefinition.Id, 1, 20)))
            .WithAttacker(squire, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(squire.Id, new BattleAttackUnitTarget(defender.Id)), [
            new BattleUnitAttackEvent(squire.Id, new BattleAttackUnitTarget(defender.Id), [new UnitAttackDefinitionId("squire")]),
            new BattleUnitAttackedEvent(squire.Id, defender.Id, new UnitAttackDefinitionId("squire")),
            new BattleUnitAttackHitEvent(squire.Id, defender.Id, new UnitAttackDefinitionId("squire"), 10),
            new BattleUnitDiedEvent(defender.Id),
            new BattleUnitExperienceGainedEvent(squire.Id, 5),
            new BattleUnitTurnEndingEvent(squire.Id),
            new BattleRoundEndedEvent(),
            new BattleEndedEvent(BattleSide.Attacker),
            new BattleUnitUpgradedEvent(squire.Id, squireDefinition.Id, knightDefinition.Id, 1, 20)
        ]);

        Assert.That(engine.State.GetUnit(squire.Id), Is.EqualTo(
            squire with { DefinitionId = knightDefinition.Id, Hitpoints = 20, Experience = 0, ExperienceGained = 0, Level = 1 }));
    }

    [Test]
    public void UpgradeToSameUnitTest()
    {
        UnitDefinition squireDefinition = Fighter("squire", maxHitpoints: 10, initiative: 50, damage: 10, experienceNextLevel: 10, experienceKilled: 0);
        UnitDefinition defenderDefinition = Fighter("defender", maxHitpoints: 10, initiative: 40, damage: 1, experienceNextLevel: 10, experienceKilled: 5);

        BattleUnit squire = new(new BattleUnitId(0), null, squireDefinition.Id, BattleSide.Attacker, 10, 7, 0, 1, [], [], null, [], null, false);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(squireDefinition)
            .WithDefinition(defenderDefinition)
            .WithUpgradeResolver(new TestBattleUnitUpgradeResolver().WithUpgrade(squireDefinition.Id, new BattleUnitUpgrade(squireDefinition.Id, 2, 20)))
            .WithAttacker(squire, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(squire.Id, new BattleAttackUnitTarget(defender.Id)), [
            new BattleUnitAttackEvent(squire.Id, new BattleAttackUnitTarget(defender.Id), [new UnitAttackDefinitionId("squire")]),
            new BattleUnitAttackedEvent(squire.Id, defender.Id, new UnitAttackDefinitionId("squire")),
            new BattleUnitAttackHitEvent(squire.Id, defender.Id, new UnitAttackDefinitionId("squire"), 10),
            new BattleUnitDiedEvent(defender.Id),
            new BattleUnitExperienceGainedEvent(squire.Id, 5),
            new BattleUnitTurnEndingEvent(squire.Id),
            new BattleRoundEndedEvent(),
            new BattleEndedEvent(BattleSide.Attacker),
            new BattleUnitUpgradedEvent(squire.Id, squireDefinition.Id, squireDefinition.Id, 2, 20)
        ]);

        Assert.That(engine.State.GetUnit(squire.Id), Is.EqualTo(
            squire with { Hitpoints = 20, Experience = 0, ExperienceGained = 0, Level = 2 }));
    }

    [Test]
    public void DefenderUpgradeTest()
    {
        UnitDefinition guardDefinition = Fighter("guard", maxHitpoints: 10, initiative: 50, damage: 10, experienceNextLevel: 10, experienceKilled: 0);
        UnitDefinition veteranDefinition = Fighter("veteran", maxHitpoints: 20, initiative: 60, damage: 15, experienceNextLevel: 10, experienceKilled: 0) with
        {
            Level = 2
        };
        UnitDefinition victimDefinition = Fighter("victim", maxHitpoints: 10, initiative: 40, damage: 1, experienceNextLevel: 10, experienceKilled: 5);

        BattleUnit victim = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, victimDefinition.Id, BattleSide.Attacker, hitpoints: 10);
        BattleUnit guard = new(new BattleUnitId(1), null, guardDefinition.Id, BattleSide.Defender, 10, 7, 0, 1, [], [], null, [], null, false);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(guardDefinition)
            .WithDefinition(veteranDefinition)
            .WithDefinition(victimDefinition)
            .WithUpgradeResolver(new TestBattleUnitUpgradeResolver().WithUpgrade(guardDefinition.Id, new BattleUnitUpgrade(veteranDefinition.Id, 2, 20)))
            .WithAttacker(victim, GridPosition.FrontBottom)
            .WithDefender(guard, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(guard.Id, new BattleAttackUnitTarget(victim.Id)), [
            new BattleUnitAttackEvent(guard.Id, new BattleAttackUnitTarget(victim.Id), [new UnitAttackDefinitionId("guard")]),
            new BattleUnitAttackedEvent(guard.Id, victim.Id, new UnitAttackDefinitionId("guard")),
            new BattleUnitAttackHitEvent(guard.Id, victim.Id, new UnitAttackDefinitionId("guard"), 10),
            new BattleUnitDiedEvent(victim.Id),
            new BattleUnitExperienceGainedEvent(guard.Id, 5),
            new BattleUnitTurnEndingEvent(guard.Id),
            new BattleRoundEndedEvent(),
            new BattleEndedEvent(BattleSide.Defender),
            new BattleUnitUpgradedEvent(guard.Id, guardDefinition.Id, veteranDefinition.Id, 2, 20)
        ]);

        Assert.That(engine.State.GetUnit(guard.Id), Is.EqualTo(
            guard with { DefinitionId = veteranDefinition.Id, Hitpoints = 20, Experience = 0, ExperienceGained = 0, Level = 2 }));
    }

    [Test]
    public void RefusedUpgradeTest()
    {
        UnitDefinition squireDefinition = Fighter("squire", maxHitpoints: 10, initiative: 50, damage: 10, experienceNextLevel: 10, experienceKilled: 0);
        UnitDefinition defenderDefinition = Fighter("defender", maxHitpoints: 10, initiative: 40, damage: 1, experienceNextLevel: 10, experienceKilled: 5);

        BattleUnit squire = new(new BattleUnitId(0), null, squireDefinition.Id, BattleSide.Attacker, 10, 7, 0, 1, [], [], null, [], null, false);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(squireDefinition)
            .WithDefinition(defenderDefinition)
            .WithUpgradeResolver(new TestBattleUnitUpgradeResolver())
            .WithAttacker(squire, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(squire.Id, new BattleAttackUnitTarget(defender.Id)), [
            new BattleUnitAttackEvent(squire.Id, new BattleAttackUnitTarget(defender.Id), [new UnitAttackDefinitionId("squire")]),
            new BattleUnitAttackedEvent(squire.Id, defender.Id, new UnitAttackDefinitionId("squire")),
            new BattleUnitAttackHitEvent(squire.Id, defender.Id, new UnitAttackDefinitionId("squire"), 10),
            new BattleUnitDiedEvent(defender.Id),
            new BattleUnitExperienceGainedEvent(squire.Id, 5),
            new BattleUnitTurnEndingEvent(squire.Id),
            new BattleRoundEndedEvent(),
            new BattleEndedEvent(BattleSide.Attacker),
            new BattleUnitExperienceGainedEvent(squire.Id, 2)
        ]);

        Assert.That(engine.State.GetUnit(squire.Id), Is.EqualTo(squire with { ExperienceGained = 2 }));
    }

    [Test]
    public void RefusedUpgradeAtCapTest()
    {
        UnitDefinition squireDefinition = Fighter("squire", maxHitpoints: 10, initiative: 50, damage: 10, experienceNextLevel: 10, experienceKilled: 0);
        UnitDefinition defenderDefinition = Fighter("defender", maxHitpoints: 10, initiative: 40, damage: 1, experienceNextLevel: 10, experienceKilled: 5);

        BattleUnit squire = new(new BattleUnitId(0), null, squireDefinition.Id, BattleSide.Attacker, 10, 9, 0, 1, [], [], null, [], null, false);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(squireDefinition)
            .WithDefinition(defenderDefinition)
            .WithUpgradeResolver(new TestBattleUnitUpgradeResolver())
            .WithAttacker(squire, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(squire.Id, new BattleAttackUnitTarget(defender.Id)), [
            new BattleUnitAttackEvent(squire.Id, new BattleAttackUnitTarget(defender.Id), [new UnitAttackDefinitionId("squire")]),
            new BattleUnitAttackedEvent(squire.Id, defender.Id, new UnitAttackDefinitionId("squire")),
            new BattleUnitAttackHitEvent(squire.Id, defender.Id, new UnitAttackDefinitionId("squire"), 10),
            new BattleUnitDiedEvent(defender.Id),
            new BattleUnitExperienceGainedEvent(squire.Id, 5),
            new BattleUnitTurnEndingEvent(squire.Id),
            new BattleRoundEndedEvent(),
            new BattleEndedEvent(BattleSide.Attacker),
            new BattleUnitExperienceGainedEvent(squire.Id, 0)
        ]);

        Assert.That(engine.State.GetUnit(squire.Id), Is.EqualTo(squire with { ExperienceGained = 0 }));
    }

    [Test]
    public void NullUpgradeResolverTest()
    {
        UnitDefinition squireDefinition = Fighter("squire", maxHitpoints: 10, initiative: 50, damage: 10, experienceNextLevel: 10, experienceKilled: 0);
        UnitDefinition defenderDefinition = Fighter("defender", maxHitpoints: 10, initiative: 40, damage: 1, experienceNextLevel: 10, experienceKilled: 5);

        BattleUnit squire = new(new BattleUnitId(0), null, squireDefinition.Id, BattleSide.Attacker, 10, 7, 0, 1, [], [], null, [], null, false);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(squireDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(squire, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(squire.Id, new BattleAttackUnitTarget(defender.Id)), [
            new BattleUnitAttackEvent(squire.Id, new BattleAttackUnitTarget(defender.Id), [new UnitAttackDefinitionId("squire")]),
            new BattleUnitAttackedEvent(squire.Id, defender.Id, new UnitAttackDefinitionId("squire")),
            new BattleUnitAttackHitEvent(squire.Id, defender.Id, new UnitAttackDefinitionId("squire"), 10),
            new BattleUnitDiedEvent(defender.Id),
            new BattleUnitExperienceGainedEvent(squire.Id, 5),
            new BattleUnitTurnEndingEvent(squire.Id),
            new BattleRoundEndedEvent(),
            new BattleEndedEvent(BattleSide.Attacker),
            new BattleUnitExperienceGainedEvent(squire.Id, 2)
        ]);

        Assert.That(engine.State.GetUnit(squire.Id), Is.EqualTo(squire with { ExperienceGained = 2 }));
    }

    [Test]
    public void RetreatedUnitTest()
    {
        UnitDefinition squireDefinition = Fighter("squire", maxHitpoints: 10, initiative: 50, damage: 10, experienceNextLevel: 10, experienceKilled: 0);
        UnitDefinition knightDefinition = Fighter("knight", maxHitpoints: 20, initiative: 60, damage: 15, experienceNextLevel: 10, experienceKilled: 0);
        UnitDefinition enemyDefinition = Fighter("enemy", maxHitpoints: 100, initiative: 40, damage: 1, experienceNextLevel: 10, experienceKilled: 5);

        UnitModifierDefinition fastRetreatModifier = TestHelpers.SimpleModifier("fastretreat") with
        {
            FastRetreat = true
        };

        BattleUnit squire = new(new BattleUnitId(0), null, squireDefinition.Id, BattleSide.Attacker, 10, 7, 5, 1, [UnitModifier.Permanent(fastRetreatModifier.Id)], [], null, [], null, false);
        BattleUnit enemy = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, enemyDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(squireDefinition)
            .WithDefinition(knightDefinition)
            .WithDefinition(enemyDefinition)
            .WithDefinition(fastRetreatModifier)
            .WithUpgradeResolver(new TestBattleUnitUpgradeResolver().WithUpgrade(squireDefinition.Id, new BattleUnitUpgrade(knightDefinition.Id, 1, 20)))
            .WithAttacker(squire, GridPosition.FrontBottom)
            .WithDefender(enemy, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleRetreatCommand(squire.Id), [
            new BattleUnitRetreatedEvent(squire.Id),
            new BattleUnitTurnEndingEvent(squire.Id),
            new BattleRoundEndedEvent(),
            new BattleEndedEvent(BattleSide.Defender),
            new BattleUnitExperienceGainedEvent(squire.Id, 2),
            new BattleUnitExperienceGainedEvent(enemy.Id, 1)
        ]);

        Assert.That(engine.State.GetUnit(squire.Id), Is.EqualTo(
            squire with { ExperienceGained = 2, StatusEffects = [new BattleRetreatedStatusEffect()] }));
    }
}
