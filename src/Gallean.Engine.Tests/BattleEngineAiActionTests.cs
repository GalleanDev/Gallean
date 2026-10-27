using System.Collections.Immutable;

namespace Gallean.Engine.Tests;

public sealed class BattleEngineAiActionTests
{
    private static BattleUnit Unit(int id, UnitDefinitionId definitionId, BattleSide side, int hitpoints)
    {
        return TestHelpers.SimpleBattleUnit(new BattleUnitId(id), null, definitionId, side, hitpoints: hitpoints);
    }

    private static UnitDefinition SupportDefinition(string id, UnitAttackType type, int heal, UnitDefinitionId? summonUnitId)
    {
        ImmutableArray<UnitDefinitionId> summonUnitIds = [];

        if (summonUnitId is not null)
        {
            summonUnitIds = [summonUnitId.Value];
        }

        return TestHelpers.SimpleUnitDefinition(id, maxHitpoints: 30, initiative: 50, damage: 0) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId(id), initiative: 50, damage: 0, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Type = type,
                Heal = heal,
                Level = 1,
                SummonUnitIds = summonUnitIds
            }
        };
    }

    private static UnitDefinition ControlDefinition(string id, UnitAttackType type)
    {
        return TestHelpers.SimpleUnitDefinition(id, maxHitpoints: 30, initiative: 60, damage: 0) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId(id), initiative: 60, damage: 0, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Type = type,
                Source = UnitAttackSource.Mind,
                Level = 1
            }
        };
    }

    private static UnitDefinition TransformDefinition(string id, UnitDefinitionId transformUnitId)
    {
        return TestHelpers.SimpleUnitDefinition(id, maxHitpoints: 30, initiative: 60, damage: 0) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId(id), initiative: 60, damage: 0, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Type = UnitAttackType.TransformEnemy,
                Source = UnitAttackSource.Mind,
                Level = 1,
                SmallTransformUnitId = transformUnitId
            }
        };
    }

    private static UnitDefinition FireDefinition(string id, int damage)
    {
        return TestHelpers.SimpleUnitDefinition(id, maxHitpoints: 100, initiative: 50, damage: damage) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId(id), initiative: 50, damage: damage, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Source = UnitAttackSource.Fire
            }
        };
    }

    private static UnitDefinition LeaderDefinition(string id, int damage)
    {
        return TestHelpers.SimpleUnitDefinition(id, maxHitpoints: 100, initiative: 50, damage: damage) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Leadership = 1
        };
    }

    private static ItemDefinition PotionDefinition(string id, ItemCategory category, ItemEffect effect)
    {
        return TestHelpers.SimpleItemDefinition(new ItemDefinitionId(id), category) with
        {
            Effect = effect
        };
    }

    [Test]
    public void SimpleTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("unit0", maxHitpoints: 20, initiative: 50, damage: 5);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("unit1", maxHitpoints: 20, initiative: 40, damage: 5);

        BattleUnit attacker = Unit(0, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 20);
        BattleUnit defender = Unit(1, defenderDefinition.Id, BattleSide.Defender, hitpoints: 20);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(attacker, new GridPosition(0, 0))
            .WithDefender(defender, new GridPosition(0, 0))
            .Build();

        engine.CheckCommand(new BattleAiActionCommand(), [
            new BattleUnitAttackEvent(attacker.Id, new BattleAttackUnitTarget(defender.Id), [attackerDefinition.Attack.Id]),
            new BattleUnitAttackedEvent(attacker.Id, defender.Id, attackerDefinition.Attack.Id),
            new BattleUnitAttackHitEvent(attacker.Id, defender.Id, attackerDefinition.Attack.Id, 5),
            new BattleUnitTurnEndingEvent(attacker.Id),
            new BattleUnitTurnEndedEvent(attacker.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal))
        ]);
    }

    [Test]
    public void TargetsLowestHitpointsTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 100, initiative: 50, damage: 5);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 50, initiative: 40, damage: 1);
        UnitDefinition woundedDefinition = TestHelpers.SimpleUnitDefinition("wounded", maxHitpoints: 10, initiative: 30, damage: 1);

        BattleUnit attacker = Unit(0, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit defender = Unit(1, defenderDefinition.Id, BattleSide.Defender, hitpoints: 50);
        BattleUnit wounded = Unit(2, woundedDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(woundedDefinition)
            .WithAttacker(attacker, new GridPosition(0, 0))
            .WithDefender(defender, new GridPosition(0, 0))
            .WithDefender(wounded, new GridPosition(0, 1))
            .Build();

        engine.CheckCommand(new BattleAiActionCommand(), [
            new BattleUnitAttackEvent(attacker.Id, new BattleAttackUnitTarget(wounded.Id), [attackerDefinition.Attack.Id]),
            new BattleUnitAttackedEvent(attacker.Id, wounded.Id, attackerDefinition.Attack.Id),
            new BattleUnitAttackHitEvent(attacker.Id, wounded.Id, attackerDefinition.Attack.Id, 5),
            new BattleUnitTurnEndingEvent(attacker.Id),
            new BattleUnitTurnEndedEvent(attacker.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal))
        ]);
    }

    [Test]
    public void PrefersNonDefendingTargetTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 100, initiative: 50, damage: 5);
        UnitDefinition defendingDefinition = TestHelpers.SimpleUnitDefinition("defending", maxHitpoints: 5, initiative: 40, damage: 1);
        UnitDefinition targetDefinition = TestHelpers.SimpleUnitDefinition("target", maxHitpoints: 30, initiative: 30, damage: 1);

        BattleUnit attacker = Unit(0, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit defending = Unit(1, defendingDefinition.Id, BattleSide.Defender, hitpoints: 5) with { StatusEffects = [new BattleDefendStatusEffect()] };
        BattleUnit target = Unit(2, targetDefinition.Id, BattleSide.Defender, hitpoints: 30);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(defendingDefinition)
            .WithDefinition(targetDefinition)
            .WithAttacker(attacker, new GridPosition(0, 0))
            .WithDefender(defending, new GridPosition(0, 0))
            .WithDefender(target, new GridPosition(0, 1))
            .Build();

        engine.CheckCommand(new BattleAiActionCommand(), [
            new BattleUnitAttackEvent(attacker.Id, new BattleAttackUnitTarget(target.Id), [attackerDefinition.Attack.Id]),
            new BattleUnitAttackedEvent(attacker.Id, target.Id, attackerDefinition.Attack.Id),
            new BattleUnitAttackHitEvent(attacker.Id, target.Id, attackerDefinition.Attack.Id, 5),
            new BattleUnitTurnEndingEvent(attacker.Id),
            new BattleUnitTurnEndedEvent(attacker.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defending.Id, BattleUnitTurnType.Normal))
        ]);
    }

    [Test]
    public void SkipsUnreachableTargetsTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("attacker"), initiative: 50, damage: 10, accuracy: 100, reach: UnitAttackReach.Adjacent)
        };
        UnitDefinition unreachableDefinition = TestHelpers.SimpleUnitDefinition("unreachable", maxHitpoints: 5, initiative: 40, damage: 1);
        UnitDefinition reachableDefinition = TestHelpers.SimpleUnitDefinition("reachable", maxHitpoints: 30, initiative: 30, damage: 1);

        BattleUnit attacker = Unit(0, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit unreachable = Unit(1, unreachableDefinition.Id, BattleSide.Defender, hitpoints: 5);
        BattleUnit reachable = Unit(2, reachableDefinition.Id, BattleSide.Defender, hitpoints: 30);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(unreachableDefinition)
            .WithDefinition(reachableDefinition)
            .WithAttacker(attacker, new GridPosition(0, 0))
            .WithDefender(unreachable, new GridPosition(1, 0))
            .WithDefender(reachable, new GridPosition(0, 0))
            .Build();

        engine.CheckCommand(new BattleAiActionCommand(), [
            new BattleUnitAttackEvent(attacker.Id, new BattleAttackUnitTarget(reachable.Id), [attackerDefinition.Attack.Id]),
            new BattleUnitAttackedEvent(attacker.Id, reachable.Id, attackerDefinition.Attack.Id),
            new BattleUnitAttackHitEvent(attacker.Id, reachable.Id, attackerDefinition.Attack.Id, 10),
            new BattleUnitTurnEndingEvent(attacker.Id),
            new BattleUnitTurnEndedEvent(attacker.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(unreachable.Id, BattleUnitTurnType.Normal))
        ]);
    }

    [Test]
    public void DefendsWhenTargetsUnreachableTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("attacker"), initiative: 50, damage: 10, accuracy: 100, reach: UnitAttackReach.Adjacent)
        };
        UnitDefinition blockerDefinition = TestHelpers.SimpleUnitDefinition("blocker", maxHitpoints: 50, initiative: 30, damage: 1);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 50, initiative: 40, damage: 1);

        BattleUnit attacker = Unit(0, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit blocker = Unit(1, blockerDefinition.Id, BattleSide.Attacker, hitpoints: 50);
        BattleUnit defender = Unit(2, defenderDefinition.Id, BattleSide.Defender, hitpoints: 50);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(blockerDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(attacker, new GridPosition(1, 0))
            .WithAttacker(blocker, new GridPosition(0, 0))
            .WithDefender(defender, new GridPosition(0, 0))
            .Build();

        engine.CheckCommand(new BattleAiActionCommand(), [
            new BattleUnitDefendedEvent(attacker.Id),
            new BattleUnitTurnEndingEvent(attacker.Id),
            new BattleUnitTurnEndedEvent(attacker.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal))
        ]);
    }

    [Test]
    public void HealsMostWoundedAllyTest()
    {
        UnitDefinition healerDefinition = SupportDefinition("healer", UnitAttackType.Heal, heal: 10, summonUnitId: null);
        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ally", maxHitpoints: 50, initiative: 40, damage: 5);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 30, initiative: 10, damage: 5);

        BattleUnit healer = Unit(0, healerDefinition.Id, BattleSide.Attacker, hitpoints: 30);
        BattleUnit lightlyWounded = Unit(1, allyDefinition.Id, BattleSide.Attacker, hitpoints: 40);
        BattleUnit heavilyWounded = Unit(2, allyDefinition.Id, BattleSide.Attacker, hitpoints: 20);
        BattleUnit defender = Unit(3, defenderDefinition.Id, BattleSide.Defender, hitpoints: 30);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(healerDefinition)
            .WithDefinition(allyDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(healer, new GridPosition(0, 0))
            .WithAttacker(lightlyWounded, new GridPosition(0, 1))
            .WithAttacker(heavilyWounded, new GridPosition(0, 2))
            .WithDefender(defender, new GridPosition(0, 0))
            .Build();

        engine.CheckCommand(new BattleAiActionCommand(), [
            new BattleUnitAttackEvent(healer.Id, new BattleAttackUnitTarget(heavilyWounded.Id), [healerDefinition.Attack.Id]),
            new BattleUnitAttackedEvent(healer.Id, heavilyWounded.Id, healerDefinition.Attack.Id),
            new BattleUnitAttackHitEvent(healer.Id, heavilyWounded.Id, healerDefinition.Attack.Id, 0),
            new BattleUnitHealedEvent(heavilyWounded.Id, 30),
            new BattleUnitTurnEndingEvent(healer.Id),
            new BattleUnitTurnEndedEvent(healer.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(lightlyWounded.Id, BattleUnitTurnType.Normal))
        ]);
    }

    [Test]
    public void DefendsWhenNoAllyWoundedTest()
    {
        UnitDefinition healerDefinition = SupportDefinition("healer", UnitAttackType.Heal, heal: 10, summonUnitId: null);
        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ally", maxHitpoints: 50, initiative: 40, damage: 5);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 30, initiative: 10, damage: 5);

        BattleUnit healer = Unit(0, healerDefinition.Id, BattleSide.Attacker, hitpoints: 30);
        BattleUnit ally = Unit(1, allyDefinition.Id, BattleSide.Attacker, hitpoints: 50);
        BattleUnit defender = Unit(2, defenderDefinition.Id, BattleSide.Defender, hitpoints: 30);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(healerDefinition)
            .WithDefinition(allyDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(healer, new GridPosition(0, 0))
            .WithAttacker(ally, new GridPosition(0, 1))
            .WithDefender(defender, new GridPosition(0, 0))
            .Build();

        engine.CheckCommand(new BattleAiActionCommand(), [
            new BattleUnitDefendedEvent(healer.Id),
            new BattleUnitTurnEndingEvent(healer.Id),
            new BattleUnitTurnEndedEvent(healer.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(ally.Id, BattleUnitTurnType.Normal))
        ]);
    }

    [Test]
    public void BoostsUnboostedDamageAllyTest()
    {
        UnitDefinition boosterDefinition = SupportDefinition("booster", UnitAttackType.IncreaseDamage, heal: 0, summonUnitId: null);
        UnitDefinition weakDefinition = TestHelpers.SimpleUnitDefinition("weak", maxHitpoints: 30, initiative: 40, damage: 5);
        UnitDefinition strongDefinition = TestHelpers.SimpleUnitDefinition("strong", maxHitpoints: 30, initiative: 30, damage: 15);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 30, initiative: 10, damage: 5);

        BattleUnit booster = Unit(0, boosterDefinition.Id, BattleSide.Attacker, hitpoints: 30);
        BattleUnit unboosted = Unit(1, weakDefinition.Id, BattleSide.Attacker, hitpoints: 30);
        BattleUnit alreadyBoosted = Unit(2, strongDefinition.Id, BattleSide.Attacker, hitpoints: 30) with { StatusEffects = [new BattleIncreaseDamageStatusEffect(50, false)] };
        BattleUnit defender = Unit(3, defenderDefinition.Id, BattleSide.Defender, hitpoints: 30);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(boosterDefinition)
            .WithDefinition(weakDefinition)
            .WithDefinition(strongDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(booster, new GridPosition(0, 0))
            .WithAttacker(unboosted, new GridPosition(0, 1))
            .WithAttacker(alreadyBoosted, new GridPosition(0, 2))
            .WithDefender(defender, new GridPosition(0, 0))
            .Build();

        engine.CheckCommand(new BattleAiActionCommand(), [
            new BattleUnitAttackEvent(booster.Id, new BattleAttackUnitTarget(unboosted.Id), [boosterDefinition.Attack.Id]),
            new BattleUnitAttackedEvent(booster.Id, unboosted.Id, boosterDefinition.Attack.Id),
            new BattleUnitAttackHitEvent(booster.Id, unboosted.Id, boosterDefinition.Attack.Id, 0),
            new BattleUnitEffectAppliedEvent(booster.Id, unboosted.Id, new BattleIncreaseDamageStatusEffect(25, false)),
            new BattleUnitTurnEndingEvent(booster.Id),
            new BattleUnitTurnEndedEvent(booster.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(unboosted.Id, BattleUnitTurnType.Normal))
        ]);
    }

    [Test]
    public void CuresAfflictedAllyTest()
    {
        UnitDefinition curerDefinition = SupportDefinition("curer", UnitAttackType.Cure, heal: 0, summonUnitId: null);
        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ally", maxHitpoints: 30, initiative: 40, damage: 5);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 30, initiative: 10, damage: 5);

        BattleUnit curer = Unit(0, curerDefinition.Id, BattleSide.Attacker, hitpoints: 30);
        BattleUnit healthy = Unit(1, allyDefinition.Id, BattleSide.Attacker, hitpoints: 30) with { StatusEffects = [new BattleIncreaseDamageStatusEffect(50, false)] };
        BattleUnit poisoned = Unit(2, allyDefinition.Id, BattleSide.Attacker, hitpoints: 30) with { StatusEffects = [new BattlePoisonStatusEffect(3, 2)] };
        BattleUnit defender = Unit(3, defenderDefinition.Id, BattleSide.Defender, hitpoints: 30);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(curerDefinition)
            .WithDefinition(allyDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(curer, new GridPosition(0, 0))
            .WithAttacker(healthy, new GridPosition(0, 1))
            .WithAttacker(poisoned, new GridPosition(0, 2))
            .WithDefender(defender, new GridPosition(0, 0))
            .Build();

        engine.CheckCommand(new BattleAiActionCommand(), [
            new BattleUnitAttackEvent(curer.Id, new BattleAttackUnitTarget(poisoned.Id), [curerDefinition.Attack.Id]),
            new BattleUnitAttackedEvent(curer.Id, poisoned.Id, curerDefinition.Attack.Id),
            new BattleUnitAttackHitEvent(curer.Id, poisoned.Id, curerDefinition.Attack.Id, 0),
            new BattleUnitCuredEvent(poisoned.Id),
            new BattleUnitTurnEndingEvent(curer.Id),
            new BattleUnitTurnEndedEvent(curer.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(healthy.Id, BattleUnitTurnType.Normal))
        ]);
    }

    [Test]
    public void RevivesDeadAllyTest()
    {
        UnitDefinition reviverDefinition = SupportDefinition("reviver", UnitAttackType.Revive, heal: 10, summonUnitId: null);
        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ally", maxHitpoints: 30, initiative: 40, damage: 5);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 30, initiative: 10, damage: 5);

        BattleUnit reviver = Unit(0, reviverDefinition.Id, BattleSide.Attacker, hitpoints: 30);
        BattleUnit living = Unit(1, allyDefinition.Id, BattleSide.Attacker, hitpoints: 30);
        BattleUnit corpse = Unit(2, allyDefinition.Id, BattleSide.Attacker, hitpoints: 0);
        BattleUnit defender = Unit(3, defenderDefinition.Id, BattleSide.Defender, hitpoints: 30);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(reviverDefinition)
            .WithDefinition(allyDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(reviver, new GridPosition(0, 0))
            .WithAttacker(living, new GridPosition(0, 1))
            .WithAttacker(corpse, new GridPosition(0, 2))
            .WithDefender(defender, new GridPosition(0, 0))
            .Build();

        engine.CheckCommand(new BattleAiActionCommand(), [
            new BattleUnitAttackEvent(reviver.Id, new BattleAttackUnitTarget(corpse.Id), [reviverDefinition.Attack.Id]),
            new BattleUnitAttackedEvent(reviver.Id, corpse.Id, reviverDefinition.Attack.Id),
            new BattleUnitAttackHitEvent(reviver.Id, corpse.Id, reviverDefinition.Attack.Id, 0),
            new BattleUnitRevivedEvent(corpse.Id, 15),
            new BattleUnitTurnEndingEvent(reviver.Id),
            new BattleUnitTurnEndedEvent(reviver.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(living.Id, BattleUnitTurnType.Normal))
        ]);
    }

    [Test]
    public void SkipsAlreadyRevivedAllyTest()
    {
        UnitDefinition reviverDefinition = SupportDefinition("reviver", UnitAttackType.Revive, heal: 10, summonUnitId: null);
        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ally", maxHitpoints: 30, initiative: 40, damage: 5);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 30, initiative: 10, damage: 5);

        BattleUnit reviver = Unit(0, reviverDefinition.Id, BattleSide.Attacker, hitpoints: 30);
        BattleUnit corpse = Unit(1, allyDefinition.Id, BattleSide.Attacker, hitpoints: 0) with { IsRevived = true };
        BattleUnit defender = Unit(2, defenderDefinition.Id, BattleSide.Defender, hitpoints: 30);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(reviverDefinition)
            .WithDefinition(allyDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(reviver, new GridPosition(0, 0))
            .WithAttacker(corpse, new GridPosition(0, 1))
            .WithDefender(defender, new GridPosition(0, 0))
            .Build();

        engine.CheckCommand(new BattleAiActionCommand(), [
            new BattleUnitDefendedEvent(reviver.Id),
            new BattleUnitTurnEndingEvent(reviver.Id),
            new BattleUnitTurnEndedEvent(reviver.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal))
        ]);
    }

    [Test]
    public void SummonsIntoFreeSlotTest()
    {
        UnitDefinition summonedDefinition = TestHelpers.SimpleUnitDefinition("summoned", maxHitpoints: 20, initiative: 40, damage: 5);
        UnitDefinition summonerDefinition = SupportDefinition("summoner", UnitAttackType.Summon, heal: 0, summonUnitId: summonedDefinition.Id);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 30, initiative: 10, damage: 5);

        BattleUnit summoner = Unit(0, summonerDefinition.Id, BattleSide.Attacker, hitpoints: 30);
        BattleUnit defender = Unit(1, defenderDefinition.Id, BattleSide.Defender, hitpoints: 30);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(summonerDefinition)
            .WithDefinition(summonedDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(summoner, new GridPosition(0, 0))
            .WithDefender(defender, new GridPosition(0, 0))
            .Build();

        BattleUnitId summonedId = new(2);
        GridPosition summonPosition = new(0, 1);

        engine.CheckCommand(new BattleAiActionCommand(), [
            new BattleUnitAttackEvent(summoner.Id, new BattleAttackTileTarget(BattleSide.Attacker, summonPosition), [summonerDefinition.Attack.Id]),
            new BattleUnitSummonedEvent(summonedId, summonedDefinition.Id, BattleSide.Attacker, summonPosition, 20, 1, summoner.Id),
            new BattleUnitTurnEndingEvent(summoner.Id),
            new BattleUnitTurnEndedEvent(summoner.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal))
        ]);
    }

    [Test]
    public void TransformsSelfTest()
    {
        UnitDefinition beastDefinition = TestHelpers.SimpleUnitDefinition("beast", maxHitpoints: 50, initiative: 50, damage: 9);
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 30, initiative: 50, damage: 0) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("attacker"), initiative: 50, damage: 0, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Type = UnitAttackType.TransformSelf,
                SmallTransformUnitId = beastDefinition.Id
            }
        };
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 20, initiative: 40, damage: 5);

        BattleUnit attacker = Unit(0, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 30);
        BattleUnit defender = Unit(1, defenderDefinition.Id, BattleSide.Defender, hitpoints: 20);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(beastDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(attacker, new GridPosition(0, 0))
            .WithDefender(defender, new GridPosition(0, 0))
            .Build();

        engine.CheckCommand(new BattleAiActionCommand(), [
            new BattleUnitAttackEvent(attacker.Id, new BattleAttackUnitTarget(attacker.Id), [attackerDefinition.Attack.Id]),
            new BattleUnitTransformAppliedEvent(attacker.Id, attacker.Id, BattleTransformType.TransformSelf, beastDefinition.Id, 1, 50, 50, null)
        ]);

        engine.CheckCommand(new BattleAiActionCommand(), [
            new BattleUnitAttackEvent(attacker.Id, new BattleAttackUnitTarget(defender.Id), [beastDefinition.Attack.Id]),
            new BattleUnitAttackedEvent(attacker.Id, defender.Id, beastDefinition.Attack.Id),
            new BattleUnitAttackHitEvent(attacker.Id, defender.Id, beastDefinition.Attack.Id, 9),
            new BattleUnitTurnEndingEvent(attacker.Id),
            new BattleUnitTurnEndedEvent(attacker.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal))
        ]);
    }

    [Test]
    public void ParalyzesStrongestEnemyTest()
    {
        UnitDefinition paralyzerDefinition = ControlDefinition("paralyzer", UnitAttackType.Paralyze);
        UnitDefinition weakDefinition = TestHelpers.SimpleUnitDefinition("weak", maxHitpoints: 10, initiative: 40, damage: 1);
        UnitDefinition strongDefinition = TestHelpers.SimpleUnitDefinition("strong", maxHitpoints: 50, initiative: 30, damage: 20);

        BattleUnit paralyzer = Unit(0, paralyzerDefinition.Id, BattleSide.Attacker, hitpoints: 30);
        BattleUnit weak = Unit(1, weakDefinition.Id, BattleSide.Defender, hitpoints: 10);
        BattleUnit strong = Unit(2, strongDefinition.Id, BattleSide.Defender, hitpoints: 50);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(paralyzerDefinition)
            .WithDefinition(weakDefinition)
            .WithDefinition(strongDefinition)
            .WithAttacker(paralyzer, new GridPosition(0, 0))
            .WithDefender(weak, new GridPosition(0, 0))
            .WithDefender(strong, new GridPosition(0, 1))
            .Build();

        engine.CheckCommand(new BattleAiActionCommand(), [
            new BattleUnitAttackEvent(paralyzer.Id, new BattleAttackUnitTarget(strong.Id), [paralyzerDefinition.Attack.Id]),
            new BattleUnitAttackedEvent(paralyzer.Id, strong.Id, paralyzerDefinition.Attack.Id),
            new BattleUnitAttackHitEvent(paralyzer.Id, strong.Id, paralyzerDefinition.Attack.Id, 0),
            new BattleUnitEffectAppliedEvent(paralyzer.Id, strong.Id, new BattleParalyzeStatusEffect(1)),
            new BattleUnitTurnEndingEvent(paralyzer.Id),
            new BattleUnitTurnEndedEvent(paralyzer.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(weak.Id, BattleUnitTurnType.Normal))
        ]);
    }

    [Test]
    public void SkipsAlreadyDisabledEnemyTest()
    {
        UnitDefinition paralyzerDefinition = ControlDefinition("paralyzer", UnitAttackType.Paralyze);
        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ally", maxHitpoints: 30, initiative: 50, damage: 1);
        UnitDefinition strongDefinition = TestHelpers.SimpleUnitDefinition("strong", maxHitpoints: 50, initiative: 20, damage: 20);
        UnitDefinition weakDefinition = TestHelpers.SimpleUnitDefinition("weak", maxHitpoints: 10, initiative: 10, damage: 1);

        BattleUnit paralyzer = Unit(0, paralyzerDefinition.Id, BattleSide.Attacker, hitpoints: 30);
        BattleUnit ally = Unit(1, allyDefinition.Id, BattleSide.Attacker, hitpoints: 30);
        BattleUnit strong = Unit(2, strongDefinition.Id, BattleSide.Defender, hitpoints: 50) with { StatusEffects = [new BattleParalyzeStatusEffect(1)] };
        BattleUnit weak = Unit(3, weakDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(paralyzerDefinition)
            .WithDefinition(allyDefinition)
            .WithDefinition(strongDefinition)
            .WithDefinition(weakDefinition)
            .WithAttacker(paralyzer, new GridPosition(0, 0))
            .WithAttacker(ally, new GridPosition(0, 1))
            .WithDefender(strong, new GridPosition(0, 0))
            .WithDefender(weak, new GridPosition(0, 1))
            .Build();

        engine.CheckCommand(new BattleAiActionCommand(), [
            new BattleUnitAttackEvent(paralyzer.Id, new BattleAttackUnitTarget(weak.Id), [paralyzerDefinition.Attack.Id]),
            new BattleUnitAttackedEvent(paralyzer.Id, weak.Id, paralyzerDefinition.Attack.Id),
            new BattleUnitAttackHitEvent(paralyzer.Id, weak.Id, paralyzerDefinition.Attack.Id, 0),
            new BattleUnitEffectAppliedEvent(paralyzer.Id, weak.Id, new BattleParalyzeStatusEffect(1)),
            new BattleUnitTurnEndingEvent(paralyzer.Id),
            new BattleUnitTurnEndedEvent(paralyzer.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(ally.Id, BattleUnitTurnType.Normal))
        ]);
    }

    [Test]
    public void GivesAdditionalAttackToStrongestAllyTest()
    {
        UnitDefinition alchemistDefinition = TestHelpers.SimpleUnitDefinition("alchemist", maxHitpoints: 30, initiative: 60, damage: 0) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("alchemist"), initiative: 60, damage: 0, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Type = UnitAttackType.GiveAdditionalAttack
            }
        };
        UnitDefinition weakDefinition = TestHelpers.SimpleUnitDefinition("weak", maxHitpoints: 30, initiative: 20, damage: 5);
        UnitDefinition strongDefinition = TestHelpers.SimpleUnitDefinition("strong", maxHitpoints: 30, initiative: 10, damage: 20);
        UnitDefinition enemyDefinition = TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 30, initiative: 40, damage: 5);

        BattleUnit alchemist = Unit(0, alchemistDefinition.Id, BattleSide.Attacker, hitpoints: 30);
        BattleUnit weak = Unit(1, weakDefinition.Id, BattleSide.Attacker, hitpoints: 30);
        BattleUnit strong = Unit(2, strongDefinition.Id, BattleSide.Attacker, hitpoints: 30);
        BattleUnit enemy = Unit(3, enemyDefinition.Id, BattleSide.Defender, hitpoints: 30);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(alchemistDefinition)
            .WithDefinition(weakDefinition)
            .WithDefinition(strongDefinition)
            .WithDefinition(enemyDefinition)
            .WithAttacker(alchemist, new GridPosition(0, 0))
            .WithAttacker(weak, new GridPosition(0, 1))
            .WithAttacker(strong, new GridPosition(0, 2))
            .WithDefender(enemy, new GridPosition(0, 0))
            .Build();

        engine.CheckCommand(new BattleAiActionCommand(), [
            new BattleUnitAttackEvent(alchemist.Id, new BattleAttackUnitTarget(strong.Id), [alchemistDefinition.Attack.Id]),
            new BattleUnitAttackedEvent(alchemist.Id, strong.Id, alchemistDefinition.Attack.Id),
            new BattleUnitAttackHitEvent(alchemist.Id, strong.Id, alchemistDefinition.Attack.Id, 0),
            new BattleUnitAttackGrantedEvent(strong.Id),
            new BattleUnitTurnEndingEvent(alchemist.Id),
            new BattleUnitTurnEndedEvent(alchemist.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(strong.Id, BattleUnitTurnType.GrantedAttack))
        ]);
    }

    [Test]
    public void SkipsImmuneTargetTest()
    {
        UnitDefinition attackerDefinition = FireDefinition("attacker", damage: 5);
        UnitDefinition immuneDefinition = TestHelpers.SimpleUnitDefinition("immune", maxHitpoints: 5, initiative: 10, damage: 1) with
        {
            Wards = [new UnitAttackSourceWard(UnitAttackSource.Fire, UnitWardCategory.Always)]
        };
        UnitDefinition normalDefinition = TestHelpers.SimpleUnitDefinition("normal", maxHitpoints: 30, initiative: 40, damage: 1);

        BattleUnit attacker = Unit(0, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit immune = Unit(1, immuneDefinition.Id, BattleSide.Defender, hitpoints: 5);
        BattleUnit normal = Unit(2, normalDefinition.Id, BattleSide.Defender, hitpoints: 30);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(immuneDefinition)
            .WithDefinition(normalDefinition)
            .WithAttacker(attacker, new GridPosition(0, 0))
            .WithDefender(immune, new GridPosition(0, 0))
            .WithDefender(normal, new GridPosition(0, 1))
            .Build();

        engine.CheckCommand(new BattleAiActionCommand(), [
            new BattleUnitAttackEvent(attacker.Id, new BattleAttackUnitTarget(normal.Id), [attackerDefinition.Attack.Id]),
            new BattleUnitAttackedEvent(attacker.Id, normal.Id, attackerDefinition.Attack.Id),
            new BattleUnitAttackHitEvent(attacker.Id, normal.Id, attackerDefinition.Attack.Id, 5),
            new BattleUnitTurnEndingEvent(attacker.Id),
            new BattleUnitTurnEndedEvent(attacker.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(normal.Id, BattleUnitTurnType.Normal))
        ]);
    }

    [Test]
    public void PrefersUnwardedTargetTest()
    {
        UnitDefinition attackerDefinition = FireDefinition("attacker", damage: 5);
        UnitDefinition wardedDefinition = TestHelpers.SimpleUnitDefinition("warded", maxHitpoints: 5, initiative: 10, damage: 1) with
        {
            Wards = [new UnitAttackSourceWard(UnitAttackSource.Fire, UnitWardCategory.Once)]
        };
        UnitDefinition unwardedDefinition = TestHelpers.SimpleUnitDefinition("unwarded", maxHitpoints: 30, initiative: 40, damage: 1);

        BattleUnit attacker = Unit(0, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit warded = Unit(1, wardedDefinition.Id, BattleSide.Defender, hitpoints: 5);
        BattleUnit unwarded = Unit(2, unwardedDefinition.Id, BattleSide.Defender, hitpoints: 30);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(wardedDefinition)
            .WithDefinition(unwardedDefinition)
            .WithAttacker(attacker, new GridPosition(0, 0))
            .WithDefender(warded, new GridPosition(0, 0))
            .WithDefender(unwarded, new GridPosition(0, 1))
            .Build();

        engine.CheckCommand(new BattleAiActionCommand(), [
            new BattleUnitAttackEvent(attacker.Id, new BattleAttackUnitTarget(unwarded.Id), [attackerDefinition.Attack.Id]),
            new BattleUnitAttackedEvent(attacker.Id, unwarded.Id, attackerDefinition.Attack.Id),
            new BattleUnitAttackHitEvent(attacker.Id, unwarded.Id, attackerDefinition.Attack.Id, 5),
            new BattleUnitTurnEndingEvent(attacker.Id),
            new BattleUnitTurnEndedEvent(attacker.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(unwarded.Id, BattleUnitTurnType.Normal))
        ]);
    }

    [Test]
    public void OnlyWardedTargetTest()
    {
        UnitWard ward = new UnitAttackSourceWard(UnitAttackSource.Fire, UnitWardCategory.Once);
        UnitDefinition attackerDefinition = FireDefinition("attacker", damage: 5);
        UnitDefinition wardedDefinition = TestHelpers.SimpleUnitDefinition("warded", maxHitpoints: 30, initiative: 40, damage: 1) with
        {
            Wards = [ward]
        };

        BattleUnit attacker = Unit(0, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit warded = Unit(1, wardedDefinition.Id, BattleSide.Defender, hitpoints: 30);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(wardedDefinition)
            .WithAttacker(attacker, new GridPosition(0, 0))
            .WithDefender(warded, new GridPosition(0, 0))
            .Build();

        engine.CheckCommand(new BattleAiActionCommand(), [
            new BattleUnitAttackEvent(attacker.Id, new BattleAttackUnitTarget(warded.Id), [attackerDefinition.Attack.Id]),
            new BattleUnitAttackedEvent(attacker.Id, warded.Id, attackerDefinition.Attack.Id),
            new BattleUnitAttackBlockedEvent(attacker.Id, warded.Id, attackerDefinition.Attack.Id, ward),
            new BattleUnitWardConsumedEvent(warded.Id, ward),
            new BattleUnitTurnEndingEvent(attacker.Id),
            new BattleUnitTurnEndedEvent(attacker.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(warded.Id, BattleUnitTurnType.Normal))
        ]);
    }

    [Test]
    public void OnlyImmuneTargetTest()
    {
        UnitDefinition attackerDefinition = FireDefinition("attacker", damage: 5);
        UnitDefinition immuneDefinition = TestHelpers.SimpleUnitDefinition("immune", maxHitpoints: 30, initiative: 40, damage: 1) with
        {
            Wards = [new UnitAttackSourceWard(UnitAttackSource.Fire, UnitWardCategory.Always)]
        };

        BattleUnit attacker = Unit(0, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit immune = Unit(1, immuneDefinition.Id, BattleSide.Defender, hitpoints: 30);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(immuneDefinition)
            .WithAttacker(attacker, new GridPosition(0, 0))
            .WithDefender(immune, new GridPosition(0, 0))
            .Build();

        engine.CheckCommand(new BattleAiActionCommand(), [
            new BattleUnitDefendedEvent(attacker.Id),
            new BattleUnitTurnEndingEvent(attacker.Id),
            new BattleUnitTurnEndedEvent(attacker.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(immune.Id, BattleUnitTurnType.Normal))
        ]);
    }

    [Test]
    public void TransformsIntoStrongestUnitTest()
    {
        UnitDefinition doppelgangerDefinition = TestHelpers.SimpleUnitDefinition("doppelganger", maxHitpoints: 60, initiative: 50, damage: 5) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("doppelganger"), initiative: 50, damage: 5, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Type = UnitAttackType.Doppelganger,
                IsInfinite = true
            }
        };
        UnitDefinition militiaDefinition = TestHelpers.SimpleUnitDefinition("militia", maxHitpoints: 40, initiative: 30, damage: 10);
        UnitDefinition championDefinition = TestHelpers.SimpleUnitDefinition("champion", maxHitpoints: 200, initiative: 40, damage: 60);

        BattleUnit doppelganger = Unit(0, doppelgangerDefinition.Id, BattleSide.Attacker, hitpoints: 60);
        BattleUnit militia = Unit(1, militiaDefinition.Id, BattleSide.Defender, hitpoints: 40);
        BattleUnit champion = Unit(2, championDefinition.Id, BattleSide.Defender, hitpoints: 200);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(doppelgangerDefinition)
            .WithDefinition(militiaDefinition)
            .WithDefinition(championDefinition)
            .WithAttacker(doppelganger, new GridPosition(0, 0))
            .WithDefender(militia, new GridPosition(0, 0))
            .WithDefender(champion, new GridPosition(0, 1))
            .Build();

        engine.CheckCommand(new BattleAiActionCommand(), [
            new BattleUnitAttackEvent(doppelganger.Id, new BattleAttackUnitTarget(champion.Id), [doppelgangerDefinition.Attack.Id]),
            new BattleUnitTransformAppliedEvent(doppelganger.Id, doppelganger.Id, BattleTransformType.Doppelganger, championDefinition.Id, 1, 200, 200, null),
            new BattleUnitTurnEndingEvent(doppelganger.Id),
            new BattleUnitTurnEndedEvent(doppelganger.Id),
            new BattleRoundEndedEvent(),
            new BattleRoundStartedEvent(
                BattleRound.Normal(1),
                [
                    new BattleUnitTurn(doppelganger.Id, BattleUnitTurnType.Normal),
                    new BattleUnitTurn(champion.Id, BattleUnitTurnType.Normal),
                    new BattleUnitTurn(militia.Id, BattleUnitTurnType.Normal)
                ]),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(doppelganger.Id, BattleUnitTurnType.Normal))
        ]);
    }

    [Test]
    public void SkipsAlreadyTransformedEnemyTest()
    {
        UnitDefinition beastDefinition = TestHelpers.SimpleUnitDefinition("beast", maxHitpoints: 50, initiative: 40, damage: 20);
        UnitDefinition transformerDefinition = TransformDefinition("transformer", beastDefinition.Id);
        UnitDefinition strongDefinition = TestHelpers.SimpleUnitDefinition("strong", maxHitpoints: 50, initiative: 40, damage: 30);
        UnitDefinition weakDefinition = TestHelpers.SimpleUnitDefinition("weak", maxHitpoints: 10, initiative: 30, damage: 5);

        BattleUnit transformer = Unit(0, transformerDefinition.Id, BattleSide.Attacker, hitpoints: 30);
        BattleUnit strong = Unit(1, beastDefinition.Id, BattleSide.Defender, hitpoints: 50) with
        {
            StatusEffects = [new BattleTransformStatusEffect(BattleTransformType.Transform, strongDefinition.Id, 1, [], null)]
        };
        BattleUnit weak = Unit(2, weakDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(transformerDefinition)
            .WithDefinition(beastDefinition)
            .WithDefinition(strongDefinition)
            .WithDefinition(weakDefinition)
            .WithAttacker(transformer, new GridPosition(0, 0))
            .WithDefender(strong, new GridPosition(0, 0))
            .WithDefender(weak, new GridPosition(0, 1))
            .Build();

        engine.CheckCommand(new BattleAiActionCommand(), [
            new BattleUnitAttackEvent(transformer.Id, new BattleAttackUnitTarget(weak.Id), [transformerDefinition.Attack.Id]),
            new BattleUnitAttackedEvent(transformer.Id, weak.Id, transformerDefinition.Attack.Id),
            new BattleUnitAttackHitEvent(transformer.Id, weak.Id, transformerDefinition.Attack.Id, 0),
            new BattleUnitTransformAppliedEvent(transformer.Id, weak.Id, BattleTransformType.Transform, beastDefinition.Id, 1, 10, 10, null),
            new BattleUnitTurnEndingEvent(transformer.Id),
            new BattleUnitTurnEndedEvent(transformer.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(strong.Id, BattleUnitTurnType.Normal))
        ]);
    }

    [Test]
    public void TransformsReducedLevelEnemyTest()
    {
        UnitDefinition beastDefinition = TestHelpers.SimpleUnitDefinition("beast", maxHitpoints: 50, initiative: 40, damage: 20);
        UnitDefinition transformerDefinition = TransformDefinition("transformer", beastDefinition.Id);
        UnitDefinition veteranDefinition = TestHelpers.SimpleUnitDefinition("veteran", maxHitpoints: 80, initiative: 40, damage: 60);
        UnitDefinition strongDefinition = TestHelpers.SimpleUnitDefinition("strong", maxHitpoints: 50, initiative: 40, damage: 30);
        UnitDefinition weakDefinition = TestHelpers.SimpleUnitDefinition("weak", maxHitpoints: 10, initiative: 30, damage: 5);

        BattleUnit transformer = Unit(0, transformerDefinition.Id, BattleSide.Attacker, hitpoints: 30);
        BattleUnit strong = Unit(1, strongDefinition.Id, BattleSide.Defender, hitpoints: 50) with
        {
            StatusEffects = [new BattleTransformStatusEffect(BattleTransformType.LevelDrain, veteranDefinition.Id, 1, [], null)]
        };
        BattleUnit weak = Unit(2, weakDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(transformerDefinition)
            .WithDefinition(beastDefinition)
            .WithDefinition(veteranDefinition)
            .WithDefinition(strongDefinition)
            .WithDefinition(weakDefinition)
            .WithAttacker(transformer, new GridPosition(0, 0))
            .WithDefender(strong, new GridPosition(0, 0))
            .WithDefender(weak, new GridPosition(0, 1))
            .Build();

        engine.CheckCommand(new BattleAiActionCommand(), [
            new BattleUnitAttackEvent(transformer.Id, new BattleAttackUnitTarget(strong.Id), [transformerDefinition.Attack.Id]),
            new BattleUnitAttackedEvent(transformer.Id, strong.Id, transformerDefinition.Attack.Id),
            new BattleUnitAttackHitEvent(transformer.Id, strong.Id, transformerDefinition.Attack.Id, 0),
            new BattleUnitTransformAppliedEvent(transformer.Id, strong.Id, BattleTransformType.Transform, beastDefinition.Id, 1, 80, 80, null),
            new BattleUnitTurnEndingEvent(transformer.Id),
            new BattleUnitTurnEndedEvent(transformer.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(strong.Id, BattleUnitTurnType.Normal))
        ]);
    }

    [Test]
    public void AttacksHeavilyArmoredTargetTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 100, initiative: 50, damage: 5);
        UnitDefinition armoredDefinition = TestHelpers.SimpleUnitDefinition("armored", maxHitpoints: 300, initiative: 40, damage: 1) with { Armor = 90 };

        BattleUnit attacker = Unit(0, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit armored = Unit(1, armoredDefinition.Id, BattleSide.Defender, hitpoints: 300);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(armoredDefinition)
            .WithAttacker(attacker, new GridPosition(0, 0))
            .WithDefender(armored, new GridPosition(0, 0))
            .Build();

        engine.CheckCommand(new BattleAiActionCommand(), [
            new BattleUnitAttackEvent(attacker.Id, new BattleAttackUnitTarget(armored.Id), [attackerDefinition.Attack.Id]),
            new BattleUnitAttackedEvent(attacker.Id, armored.Id, attackerDefinition.Attack.Id),
            new BattleUnitAttackHitEvent(attacker.Id, armored.Id, attackerDefinition.Attack.Id, 1),
            new BattleUnitTurnEndingEvent(attacker.Id),
            new BattleUnitTurnEndedEvent(attacker.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(armored.Id, BattleUnitTurnType.Normal))
        ]);
    }

    [Test]
    public void HealPotionTest()
    {
        UnitDefinition leaderDefinition = LeaderDefinition("leader", damage: 10);
        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ally", maxHitpoints: 100, initiative: 30, damage: 10);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 50, initiative: 40, damage: 5);
        ItemDefinition potion = PotionDefinition("potion", ItemCategory.PotionHeal, new HealItemEffect(50));

        BattleUnit leader = Unit(0, leaderDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit healthy = Unit(1, allyDefinition.Id, BattleSide.Attacker, hitpoints: 40);
        BattleUnit wounded = Unit(2, allyDefinition.Id, BattleSide.Attacker, hitpoints: 20);
        BattleUnit defender = Unit(3, defenderDefinition.Id, BattleSide.Defender, hitpoints: 50);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(leaderDefinition)
            .WithDefinition(allyDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(potion)
            .WithAttacker(leader, GridPosition.FrontBottom)
            .WithAttacker(healthy, GridPosition.BackBottom)
            .WithAttacker(wounded, GridPosition.BackCenter)
            .WithDefender(defender, GridPosition.FrontBottom)
            .WithItem(new BattleItem(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, potion.Id, Charges: null, Consumed: false))
            .Build();

        engine.CheckCommand(new BattleAiActionCommand(), [
            new BattleItemUsedEvent(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, potion.Id, leader.Id, new BattleAttackUnitTarget(wounded.Id), RemainingCharges: null, Consumed: true),
            new BattleUnitHealedEvent(wounded.Id, 70),
            new BattleUnitTurnEndingEvent(leader.Id),
            new BattleUnitTurnEndedEvent(leader.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal))
        ]);
    }

    [Test]
    public void HealPotionSummonTest()
    {
        UnitDefinition leaderDefinition = LeaderDefinition("leader", damage: 10);
        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ally", maxHitpoints: 100, initiative: 30, damage: 10);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 50, initiative: 40, damage: 5);
        ItemDefinition potion = PotionDefinition("potion", ItemCategory.PotionHeal, new HealItemEffect(50));

        BattleUnit leader = Unit(0, leaderDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit summon = Unit(1, allyDefinition.Id, BattleSide.Attacker, hitpoints: 10) with { SummonerId = leader.Id };
        BattleUnit wounded = Unit(2, allyDefinition.Id, BattleSide.Attacker, hitpoints: 20);
        BattleUnit defender = Unit(3, defenderDefinition.Id, BattleSide.Defender, hitpoints: 50);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(leaderDefinition)
            .WithDefinition(allyDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(potion)
            .WithAttacker(leader, GridPosition.FrontBottom)
            .WithAttacker(summon, GridPosition.BackBottom)
            .WithAttacker(wounded, GridPosition.BackCenter)
            .WithDefender(defender, GridPosition.FrontBottom)
            .WithItem(new BattleItem(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, potion.Id, Charges: null, Consumed: false))
            .Build();

        engine.CheckCommand(new BattleAiActionCommand(), [
            new BattleItemUsedEvent(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, potion.Id, leader.Id, new BattleAttackUnitTarget(wounded.Id), RemainingCharges: null, Consumed: true),
            new BattleUnitHealedEvent(wounded.Id, 70),
            new BattleUnitTurnEndingEvent(leader.Id),
            new BattleUnitTurnEndedEvent(leader.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal))
        ]);
    }

    [Test]
    public void AttackBeforeHealTest()
    {
        UnitDefinition leaderDefinition = LeaderDefinition("leader", damage: 10);
        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ally", maxHitpoints: 100, initiative: 30, damage: 10);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 50, initiative: 40, damage: 5);
        ItemDefinition potion = PotionDefinition("potion", ItemCategory.PotionHeal, new HealItemEffect(50));

        BattleUnit leader = Unit(0, leaderDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit ally = Unit(1, allyDefinition.Id, BattleSide.Attacker, hitpoints: 95);
        BattleUnit defender = Unit(2, defenderDefinition.Id, BattleSide.Defender, hitpoints: 50);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(leaderDefinition)
            .WithDefinition(allyDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(potion)
            .WithAttacker(leader, GridPosition.FrontBottom)
            .WithAttacker(ally, GridPosition.BackBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .WithItem(new BattleItem(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, potion.Id, Charges: null, Consumed: false))
            .Build();

        engine.CheckCommand(new BattleAiActionCommand(), [
            new BattleUnitAttackEvent(leader.Id, new BattleAttackUnitTarget(defender.Id), [leaderDefinition.Attack.Id]),
            new BattleUnitAttackedEvent(leader.Id, defender.Id, leaderDefinition.Attack.Id),
            new BattleUnitAttackHitEvent(leader.Id, defender.Id, leaderDefinition.Attack.Id, 10),
            new BattleUnitTurnEndingEvent(leader.Id),
            new BattleUnitTurnEndedEvent(leader.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal))
        ]);
    }

    [Test]
    public void AttackBeforeHealTest2()
    {
        UnitDefinition leaderDefinition = LeaderDefinition("leader", damage: 10);
        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ally", maxHitpoints: 100, initiative: 30, damage: 10);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 50, initiative: 40, damage: 5);
        ItemDefinition potion = PotionDefinition("potion", ItemCategory.PotionHeal, new HealItemEffect(50));

        BattleUnit leader = Unit(0, leaderDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit ally = Unit(1, allyDefinition.Id, BattleSide.Attacker, hitpoints: 60);
        BattleUnit defender = Unit(2, defenderDefinition.Id, BattleSide.Defender, hitpoints: 50);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(leaderDefinition)
            .WithDefinition(allyDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(potion)
            .WithAttacker(leader, GridPosition.FrontBottom)
            .WithAttacker(ally, GridPosition.BackBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .WithItem(new BattleItem(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, potion.Id, Charges: null, Consumed: false))
            .Build();

        engine.CheckCommand(new BattleAiActionCommand(), [
            new BattleUnitAttackEvent(leader.Id, new BattleAttackUnitTarget(defender.Id), [leaderDefinition.Attack.Id]),
            new BattleUnitAttackedEvent(leader.Id, defender.Id, leaderDefinition.Attack.Id),
            new BattleUnitAttackHitEvent(leader.Id, defender.Id, leaderDefinition.Attack.Id, 10),
            new BattleUnitTurnEndingEvent(leader.Id),
            new BattleUnitTurnEndedEvent(leader.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal))
        ]);
    }

    [Test]
    public void KillBeforePotionTest()
    {
        UnitDefinition leaderDefinition = LeaderDefinition("leader", damage: 10);
        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ally", maxHitpoints: 100, initiative: 30, damage: 10);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 50, initiative: 40, damage: 5);
        ItemDefinition potion = PotionDefinition("potion", ItemCategory.PotionHeal, new HealItemEffect(50));

        BattleUnit leader = Unit(0, leaderDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit ally = Unit(1, allyDefinition.Id, BattleSide.Attacker, hitpoints: 20);
        BattleUnit defender = Unit(2, defenderDefinition.Id, BattleSide.Defender, hitpoints: 10);
        BattleUnit reserve = Unit(3, defenderDefinition.Id, BattleSide.Defender, hitpoints: 50);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(leaderDefinition)
            .WithDefinition(allyDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(potion)
            .WithAttacker(leader, GridPosition.FrontBottom)
            .WithAttacker(ally, GridPosition.BackBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .WithDefender(reserve, GridPosition.FrontCenter)
            .WithItem(new BattleItem(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, potion.Id, Charges: null, Consumed: false))
            .Build();

        engine.CheckCommand(new BattleAiActionCommand(), [
            new BattleUnitAttackEvent(leader.Id, new BattleAttackUnitTarget(defender.Id), [leaderDefinition.Attack.Id]),
            new BattleUnitAttackedEvent(leader.Id, defender.Id, leaderDefinition.Attack.Id),
            new BattleUnitAttackHitEvent(leader.Id, defender.Id, leaderDefinition.Attack.Id, 10),
            new BattleUnitDiedEvent(defender.Id),
            new BattleUnitTurnEndingEvent(leader.Id),
            new BattleUnitTurnEndedEvent(leader.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(reserve.Id, BattleUnitTurnType.Normal))
        ]);
    }

    [Test]
    public void WardPotionTest()
    {
        UnitDefinition leaderDefinition = LeaderDefinition("leader", damage: 10);
        UnitDefinition dragonDefinition = FireDefinition("dragon", damage: 5);
        UnitModifierDefinition modifier = TestHelpers.SimpleModifier("fire-ward") with
        {
            Wards = [new UnitAttackSourceWard(UnitAttackSource.Fire, UnitWardCategory.Once)]
        };
        ItemDefinition potion = PotionDefinition("potion", ItemCategory.PotionTemporaryBoost, new TemporaryBoostItemEffect(modifier.Id));

        BattleUnit leader = Unit(0, leaderDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit dragon = Unit(1, dragonDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(leaderDefinition)
            .WithDefinition(dragonDefinition)
            .WithDefinition(modifier)
            .WithDefinition(potion)
            .WithAttacker(leader, GridPosition.FrontBottom)
            .WithDefender(dragon, GridPosition.FrontBottom)
            .WithItem(new BattleItem(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, potion.Id, Charges: null, Consumed: false))
            .Build();

        engine.CheckCommand(new BattleAiActionCommand(), [
            new BattleUnitAttackEvent(leader.Id, new BattleAttackUnitTarget(dragon.Id), [leaderDefinition.Attack.Id]),
            new BattleUnitAttackedEvent(leader.Id, dragon.Id, leaderDefinition.Attack.Id),
            new BattleUnitAttackHitEvent(leader.Id, dragon.Id, leaderDefinition.Attack.Id, 10),
            new BattleUnitTurnEndingEvent(leader.Id),
            new BattleUnitTurnEndedEvent(leader.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(dragon.Id, BattleUnitTurnType.Normal))
        ]);
    }
}
