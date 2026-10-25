namespace Gallean.Engine.Tests;

public sealed class BattleEngineAccuracyReductionTests
{
    private static WorldRaceDefinition HumanRace(params UnitDefinitionId[] leaders)
    {
        return new WorldRaceDefinition(
            WorldRace.Human,
            new GlobalTextId("Empire"),
            new WorldResourceSet(100, 0, 0, 25, 0, 0),
            WorldMapTerrain.Human,
            true,
            35,
            null,
            [],
            [.. leaders],
            [],
            [],
            []);
    }

    [Test]
    public void AccuracyReducedPerTargetTest()
    {
        UnitDefinition archmageDefinition = TestHelpers.SimpleUnitDefinition("archmage", maxHitpoints: 30, initiative: 50, damage: 5) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("archmage"), 50, 5, accuracy: 50, reach: UnitAttackReach.All),
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Mage
        };
        UnitDefinition firstDefenderDefinition = TestHelpers.SimpleUnitDefinition("first", maxHitpoints: 10, initiative: 40, damage: 5);
        UnitDefinition secondDefenderDefinition = TestHelpers.SimpleUnitDefinition("second", maxHitpoints: 10, initiative: 30, damage: 5);

        BattleUnit archmage = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, archmageDefinition.Id, BattleSide.Attacker, hitpoints: 30);
        BattleUnit firstDefender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, firstDefenderDefinition.Id, BattleSide.Defender, hitpoints: 10);
        BattleUnit secondDefender = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, secondDefenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(archmageDefinition)
            .WithDefinition(firstDefenderDefinition)
            .WithDefinition(secondDefenderDefinition)
            .WithDefinition(HumanRace(archmageDefinition.Id))
            .WithAttacker(archmage, GridPosition.FrontBottom)
            .WithDefender(firstDefender, GridPosition.FrontBottom)
            .WithDefender(secondDefender, GridPosition.FrontCenter)
            .WithRandom(new TestSequenceRandomProvider(50, 50, 50, 50, 50, 50, 50, 50))
            .Build();

        engine.CheckCommand(new BattleAttackCommand(archmage.Id, new BattleAttackUnitTarget(firstDefender.Id)), [
            new BattleUnitAttackEvent(archmage.Id, new BattleAttackUnitTarget(firstDefender.Id), [new UnitAttackDefinitionId("archmage")]),
            new BattleUnitAttackedEvent(archmage.Id, firstDefender.Id, new UnitAttackDefinitionId("archmage")),
            new BattleUnitAttackHitEvent(archmage.Id, firstDefender.Id, new UnitAttackDefinitionId("archmage"), 5),
            new BattleUnitAttackedEvent(archmage.Id, secondDefender.Id, new UnitAttackDefinitionId("archmage")),
            new BattleUnitAttackMissedEvent(archmage.Id, secondDefender.Id, new UnitAttackDefinitionId("archmage")),
            new BattleUnitTurnEndingEvent(archmage.Id),
            new BattleUnitTurnEndedEvent(archmage.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(firstDefender.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(archmage, GridPosition.FrontBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(firstDefender with { Hitpoints = 5 }, GridPosition.FrontBottom, false)
                    .Place(secondDefender, GridPosition.FrontCenter, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(firstDefender.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(1)
            }));
    }

    [Test]
    public void SoldierAccuracyTest()
    {
        UnitDefinition wizardDefinition = TestHelpers.SimpleUnitDefinition("wizard", maxHitpoints: 30, initiative: 50, damage: 5) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("wizard"), 50, 5, accuracy: 50, reach: UnitAttackReach.All)
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
            .WithDefinition(HumanRace())
            .WithAttacker(wizard, GridPosition.FrontBottom)
            .WithDefender(firstDefender, GridPosition.FrontBottom)
            .WithDefender(secondDefender, GridPosition.FrontCenter)
            .WithRandom(new TestSequenceRandomProvider(50, 50, 50, 50, 50, 50, 50, 50))
            .Build();

        engine.CheckCommand(new BattleAttackCommand(wizard.Id, new BattleAttackUnitTarget(firstDefender.Id)), [
            new BattleUnitAttackEvent(wizard.Id, new BattleAttackUnitTarget(firstDefender.Id), [new UnitAttackDefinitionId("wizard")]),
            new BattleUnitAttackedEvent(wizard.Id, firstDefender.Id, new UnitAttackDefinitionId("wizard")),
            new BattleUnitAttackHitEvent(wizard.Id, firstDefender.Id, new UnitAttackDefinitionId("wizard"), 5),
            new BattleUnitAttackedEvent(wizard.Id, secondDefender.Id, new UnitAttackDefinitionId("wizard")),
            new BattleUnitAttackHitEvent(wizard.Id, secondDefender.Id, new UnitAttackDefinitionId("wizard"), 5),
            new BattleUnitTurnEndingEvent(wizard.Id),
            new BattleUnitTurnEndedEvent(wizard.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(firstDefender.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(wizard, GridPosition.FrontBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(firstDefender with { Hitpoints = 5 }, GridPosition.FrontBottom, false)
                    .Place(secondDefender with { Hitpoints = 5 }, GridPosition.FrontCenter, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(firstDefender.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(1)
            }));
    }

    [Test]
    public void UnhireableLeaderAccuracyTest()
    {
        UnitDefinition dragonDefinition = TestHelpers.SimpleUnitDefinition("dragon", maxHitpoints: 30, initiative: 50, damage: 5) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("dragon"), 50, 5, accuracy: 50, reach: UnitAttackReach.All),
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter
        };
        UnitDefinition archmageDefinition = TestHelpers.SimpleUnitDefinition("archmage", maxHitpoints: 30, initiative: 10, damage: 5) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("archmage"), 10, 5, accuracy: 50, reach: UnitAttackReach.All),
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Mage
        };
        UnitDefinition firstDefenderDefinition = TestHelpers.SimpleUnitDefinition("first", maxHitpoints: 10, initiative: 40, damage: 5);
        UnitDefinition secondDefenderDefinition = TestHelpers.SimpleUnitDefinition("second", maxHitpoints: 10, initiative: 30, damage: 5);

        BattleUnit dragon = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, dragonDefinition.Id, BattleSide.Attacker, hitpoints: 30);
        BattleUnit firstDefender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, firstDefenderDefinition.Id, BattleSide.Defender, hitpoints: 10);
        BattleUnit secondDefender = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, secondDefenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(dragonDefinition)
            .WithDefinition(archmageDefinition)
            .WithDefinition(firstDefenderDefinition)
            .WithDefinition(secondDefenderDefinition)
            .WithDefinition(HumanRace(archmageDefinition.Id))
            .WithAttacker(dragon, GridPosition.FrontBottom)
            .WithDefender(firstDefender, GridPosition.FrontBottom)
            .WithDefender(secondDefender, GridPosition.FrontCenter)
            .WithRandom(new TestSequenceRandomProvider(50, 50, 50, 50, 50, 50, 50, 50))
            .Build();

        engine.CheckCommand(new BattleAttackCommand(dragon.Id, new BattleAttackUnitTarget(firstDefender.Id)), [
            new BattleUnitAttackEvent(dragon.Id, new BattleAttackUnitTarget(firstDefender.Id), [new UnitAttackDefinitionId("dragon")]),
            new BattleUnitAttackedEvent(dragon.Id, firstDefender.Id, new UnitAttackDefinitionId("dragon")),
            new BattleUnitAttackHitEvent(dragon.Id, firstDefender.Id, new UnitAttackDefinitionId("dragon"), 5),
            new BattleUnitAttackedEvent(dragon.Id, secondDefender.Id, new UnitAttackDefinitionId("dragon")),
            new BattleUnitAttackHitEvent(dragon.Id, secondDefender.Id, new UnitAttackDefinitionId("dragon"), 5),
            new BattleUnitTurnEndingEvent(dragon.Id),
            new BattleUnitTurnEndedEvent(dragon.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(firstDefender.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(dragon, GridPosition.FrontBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(firstDefender with { Hitpoints = 5 }, GridPosition.FrontBottom, false)
                    .Place(secondDefender with { Hitpoints = 5 }, GridPosition.FrontCenter, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(firstDefender.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(1)
            }));
    }
}
