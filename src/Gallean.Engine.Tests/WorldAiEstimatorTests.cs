namespace Gallean.Engine.Tests;

public sealed class WorldAiEstimatorTests
{
    private static IEngineContext BuildContext(TestGameDefinitionProviderBuilder definitionsBuilder)
    {
        IGameDefinitionProvider definitions = definitionsBuilder.Build();
        GlobalVariables globals = new();

        return new EngineContext(
            new FixedRandomProvider(),
            definitions,
            new UnitStatsProvider(definitions, globals, GameScenarioProvider.Empty.Definition),
            GameScenarioProvider.Empty,
            globals);
    }

    private static BattleUnit CreateUnit(int id, UnitDefinition definition, BattleSide side, int hitpoints)
    {
        return TestHelpers.SimpleBattleUnit(new BattleUnitId(id), null, definition.Id, side, hitpoints) with { Level = definition.Level };
    }

    [Test]
    public void PowerTest()
    {
        UnitDefinition definition = TestHelpers.SimpleUnitDefinition("unit", maxHitpoints: 100, initiative: 50, damage: 25);
        IEngineContext context = BuildContext(new TestGameDefinitionProviderBuilder().AddUnit(definition));
        BattleStateBuilder battleBuilder = new(context.Definitions);
        battleBuilder.Add(CreateUnit(0, definition, BattleSide.Attacker, 100), GridPosition.FrontTop);
        battleBuilder.Add(CreateUnit(1, definition, BattleSide.Attacker, 25), GridPosition.FrontBottom);
        battleBuilder.Add(CreateUnit(2, definition, BattleSide.Defender, 100), GridPosition.FrontBottom);
        BattleState state = battleBuilder.Build(BattleModifiers.None);
        WorldAiEstimator estimator = new(context);

        Assert.That(estimator.EstimatePower(state), Is.EqualTo(new WorldAiEstimator.PowerEstimate(75, 50)));
    }

    [Test]
    public void DeadUnitTest()
    {
        UnitDefinition definition = TestHelpers.SimpleUnitDefinition("unit", maxHitpoints: 100, initiative: 50, damage: 25);
        IEngineContext context = BuildContext(new TestGameDefinitionProviderBuilder().AddUnit(definition));
        BattleStateBuilder battleBuilder = new(context.Definitions);
        battleBuilder.Add(CreateUnit(0, definition, BattleSide.Attacker, 100), GridPosition.FrontTop);
        battleBuilder.Add(CreateUnit(1, definition, BattleSide.Attacker, 0), GridPosition.FrontBottom);
        BattleState state = battleBuilder.Build(BattleModifiers.None);
        WorldAiEstimator estimator = new(context);

        Assert.That(estimator.EstimatePower(state), Is.EqualTo(new WorldAiEstimator.PowerEstimate(50, 0)));
    }

    [Test]
    public void FortificationArmorTest()
    {
        UnitDefinition definition = TestHelpers.SimpleUnitDefinition("unit", maxHitpoints: 100, initiative: 50, damage: 25);
        IEngineContext context = BuildContext(new TestGameDefinitionProviderBuilder().AddUnit(definition));
        BattleStateBuilder battleBuilder = new(context.Definitions);
        battleBuilder.Add(CreateUnit(0, definition, BattleSide.Attacker, 100), GridPosition.FrontBottom);
        battleBuilder.Add(
            CreateUnit(1, definition, BattleSide.Defender, 100) with { StatusEffects = [new BattleFortificationStatusEffect(50)] },
            GridPosition.FrontBottom);
        BattleState state = battleBuilder.Build(BattleModifiers.None);
        WorldAiEstimator estimator = new(context);

        Assert.That(estimator.EstimatePower(state), Is.EqualTo(new WorldAiEstimator.PowerEstimate(50, 71)));
    }

    [Test]
    public void ArmorCapTest()
    {
        UnitDefinition definition = TestHelpers.SimpleUnitDefinition("unit", maxHitpoints: 100, initiative: 50, damage: 25);
        UnitDefinition armored = definition with { Id = new UnitDefinitionId("armored"), Armor = 100 };
        IEngineContext context = BuildContext(new TestGameDefinitionProviderBuilder().AddUnit(definition).AddUnit(armored));
        BattleStateBuilder battleBuilder = new(context.Definitions);
        battleBuilder.Add(CreateUnit(0, definition, BattleSide.Attacker, 100), GridPosition.FrontBottom);
        battleBuilder.Add(CreateUnit(1, armored, BattleSide.Defender, 100), GridPosition.FrontBottom);
        BattleState state = battleBuilder.Build(BattleModifiers.None);
        WorldAiEstimator estimator = new(context);

        Assert.That(estimator.EstimatePower(state), Is.EqualTo(new WorldAiEstimator.PowerEstimate(50, 158)));
    }

