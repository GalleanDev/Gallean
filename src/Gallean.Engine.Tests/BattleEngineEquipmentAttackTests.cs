using Gallean.Entities.GameData;
using Gallean.Tests.Common;

namespace Gallean.Engine.Tests;

public sealed class BattleEngineEquipmentAttackTests
{
    private static UnitDefinition LeaderDefinition(int damage)
    {
        return TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: damage) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            LeaderAbilities = LeaderAbility.WeaponArmorUse
        };
    }

    private static UnitAttackDefinition PoisonAttack(UnitAttackDefinitionId id, int damage)
    {
        return TestHelpers.SimpleAttack(id, initiative: 30, damage: damage, accuracy: 100, reach: UnitAttackReach.Any) with
        {
            Type = UnitAttackType.Poison,
            Source = UnitAttackSource.Death
        };
    }

    [Test]
    public void PoisonsTargetTest()
    {
        UnitDefinition leaderDefinition = LeaderDefinition(damage: 10);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 100, initiative: 40, damage: 5);
        UnitAttackDefinition bladeAttack = PoisonAttack(new UnitAttackDefinitionId("thanatos-blade"), damage: 20);

        BattleUnit leader = new(new BattleUnitId(0), null, leaderDefinition.Id, BattleSide.Attacker, 100, 0, 0, 1, [], [], null, [bladeAttack.Id], null, false);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(leaderDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(bladeAttack)
            .WithAttacker(leader, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(leader.Id, new BattleAttackUnitTarget(defender.Id)), [
            new BattleUnitAttackEvent(leader.Id, new BattleAttackUnitTarget(defender.Id), [leaderDefinition.Attack.Id, bladeAttack.Id]),
            new BattleUnitAttackedEvent(leader.Id, defender.Id, leaderDefinition.Attack.Id),
            new BattleUnitAttackHitEvent(leader.Id, defender.Id, leaderDefinition.Attack.Id, 10),
            new BattleUnitAttackedEvent(leader.Id, defender.Id, bladeAttack.Id),
            new BattleUnitAttackHitEvent(leader.Id, defender.Id, bladeAttack.Id, 0),
            new BattleUnitEffectAppliedEvent(leader.Id, defender.Id, new BattlePoisonStatusEffect(1, 20)),
            new BattleUnitTurnEndingEvent(leader.Id),
            new BattleUnitTurnEndedEvent(leader.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal)),
            new BattleUnitDamagedByEffectEvent(defender.Id, new BattlePoisonStatusEffect(1, 20), 20, 70),
            new BattleUnitEffectDurationDecreasedEvent(defender.Id),
            new BattleUnitEffectExpiredEvent(defender.Id, new BattlePoisonStatusEffect(0, 20))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(leader, GridPosition.FrontBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(defender with { Hitpoints = 70 }, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(1)
            }));
    }

    [Test]
    public void LeaderStatsTest()
    {
        UnitDefinition leaderDefinition = LeaderDefinition(damage: 10);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 200, initiative: 40, damage: 5);
        UnitAttackDefinition bladeAttack = PoisonAttack(new UnitAttackDefinitionId("thanatos-blade"), damage: 20);
        UnitDynamicUpgradeDefinition dynamicUpgrade = new(
            leaderDefinition.Id,
            UpgradeChangeLevel: 10,
            LowLevelUpgrade: UnitDynamicUpgrade.Empty with { Damage = 10 },
            HighLevelUpgrade: UnitDynamicUpgrade.Empty);
        UnitModifierDefinition damageBonus = TestHelpers.SimpleModifier("damage") with
        {
            Damage = new UnitStatModifier(0, 100)
        };

        BattleUnit leader = new(
            new BattleUnitId(0),
            null,
            leaderDefinition.Id,
            BattleSide.Attacker,
            100,
            0,
            0,
            5,
            [UnitModifier.Permanent(damageBonus.Id)],
            [],
            null,
            [bladeAttack.Id],
            Name: null,
            IsTypeLocked: false);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 200);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(leaderDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(bladeAttack)
            .WithDefinition(dynamicUpgrade)
            .WithDefinition(damageBonus)
            .WithAttacker(leader, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(leader.Id, new BattleAttackUnitTarget(defender.Id)), [
            new BattleUnitAttackEvent(leader.Id, new BattleAttackUnitTarget(defender.Id), [leaderDefinition.Attack.Id, bladeAttack.Id]),
            new BattleUnitAttackedEvent(leader.Id, defender.Id, leaderDefinition.Attack.Id),
            new BattleUnitAttackHitEvent(leader.Id, defender.Id, leaderDefinition.Attack.Id, 100),
            new BattleUnitAttackedEvent(leader.Id, defender.Id, bladeAttack.Id),
            new BattleUnitAttackHitEvent(leader.Id, defender.Id, bladeAttack.Id, 0),
            new BattleUnitEffectAppliedEvent(leader.Id, defender.Id, new BattlePoisonStatusEffect(1, 20)),
            new BattleUnitTurnEndingEvent(leader.Id),
            new BattleUnitTurnEndedEvent(leader.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal)),
            new BattleUnitDamagedByEffectEvent(defender.Id, new BattlePoisonStatusEffect(1, 20), 20, 80),
            new BattleUnitEffectDurationDecreasedEvent(defender.Id),
            new BattleUnitEffectExpiredEvent(defender.Id, new BattlePoisonStatusEffect(0, 20))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(leader, GridPosition.FrontBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(defender with { Hitpoints = 80 }, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(1)
            }));
    }

    [Test]
    public void TwoArtifactsTest()
    {
        UnitDefinition leaderDefinition = LeaderDefinition(damage: 10);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 100, initiative: 40, damage: 5);
        UnitAttackDefinition bladeAttack = PoisonAttack(new UnitAttackDefinitionId("thanatos-blade"), damage: 20);
        UnitAttackDefinition crystalAttack = TestHelpers.SimpleAttack(
                new UnitAttackDefinitionId("soul-crystal"),
                initiative: 30,
                damage: 0,
                accuracy: 100,
                reach: UnitAttackReach.Any) with
            {
                Type = UnitAttackType.Paralyze,
                Source = UnitAttackSource.Weapon
            };

        BattleUnit leader = new(
            new BattleUnitId(0),
            null,
            leaderDefinition.Id,
            BattleSide.Attacker,
            100,
            0,
            0,
            1,
            [],
            [],
            null,
            [bladeAttack.Id, crystalAttack.Id],
            Name: null,
            IsTypeLocked: false);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(leaderDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(bladeAttack)
            .WithDefinition(crystalAttack)
            .WithAttacker(leader, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(leader.Id, new BattleAttackUnitTarget(defender.Id)), [
            new BattleUnitAttackEvent(leader.Id, new BattleAttackUnitTarget(defender.Id), [leaderDefinition.Attack.Id, bladeAttack.Id, crystalAttack.Id]),
            new BattleUnitAttackedEvent(leader.Id, defender.Id, leaderDefinition.Attack.Id),
            new BattleUnitAttackHitEvent(leader.Id, defender.Id, leaderDefinition.Attack.Id, 10),
            new BattleUnitAttackedEvent(leader.Id, defender.Id, bladeAttack.Id),
            new BattleUnitAttackHitEvent(leader.Id, defender.Id, bladeAttack.Id, 0),
            new BattleUnitEffectAppliedEvent(leader.Id, defender.Id, new BattlePoisonStatusEffect(1, 20)),
            new BattleUnitAttackedEvent(leader.Id, defender.Id, crystalAttack.Id),
            new BattleUnitAttackHitEvent(leader.Id, defender.Id, crystalAttack.Id, 0),
            new BattleUnitEffectAppliedEvent(leader.Id, defender.Id, new BattleParalyzeStatusEffect(1)),
            new BattleUnitTurnEndingEvent(leader.Id),
            new BattleUnitTurnEndedEvent(leader.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal)),
            new BattleUnitDamagedByEffectEvent(defender.Id, new BattlePoisonStatusEffect(1, 20), 20, 70),
            new BattleUnitEffectDurationDecreasedEvent(defender.Id),
            new BattleUnitEffectExpiredEvent(defender.Id, new BattlePoisonStatusEffect(0, 20)),
            new BattleUnitEffectExpiredEvent(defender.Id, new BattleParalyzeStatusEffect(0)),
            new BattleUnitTurnSkippedEvent(defender.Id),
            new BattleUnitTurnEndingEvent(defender.Id),
            new BattleUnitTurnEndedEvent(defender.Id),
            new BattleRoundEndedEvent(),
            new BattleRoundStartedEvent(BattleRound.Normal(2), [new BattleUnitTurn(leader.Id, BattleUnitTurnType.Normal), new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal)]),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(leader.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(leader, GridPosition.FrontBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(defender with { Hitpoints = 70 }, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(leader.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(2)
            }));
    }

    [Test]
    public void UnreachableTargetTest()
    {
        UnitDefinition leaderDefinition = LeaderDefinition(damage: 10) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("leader"), initiative: 50, damage: 10, accuracy: 100, reach: UnitAttackReach.Adjacent)
        };
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 100, initiative: 40, damage: 5);
        UnitAttackDefinition bladeAttack = PoisonAttack(new UnitAttackDefinitionId("thanatos-blade"), damage: 20);

        BattleUnit leader = new(new BattleUnitId(0), null, leaderDefinition.Id, BattleSide.Attacker, 100, 0, 0, 1, [], [], null, [bladeAttack.Id], null, false);
        BattleUnit frontDefender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 100);
        BattleUnit backDefender = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(leaderDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(bladeAttack)
            .WithAttacker(leader, GridPosition.FrontBottom)
            .WithDefender(frontDefender, GridPosition.FrontTop)
            .WithDefender(backDefender, GridPosition.BackTop)
            .Build();

        engine.CheckCommandError(
            new BattleAttackCommand(leader.Id, new BattleAttackUnitTarget(backDefender.Id)),
            BattleCommandError.TargetNotReachable);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(leader, GridPosition.FrontBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(frontDefender, GridPosition.FrontTop, false)
                    .Place(backDefender, GridPosition.BackTop, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(leader.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(1)
            }));
    }

    [Test]
    public void MissedPrimaryTest()
    {
        UnitDefinition leaderDefinition = LeaderDefinition(damage: 10);
        leaderDefinition = leaderDefinition with { Attack = leaderDefinition.Attack with { Accuracy = 10 } };
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 100, initiative: 40, damage: 5);
        UnitAttackDefinition bladeAttack = PoisonAttack(new UnitAttackDefinitionId("thanatos-blade"), damage: 20);

        BattleUnit leader = new(new BattleUnitId(0), null, leaderDefinition.Id, BattleSide.Attacker, 100, 0, 0, 1, [], [], null, [bladeAttack.Id], null, false);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(leaderDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(bladeAttack)
            .WithAttacker(leader, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .WithRandom(new TestSequenceRandomProvider(99, 99, 99, 99))
            .Build();

        engine.CheckCommand(new BattleAttackCommand(leader.Id, new BattleAttackUnitTarget(defender.Id)), [
            new BattleUnitAttackEvent(leader.Id, new BattleAttackUnitTarget(defender.Id), [leaderDefinition.Attack.Id, bladeAttack.Id]),
            new BattleUnitAttackedEvent(leader.Id, defender.Id, leaderDefinition.Attack.Id),
            new BattleUnitAttackMissedEvent(leader.Id, defender.Id, leaderDefinition.Attack.Id),
            new BattleUnitTurnEndingEvent(leader.Id),
            new BattleUnitTurnEndedEvent(leader.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(leader, GridPosition.FrontBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(defender, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(1)
            }));
    }

    [Test]
    public void HornOfIncubusTest()
    {
        GameDefinitionProvider gameDefinitions = new(GlobalDatabaseLoader.Load(RiseOfTheElves.GlobalDatabasePath));
        ItemDefinition horn = gameDefinitions.GetItem(new ItemDefinitionId("G000IG3016"));
        UnitAttackDefinitionId hornAttackId = Assert.NotNull(horn.EquipmentAttackId);
        UnitAttackDefinition hornAttack = gameDefinitions.GetAttack(hornAttackId);

        UnitDefinition leaderDefinition = LeaderDefinition(damage: 10);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 100, initiative: 40, damage: 5);

        BattleUnit leader = new(new BattleUnitId(0), null, leaderDefinition.Id, BattleSide.Attacker, 100, 0, 0, 1, [], [], null, [hornAttack.Id], null, false);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(leaderDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(hornAttack)
            .WithAttacker(leader, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(leader.Id, new BattleAttackUnitTarget(defender.Id)), [
            new BattleUnitAttackEvent(leader.Id, new BattleAttackUnitTarget(defender.Id), [leaderDefinition.Attack.Id, new UnitAttackDefinitionId("g000aa9016")]),
            new BattleUnitAttackedEvent(leader.Id, defender.Id, leaderDefinition.Attack.Id),
            new BattleUnitAttackHitEvent(leader.Id, defender.Id, leaderDefinition.Attack.Id, 10),
            new BattleUnitAttackedEvent(leader.Id, defender.Id, new UnitAttackDefinitionId("g000aa9016")),
            new BattleUnitAttackHitEvent(leader.Id, defender.Id, new UnitAttackDefinitionId("g000aa9016"), 0),
            new BattleUnitEffectAppliedEvent(leader.Id, defender.Id, new BattlePetrifyStatusEffect(1)),
            new BattleUnitTurnEndingEvent(leader.Id),
            new BattleUnitTurnEndedEvent(leader.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal)),
            new BattleUnitEffectDurationDecreasedEvent(defender.Id),
            new BattleUnitEffectExpiredEvent(defender.Id, new BattlePetrifyStatusEffect(0)),
            new BattleUnitTurnSkippedEvent(defender.Id),
            new BattleUnitTurnEndingEvent(defender.Id),
            new BattleUnitTurnEndedEvent(defender.Id),
            new BattleRoundEndedEvent(),
            new BattleRoundStartedEvent(BattleRound.Normal(2), [new BattleUnitTurn(leader.Id, BattleUnitTurnType.Normal), new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal)]),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(leader.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(leader, GridPosition.FrontBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(defender with { Hitpoints = 90 }, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(leader.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(2)
            }));
    }
}
