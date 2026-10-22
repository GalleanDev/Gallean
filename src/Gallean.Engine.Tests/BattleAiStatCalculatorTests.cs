using System.Collections.Immutable;

namespace Gallean.Engine.Tests;

public sealed class BattleAiStatCalculatorTests
{
    private static IEngineContext BuildContext(UnitDefinition definition)
    {
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder().AddUnit(definition).Build();
        GlobalVariables globals = new();

        return new EngineContext(
            new FixedRandomProvider(),
            definitions,
            new UnitStatsProvider(definitions, globals, GameScenarioProvider.Empty.Definition),
            GameScenarioProvider.Empty,
            globals);
    }

    private static BattleState BuildState(IEngineContext context, BattleUnit unit)
    {
        return new BattleStateBuilder(context.Definitions).Add(unit, GridPosition.FrontBottom).Build(BattleModifiers.None);
    }

    private static BattleUnit CreateUnit(UnitDefinition definition, int hitpoints, ImmutableArray<BattleAbstractStatusEffect> statusEffects)
    {
        return TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, definition.Id, BattleSide.Defender, hitpoints) with
        {
            Level = definition.Level,
            StatusEffects = statusEffects
        };
    }

    [Test]
    public void PlainUnitTest()
    {
        UnitDefinition definition = TestHelpers.SimpleUnitDefinition("unit", maxHitpoints: 100, initiative: 50, damage: 25) with { Armor = 20 };
        IEngineContext context = BuildContext(definition);
        BattleUnit unit = CreateUnit(definition, hitpoints: 100, []);

        Assert.That(BattleAiStatCalculator.GetEffectiveHitpoints(context, BuildState(context, unit), unit), Is.EqualTo(125));
    }

    [Test]
    public void NoArmorTest()
    {
        UnitDefinition definition = TestHelpers.SimpleUnitDefinition("unit", maxHitpoints: 100, initiative: 50, damage: 25);
        IEngineContext context = BuildContext(definition);
        BattleUnit unit = CreateUnit(definition, hitpoints: 60, []);

        Assert.That(BattleAiStatCalculator.GetEffectiveHitpoints(context, BuildState(context, unit), unit), Is.EqualTo(60));
    }

    [Test]
    public void ShatteredArmorTest()
    {
        UnitDefinition definition = TestHelpers.SimpleUnitDefinition("unit", maxHitpoints: 100, initiative: 50, damage: 25) with { Armor = 50 };
        IEngineContext context = BuildContext(definition);
        BattleUnit unit = CreateUnit(definition, hitpoints: 80, [new BattleShatterArmorStatusEffect(30)]);

        Assert.That(BattleAiStatCalculator.GetEffectiveHitpoints(context, BuildState(context, unit), unit), Is.EqualTo(100));
    }

    [Test]
    public void ShatteredArmorMultipleTest()
    {
        UnitDefinition definition = TestHelpers.SimpleUnitDefinition("unit", maxHitpoints: 100, initiative: 50, damage: 25) with { Armor = 50 };
        IEngineContext context = BuildContext(definition);
        BattleUnit unit = CreateUnit(definition, hitpoints: 80, [new BattleShatterArmorStatusEffect(30), new BattleShatterArmorStatusEffect(10)]);

        Assert.That(BattleAiStatCalculator.GetEffectiveHitpoints(context, BuildState(context, unit), unit), Is.EqualTo(88));
    }

    [Test]
    public void FortificationArmorLowArmorTest()
    {
        UnitDefinition definition = TestHelpers.SimpleUnitDefinition("unit", maxHitpoints: 100, initiative: 50, damage: 25) with { Armor = 10 };
        IEngineContext context = BuildContext(definition);
        BattleUnit unit = CreateUnit(definition, hitpoints: 100, [new BattleFortificationStatusEffect(30)]);

        Assert.That(BattleAiStatCalculator.GetEffectiveHitpoints(context, BuildState(context, unit), unit), Is.EqualTo(142));
    }

    [Test]
    public void FortificationArmorHighArmorTest()
    {
        UnitDefinition definition = TestHelpers.SimpleUnitDefinition("unit", maxHitpoints: 100, initiative: 50, damage: 25) with { Armor = 45 };
        IEngineContext context = BuildContext(definition);
        BattleUnit unit = CreateUnit(definition, hitpoints: 100, [new BattleFortificationStatusEffect(30)]);

        Assert.That(BattleAiStatCalculator.GetEffectiveHitpoints(context, BuildState(context, unit), unit), Is.EqualTo(181));
    }

    [Test]
    public void FortificationArmorShatterTest()
    {
        UnitDefinition definition = TestHelpers.SimpleUnitDefinition("unit", maxHitpoints: 100, initiative: 50, damage: 25) with { Armor = 45 };
        IEngineContext context = BuildContext(definition);
        BattleUnit unit = CreateUnit(definition, hitpoints: 100, [new BattleFortificationStatusEffect(30), new BattleShatterArmorStatusEffect(45)]);

        Assert.That(BattleAiStatCalculator.GetEffectiveHitpoints(context, BuildState(context, unit), unit), Is.EqualTo(142));
    }

    [Test]
    public void DefendTest()
    {
        UnitDefinition definition = TestHelpers.SimpleUnitDefinition("unit", maxHitpoints: 100, initiative: 50, damage: 25) with { Armor = 20 };
        IEngineContext context = BuildContext(definition);
        BattleUnit unit = CreateUnit(definition, hitpoints: 100, [new BattleDefendStatusEffect()]);

        Assert.That(BattleAiStatCalculator.GetEffectiveHitpoints(context, BuildState(context, unit), unit), Is.EqualTo(250));
    }

    [Test]
    public void DefendTruncatesTest()
    {
        UnitDefinition definition = TestHelpers.SimpleUnitDefinition("unit", maxHitpoints: 100, initiative: 50, damage: 25) with { Armor = 31 };
        IEngineContext context = BuildContext(definition);
        BattleUnit unit = CreateUnit(definition, hitpoints: 100, [new BattleDefendStatusEffect()]);

        Assert.That(BattleAiStatCalculator.GetEffectiveHitpoints(context, BuildState(context, unit), unit), Is.EqualTo(285));
    }

    [Test]
    public void DefendWithoutArmorTest()
    {
        UnitDefinition definition = TestHelpers.SimpleUnitDefinition("unit", maxHitpoints: 100, initiative: 50, damage: 25);
        IEngineContext context = BuildContext(definition);
        BattleUnit unit = CreateUnit(definition, hitpoints: 100, [new BattleDefendStatusEffect()]);

        Assert.That(BattleAiStatCalculator.GetEffectiveHitpoints(context, BuildState(context, unit), unit), Is.EqualTo(200));
    }

    [Test]
    public void DefendAfterFortificationTest()
    {
        UnitDefinition definition = TestHelpers.SimpleUnitDefinition("unit", maxHitpoints: 100, initiative: 50, damage: 25) with { Armor = 10 };
        IEngineContext context = BuildContext(definition);
        BattleUnit unit = CreateUnit(definition, hitpoints: 100, [new BattleFortificationStatusEffect(50), new BattleDefendStatusEffect()]);

        Assert.That(BattleAiStatCalculator.GetEffectiveHitpoints(context, BuildState(context, unit), unit), Is.EqualTo(400));
    }

    [Test]
    public void RoundingTest()
    {
        UnitDefinition definition = TestHelpers.SimpleUnitDefinition("unit", maxHitpoints: 100, initiative: 50, damage: 25) with { Armor = 7 };
        IEngineContext context = BuildContext(definition);

        BattleUnit unit0 = CreateUnit(definition, hitpoints: 0, []);
        BattleUnit unit13 = CreateUnit(definition, hitpoints: 13, []);
        BattleUnit unit14 = CreateUnit(definition, hitpoints: 14, []);
        BattleUnit unit15 = CreateUnit(definition, hitpoints: 15, []);

        Assert.That(BattleAiStatCalculator.GetEffectiveHitpoints(context, BuildState(context, unit0), unit0), Is.Zero);
        Assert.That(BattleAiStatCalculator.GetEffectiveHitpoints(context, BuildState(context, unit13), unit13), Is.EqualTo(13));
        Assert.That(BattleAiStatCalculator.GetEffectiveHitpoints(context, BuildState(context, unit14), unit14), Is.EqualTo(15));
        Assert.That(BattleAiStatCalculator.GetEffectiveHitpoints(context, BuildState(context, unit15), unit15), Is.EqualTo(16));
    }
}