    [Test]
    public void EquipmentModifierTest()
    {
        UnitModifierDefinition weapon = TestHelpers.SimpleModifier("weapon") with
        {
            Damage = new UnitStatModifier(Flat: 75, Percent: 0)
        };
        UnitDefinition definition = TestHelpers.SimpleUnitDefinition("unit", maxHitpoints: 100, initiative: 50, damage: 25);
        IEngineContext context = BuildContext(
            new TestGameDefinitionProviderBuilder().AddUnit(definition).AddUnitModifier(weapon));
        BattleStateBuilder battleBuilder = new(context.Definitions);
        battleBuilder.Add(CreateUnit(0, definition, BattleSide.Attacker, 100), GridPosition.FrontBottom);
        battleBuilder.Add(
            CreateUnit(1, definition, BattleSide.Defender, 100) with { Modifiers = [UnitModifier.Permanent(weapon.Id)] },
            GridPosition.FrontBottom);
        BattleState state = battleBuilder.Build(BattleModifiers.None);
        WorldAiEstimator estimator = new(context);

        Assert.That(estimator.EstimatePower(state), Is.EqualTo(new WorldAiEstimator.PowerEstimate(50, 100)));
    }

    [Test]
    public void AccuracyTest()
    {
        UnitDefinition definition = TestHelpers.SimpleUnitDefinition("unit", maxHitpoints: 100, initiative: 50, damage: 100);
        UnitDefinition inaccurate = definition with
        {
            Id = new UnitDefinitionId("inaccurate"),
            Attack = TestHelpers.SimpleAttack(
                new UnitAttackDefinitionId("inaccurate"),
                initiative: 50,
                damage: 100,
                accuracy: 25,
                reach: UnitAttackReach.Any)
        };
        IEngineContext context = BuildContext(new TestGameDefinitionProviderBuilder().AddUnit(definition).AddUnit(inaccurate));
        BattleStateBuilder battleBuilder = new(context.Definitions);
        battleBuilder.Add(CreateUnit(0, definition, BattleSide.Attacker, 100), GridPosition.FrontBottom);
        battleBuilder.Add(CreateUnit(1, inaccurate, BattleSide.Defender, 100), GridPosition.FrontBottom);
        BattleState state = battleBuilder.Build(BattleModifiers.None);
        WorldAiEstimator estimator = new(context);

        Assert.That(estimator.EstimatePower(state), Is.EqualTo(new WorldAiEstimator.PowerEstimate(100, 50)));
    }

    [Test]
    public void AttacksTwiceTest()
    {
        UnitDefinition definition = TestHelpers.SimpleUnitDefinition("unit", maxHitpoints: 100, initiative: 50, damage: 25);
        UnitDefinition doubleAttacker = definition with { Id = new UnitDefinitionId("double"), AttacksTwice = true };
        IEngineContext context = BuildContext(new TestGameDefinitionProviderBuilder().AddUnit(definition).AddUnit(doubleAttacker));
        BattleStateBuilder battleBuilder = new(context.Definitions);
        battleBuilder.Add(CreateUnit(0, definition, BattleSide.Attacker, 100), GridPosition.FrontBottom);
        battleBuilder.Add(CreateUnit(1, doubleAttacker, BattleSide.Defender, 100), GridPosition.FrontBottom);
        BattleState state = battleBuilder.Build(BattleModifiers.None);
        WorldAiEstimator estimator = new(context);

        Assert.That(estimator.EstimatePower(state), Is.EqualTo(new WorldAiEstimator.PowerEstimate(50, 71)));
    }

    [Test]
    public void AreaAttackTest()
    {
        UnitDefinition definition = TestHelpers.SimpleUnitDefinition("unit", maxHitpoints: 100, initiative: 50, damage: 25);
        UnitDefinition area = definition with
        {
            Id = new UnitDefinitionId("area"),
            Attack = TestHelpers.SimpleAttack(
                new UnitAttackDefinitionId("area"),
                initiative: 50,
                damage: 25,
                accuracy: 100,
                reach: UnitAttackReach.All)
        };
        IEngineContext context = BuildContext(new TestGameDefinitionProviderBuilder().AddUnit(definition).AddUnit(area));
        BattleStateBuilder battleBuilder = new(context.Definitions);
        battleBuilder.Add(CreateUnit(0, definition, BattleSide.Attacker, 100), GridPosition.FrontTop);
        battleBuilder.Add(CreateUnit(1, definition, BattleSide.Attacker, 100), GridPosition.FrontCenter);
        battleBuilder.Add(CreateUnit(2, definition, BattleSide.Attacker, 100), GridPosition.FrontBottom);
        battleBuilder.Add(CreateUnit(3, area, BattleSide.Defender, 100), GridPosition.FrontBottom);
        BattleState state = battleBuilder.Build(BattleModifiers.None);
        WorldAiEstimator estimator = new(context);

        Assert.That(estimator.EstimatePower(state), Is.EqualTo(new WorldAiEstimator.PowerEstimate(150, 67)));
    }

