namespace Gallean.Engine.Tests;

public sealed class BattleEngineGetAttackInfoTests
{
    [Test]
    public void AllReachTest()
    {
        UnitDefinition wizardDefinition = TestHelpers.SimpleUnitDefinition("wizard", maxHitpoints: 30, initiative: 60, damage: 5) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("wizard"), 60, 5, accuracy: 100, reach: UnitAttackReach.All)
        };
        UnitDefinition firstDefenderDefinition = TestHelpers.SimpleUnitDefinition("first", maxHitpoints: 10, initiative: 40, damage: 5);
        UnitDefinition secondDefenderDefinition = TestHelpers.SimpleUnitDefinition("second", maxHitpoints: 10, initiative: 30, damage: 5);

        BattleUnit wizard = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, wizardDefinition.Id, BattleSide.Attacker, hitpoints: 30);
        BattleUnit firstDefender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, firstDefenderDefinition.Id, BattleSide.Defender, hitpoints: 10);
        BattleUnit secondDefender = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, secondDefenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(wizardDefinition)
            .WithDefinition(firstDefenderDefinition)
            .WithDefinition(secondDefenderDefinition)
            .WithAttacker(wizard, GridPosition.FrontBottom)
            .WithDefender(firstDefender, GridPosition.FrontBottom)
            .WithDefender(secondDefender, GridPosition.FrontCenter)
            .Build();

        BattleGetAttackInfoCommand.Result result = engine.CheckCommand(new BattleGetAttackInfoCommand(wizard.Id), []);

        Assert.That(result, Is.EqualTo(new BattleGetAttackInfoCommand.Result(
        [
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackUnitTarget(wizard.Id), [], BattleCommandError.TargetIncorrect),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackUnitTarget(firstDefender.Id), [new BattleAttackUnitTarget(firstDefender.Id), new BattleAttackUnitTarget(secondDefender.Id)], null),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackUnitTarget(secondDefender.Id), [new BattleAttackUnitTarget(firstDefender.Id), new BattleAttackUnitTarget(secondDefender.Id)], null),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.FrontCenter), [], BattleCommandError.AttackRequiresUnitTarget),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.FrontTop), [], BattleCommandError.AttackRequiresUnitTarget),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.BackBottom), [], BattleCommandError.AttackRequiresUnitTarget),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.BackCenter), [], BattleCommandError.AttackRequiresUnitTarget),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.BackTop), [], BattleCommandError.AttackRequiresUnitTarget),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackTileTarget(BattleSide.Defender, GridPosition.FrontTop), [], BattleCommandError.AttackRequiresUnitTarget),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackTileTarget(BattleSide.Defender, GridPosition.BackBottom), [], BattleCommandError.AttackRequiresUnitTarget),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackTileTarget(BattleSide.Defender, GridPosition.BackCenter), [], BattleCommandError.AttackRequiresUnitTarget),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackTileTarget(BattleSide.Defender, GridPosition.BackTop), [], BattleCommandError.AttackRequiresUnitTarget)
        ])));
    }

    [Test]
    public void HealAllReachTest()
    {
        UnitDefinition clericDefinition = TestHelpers.SimpleUnitDefinition("cleric", maxHitpoints: 100, initiative: 60, damage: 0) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("cleric"), 60, 0, accuracy: 100, reach: UnitAttackReach.All) with
            {
                Type = UnitAttackType.Heal,
                Heal = 30
            }
        };
        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ally", maxHitpoints: 100, initiative: 20, damage: 5);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 100, initiative: 40, damage: 5);

        BattleUnit cleric = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, clericDefinition.Id, BattleSide.Attacker, hitpoints: 70);
        BattleUnit ally = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, allyDefinition.Id, BattleSide.Attacker, hitpoints: 50);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(clericDefinition)
            .WithDefinition(allyDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(cleric, GridPosition.FrontBottom)
            .WithAttacker(ally, GridPosition.BackBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        BattleGetAttackInfoCommand.Result result = engine.CheckCommand(new BattleGetAttackInfoCommand(cleric.Id), []);

        Assert.That(result, Is.EqualTo(new BattleGetAttackInfoCommand.Result(
        [
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackUnitTarget(cleric.Id), [new BattleAttackUnitTarget(cleric.Id), new BattleAttackUnitTarget(ally.Id)], null),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackUnitTarget(ally.Id), [new BattleAttackUnitTarget(cleric.Id), new BattleAttackUnitTarget(ally.Id)], null),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackUnitTarget(defender.Id), [], BattleCommandError.TargetIncorrect),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.FrontCenter), [], BattleCommandError.AttackRequiresUnitTarget),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.FrontTop), [], BattleCommandError.AttackRequiresUnitTarget),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.BackCenter), [], BattleCommandError.AttackRequiresUnitTarget),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.BackTop), [], BattleCommandError.AttackRequiresUnitTarget),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackTileTarget(BattleSide.Defender, GridPosition.FrontCenter), [], BattleCommandError.AttackRequiresUnitTarget),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackTileTarget(BattleSide.Defender, GridPosition.FrontTop), [], BattleCommandError.AttackRequiresUnitTarget),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackTileTarget(BattleSide.Defender, GridPosition.BackBottom), [], BattleCommandError.AttackRequiresUnitTarget),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackTileTarget(BattleSide.Defender, GridPosition.BackCenter), [], BattleCommandError.AttackRequiresUnitTarget),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackTileTarget(BattleSide.Defender, GridPosition.BackTop), [], BattleCommandError.AttackRequiresUnitTarget)
        ])));
    }

    [Test]
    public void SingleReachTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 30, initiative: 60, damage: 5) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("attacker"), 60, 5, accuracy: 100, reach: UnitAttackReach.Any)
        };
        UnitDefinition firstDefenderDefinition = TestHelpers.SimpleUnitDefinition("first", maxHitpoints: 10, initiative: 40, damage: 5);
        UnitDefinition secondDefenderDefinition = TestHelpers.SimpleUnitDefinition("second", maxHitpoints: 10, initiative: 30, damage: 5);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 30);
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

        BattleGetAttackInfoCommand.Result result = engine.CheckCommand(new BattleGetAttackInfoCommand(attacker.Id), []);

        Assert.That(result, Is.EqualTo(new BattleGetAttackInfoCommand.Result(
        [
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackUnitTarget(attacker.Id), [], BattleCommandError.TargetIncorrect),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackUnitTarget(firstDefender.Id), [new BattleAttackUnitTarget(firstDefender.Id)], null),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackUnitTarget(secondDefender.Id), [new BattleAttackUnitTarget(secondDefender.Id)], null),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.FrontCenter), [], BattleCommandError.AttackRequiresUnitTarget),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.FrontTop), [], BattleCommandError.AttackRequiresUnitTarget),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.BackBottom), [], BattleCommandError.AttackRequiresUnitTarget),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.BackCenter), [], BattleCommandError.AttackRequiresUnitTarget),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.BackTop), [], BattleCommandError.AttackRequiresUnitTarget),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackTileTarget(BattleSide.Defender, GridPosition.FrontTop), [], BattleCommandError.AttackRequiresUnitTarget),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackTileTarget(BattleSide.Defender, GridPosition.BackBottom), [], BattleCommandError.AttackRequiresUnitTarget),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackTileTarget(BattleSide.Defender, GridPosition.BackCenter), [], BattleCommandError.AttackRequiresUnitTarget),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackTileTarget(BattleSide.Defender, GridPosition.BackTop), [], BattleCommandError.AttackRequiresUnitTarget)
        ])));
    }

    [Test]
    public void SummonTileTest()
    {
        UnitDefinition impDefinition = TestHelpers.SimpleUnitDefinition("imp", maxHitpoints: 20, initiative: 35, damage: 8);
        UnitDefinition summonerDefinition = TestHelpers.SimpleUnitDefinition("summoner", maxHitpoints: 30, initiative: 50, damage: 5) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("summoner"), initiative: 50, damage: 5, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Type = UnitAttackType.Summon,
                SummonUnitIds = [impDefinition.Id]
            }
        };
        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ally", maxHitpoints: 40, initiative: 40, damage: 5);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 30, damage: 5);

        BattleUnit summoner = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, summonerDefinition.Id, BattleSide.Attacker, hitpoints: 30);
        BattleUnit corpse = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, allyDefinition.Id, BattleSide.Attacker, hitpoints: 0);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(summonerDefinition)
            .WithDefinition(impDefinition)
            .WithDefinition(allyDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(summoner, GridPosition.FrontBottom)
            .WithAttacker(corpse, GridPosition.FrontCenter)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        BattleGetAttackInfoCommand.Result result = engine.CheckCommand(new BattleGetAttackInfoCommand(summoner.Id), []);

        Assert.That(result, Is.EqualTo(new BattleGetAttackInfoCommand.Result(
        [
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackUnitTarget(summoner.Id), [], BattleCommandError.SummonTargetOccupied),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackUnitTarget(corpse.Id), [new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.FrontCenter)], null),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackUnitTarget(defender.Id), [], BattleCommandError.SummonTargetInvalid),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.FrontTop), [new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.FrontTop)], null),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.BackBottom), [new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.BackBottom)], null),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.BackCenter), [new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.BackCenter)], null),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.BackTop), [new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.BackTop)], null),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackTileTarget(BattleSide.Defender, GridPosition.FrontCenter), [], BattleCommandError.SummonTargetInvalid),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackTileTarget(BattleSide.Defender, GridPosition.FrontTop), [], BattleCommandError.SummonTargetInvalid),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackTileTarget(BattleSide.Defender, GridPosition.BackBottom), [], BattleCommandError.SummonTargetInvalid),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackTileTarget(BattleSide.Defender, GridPosition.BackCenter), [], BattleCommandError.SummonTargetInvalid),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackTileTarget(BattleSide.Defender, GridPosition.BackTop), [], BattleCommandError.SummonTargetInvalid)
        ])));
    }

    [Test]
    public void LargeTargetTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 30, initiative: 60, damage: 5) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("attacker"), 60, 5, accuracy: 100, reach: UnitAttackReach.Any)
        };
        UnitDefinition giantDefinition = TestHelpers.SimpleUnitDefinition("giant", maxHitpoints: 300, initiative: 30, damage: 40) with
        {
            IsLarge = true
        };

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 30);
        BattleUnit giant = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, giantDefinition.Id, BattleSide.Defender, hitpoints: 300);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(giantDefinition)
            .WithAttacker(attacker, GridPosition.FrontBottom)
            .WithDefender(giant, GridPosition.FrontTop)
            .Build();

        BattleGetAttackInfoCommand.Result result = engine.CheckCommand(new BattleGetAttackInfoCommand(attacker.Id), []);

        Assert.That(result, Is.EqualTo(new BattleGetAttackInfoCommand.Result(
        [
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackUnitTarget(attacker.Id), [], BattleCommandError.TargetIncorrect),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackUnitTarget(giant.Id), [new BattleAttackUnitTarget(giant.Id)], null),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.FrontCenter), [], BattleCommandError.AttackRequiresUnitTarget),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.FrontTop), [], BattleCommandError.AttackRequiresUnitTarget),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.BackBottom), [], BattleCommandError.AttackRequiresUnitTarget),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.BackCenter), [], BattleCommandError.AttackRequiresUnitTarget),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.BackTop), [], BattleCommandError.AttackRequiresUnitTarget),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackTileTarget(BattleSide.Defender, GridPosition.FrontBottom), [], BattleCommandError.AttackRequiresUnitTarget),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackTileTarget(BattleSide.Defender, GridPosition.FrontCenter), [], BattleCommandError.AttackRequiresUnitTarget),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackTileTarget(BattleSide.Defender, GridPosition.BackBottom), [], BattleCommandError.AttackRequiresUnitTarget),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackTileTarget(BattleSide.Defender, GridPosition.BackCenter), [], BattleCommandError.AttackRequiresUnitTarget)
        ])));
    }

    [Test]
    public void NotUnitTurnTest()
    {
        UnitDefinition wizardDefinition = TestHelpers.SimpleUnitDefinition("wizard", maxHitpoints: 30, initiative: 60, damage: 5) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("wizard"), 60, 5, accuracy: 100, reach: UnitAttackReach.All)
        };
        UnitDefinition firstDefenderDefinition = TestHelpers.SimpleUnitDefinition("first", maxHitpoints: 10, initiative: 40, damage: 5);
        UnitDefinition secondDefenderDefinition = TestHelpers.SimpleUnitDefinition("second", maxHitpoints: 10, initiative: 30, damage: 5);

        BattleUnit wizard = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, wizardDefinition.Id, BattleSide.Attacker, hitpoints: 30);
        BattleUnit firstDefender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, firstDefenderDefinition.Id, BattleSide.Defender, hitpoints: 10);
        BattleUnit secondDefender = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, secondDefenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(wizardDefinition)
            .WithDefinition(firstDefenderDefinition)
            .WithDefinition(secondDefenderDefinition)
            .WithAttacker(wizard, GridPosition.FrontBottom)
            .WithDefender(firstDefender, GridPosition.FrontBottom)
            .WithDefender(secondDefender, GridPosition.FrontCenter)
            .Build();

        engine.CheckCommandError(new BattleGetAttackInfoCommand(firstDefender.Id), BattleCommandError.NotUnitTurn);
    }
}