    [Test]
    public void ZeroImpactTest()
    {
        UnitDefinition definition = TestHelpers.SimpleUnitDefinition("unit", maxHitpoints: 100, initiative: 50, damage: 25);
        UnitDefinition harmless = definition with
        {
            Id = new UnitDefinitionId("harmless"),
            Attack = TestHelpers.SimpleAttack(
                new UnitAttackDefinitionId("harmless"),
                initiative: 50,
                damage: 0,
                accuracy: 0,
                reach: UnitAttackReach.Any)
        };
        IEngineContext context = BuildContext(new TestGameDefinitionProviderBuilder().AddUnit(definition).AddUnit(harmless));
        BattleStateBuilder battleBuilder = new(context.Definitions);
        battleBuilder.Add(CreateUnit(0, definition, BattleSide.Attacker, 100), GridPosition.FrontBottom);
        battleBuilder.Add(CreateUnit(1, harmless, BattleSide.Defender, 100), GridPosition.FrontBottom);
        BattleState state = battleBuilder.Build(BattleModifiers.None);
        WorldAiEstimator estimator = new(context);

        Assert.That(estimator.EstimatePower(state), Is.EqualTo(new WorldAiEstimator.PowerEstimate(50, 10)));
    }

    [Test]
    public void CorpseExperienceTest()
    {
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 50),
            movementPoints: 6, leadership: 6);
        UnitDefinition weak = TestHelpers.SimpleUnitDefinition("weak", maxHitpoints: 10, initiative: 40, damage: 5);
        IEngineContext context = BuildContext(new TestGameDefinitionProviderBuilder().AddUnit(leader).AddUnit(weak));
        BattleStateBuilder battleBuilder = new(context.Definitions);
        battleBuilder.Add(CreateUnit(0, leader, BattleSide.Attacker, 100), GridPosition.FrontBottom);
        battleBuilder.Add(TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, weak.Id, BattleSide.Attacker, hitpoints: 0) with { Experience = 300 }, GridPosition.FrontTop);
        battleBuilder.Add(CreateUnit(2, weak, BattleSide.Defender, 10), GridPosition.FrontBottom);
        BattleState state = battleBuilder.Build(BattleModifiers.None);
        WorldAiEstimator estimator = new(context);

        Assert.That(estimator.EstimateBattle(state), Is.EqualTo(new WorldAiEstimator.BattleEstimate(
            new BattleOutcome(BattleSide.Attacker),
            UnitCountBefore: 1,
            UnitCountAfter: 1,
            HitpointsBefore: 100,
            HitpointsAfter: 100,
            ExperienceBefore: 0,
            ExperienceLost: 0,
            ExperienceGained: 1,
            IsLeaderAlive: true)));
    }

    [Test]
    public void LeaderWithoutCategoryTest()
    {
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("summon", maxHitpoints: 100, initiative: 50, damage: 50),
                movementPoints: 6, leadership: 6) with
            {
                LeaderCategory = null
            };
        UnitDefinition weak = TestHelpers.SimpleUnitDefinition("weak", maxHitpoints: 10, initiative: 40, damage: 5);
        IEngineContext context = BuildContext(new TestGameDefinitionProviderBuilder().AddUnit(leader).AddUnit(weak));
        BattleStateBuilder battleBuilder = new(context.Definitions);
        battleBuilder.Add(CreateUnit(0, leader, BattleSide.Attacker, 100), GridPosition.FrontBottom);
        battleBuilder.Add(CreateUnit(1, weak, BattleSide.Defender, 10), GridPosition.FrontBottom);
        BattleState state = battleBuilder.Build(BattleModifiers.None);
        WorldAiEstimator estimator = new(context);

        Assert.That(estimator.EstimateBattle(state), Is.EqualTo(new WorldAiEstimator.BattleEstimate(
            new BattleOutcome(BattleSide.Attacker),
            UnitCountBefore: 1,
            UnitCountAfter: 1,
            HitpointsBefore: 100,
            HitpointsAfter: 100,
            ExperienceBefore: 0,
            ExperienceLost: 0,
            ExperienceGained: 1,
            IsLeaderAlive: true)));
    }
}
