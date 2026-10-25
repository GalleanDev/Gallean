namespace Gallean.Engine.Tests;

public sealed class UnitStatsProviderTests
{
    [Test]
    public void ArmorTest()
    {
        UnitDefinition definition = TestHelpers.SimpleUnitDefinition("unit", maxHitpoints: 100, initiative: 50, damage: 25) with { Armor = 20 };
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder().AddUnit(definition).Build();
        GlobalVariables globals = new();

        BattleUnit unit = new(new BattleUnitId(0), null, definition.Id, BattleSide.Defender, 100, 0, 0, 1, [], [], null, [], null, false);
        BattleState state = new BattleStateBuilder(definitions).Add(unit, GridPosition.FrontBottom).Build(BattleModifiers.None);

        UnitStats stats = new UnitStatsProvider(definitions, globals, GameScenarioProvider.Empty.Definition).GetBattleUnitStats(state, unit.Id);

        Assert.That(stats, Is.EqualTo(new UnitStats(MaxHitpoints: 100, Armor: 20, Regeneration: 5, Initiative: 50, Damage: 25, Accuracy: 100, ExperienceKilled: 0, ExperienceNextLevel: 1000)));
    }

    [Test]
    public void NoArmorTest()
    {
        UnitDefinition definition = TestHelpers.SimpleUnitDefinition("unit", maxHitpoints: 100, initiative: 50, damage: 25);
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder().AddUnit(definition).Build();
        GlobalVariables globals = new();

        BattleUnit unit = new(new BattleUnitId(0), null, definition.Id, BattleSide.Defender, 100, 0, 0, 1, [], [], null, [], null, false);
        BattleState state = new BattleStateBuilder(definitions).Add(unit, GridPosition.FrontBottom).Build(BattleModifiers.None);

        UnitStats stats = new UnitStatsProvider(definitions, globals, GameScenarioProvider.Empty.Definition).GetBattleUnitStats(state, unit.Id);

        Assert.That(stats, Is.EqualTo(new UnitStats(MaxHitpoints: 100, Armor: 0, Regeneration: 5, Initiative: 50, Damage: 25, Accuracy: 100, ExperienceKilled: 0, ExperienceNextLevel: 1000)));
    }

    [Test]
    public void ShatteredArmorTest()
    {
        UnitDefinition definition = TestHelpers.SimpleUnitDefinition("unit", maxHitpoints: 100, initiative: 50, damage: 25) with { Armor = 50 };
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder().AddUnit(definition).Build();
        GlobalVariables globals = new();

        BattleUnit unit = new(new BattleUnitId(0), null, definition.Id, BattleSide.Defender, 100, 0, 0, 1, [], [new BattleShatterArmorStatusEffect(30)], null, [], null, false);
        BattleState state = new BattleStateBuilder(definitions).Add(unit, GridPosition.FrontBottom).Build(BattleModifiers.None);

        UnitStats stats = new UnitStatsProvider(definitions, globals, GameScenarioProvider.Empty.Definition).GetBattleUnitStats(state, unit.Id);

        Assert.That(stats, Is.EqualTo(new UnitStats(MaxHitpoints: 100, Armor: 20, Regeneration: 5, Initiative: 50, Damage: 25, Accuracy: 100, ExperienceKilled: 0, ExperienceNextLevel: 1000)));
    }

    [Test]
    public void ShatteredArmorMultipleTest()
    {
        UnitDefinition definition = TestHelpers.SimpleUnitDefinition("unit", maxHitpoints: 100, initiative: 50, damage: 25) with { Armor = 50 };
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder().AddUnit(definition).Build();
        GlobalVariables globals = new();

        BattleUnit unit = new(new BattleUnitId(0), null, definition.Id, BattleSide.Defender, 100, 0, 0, 1, [], [new BattleShatterArmorStatusEffect(30), new BattleShatterArmorStatusEffect(10)], null, [], null, false);
        BattleState state = new BattleStateBuilder(definitions).Add(unit, GridPosition.FrontBottom).Build(BattleModifiers.None);

        UnitStats stats = new UnitStatsProvider(definitions, globals, GameScenarioProvider.Empty.Definition).GetBattleUnitStats(state, unit.Id);

        Assert.That(stats, Is.EqualTo(new UnitStats(MaxHitpoints: 100, Armor: 10, Regeneration: 5, Initiative: 50, Damage: 25, Accuracy: 100, ExperienceKilled: 0, ExperienceNextLevel: 1000)));
    }

    [Test]
    public void FortificationLowArmorTest()
    {
        UnitDefinition definition = TestHelpers.SimpleUnitDefinition("unit", maxHitpoints: 100, initiative: 50, damage: 25) with { Armor = 10 };
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder().AddUnit(definition).Build();
        GlobalVariables globals = new();

        BattleUnit unit = new(new BattleUnitId(0), null, definition.Id, BattleSide.Defender, 100, 0, 0, 1, [], [new BattleFortificationStatusEffect(30)], null, [], null, false);
        BattleState state = new BattleStateBuilder(definitions).Add(unit, GridPosition.FrontBottom).Build(BattleModifiers.None);

        UnitStats stats = new UnitStatsProvider(definitions, globals, GameScenarioProvider.Empty.Definition).GetBattleUnitStats(state, unit.Id);

        Assert.That(stats, Is.EqualTo(new UnitStats(MaxHitpoints: 100, Armor: 30, Regeneration: 5, Initiative: 50, Damage: 25, Accuracy: 100, ExperienceKilled: 0, ExperienceNextLevel: 1000)));
    }

    [Test]
    public void FortificationHighArmorTest()
    {
        UnitDefinition definition = TestHelpers.SimpleUnitDefinition("unit", maxHitpoints: 100, initiative: 50, damage: 25) with { Armor = 45 };
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder().AddUnit(definition).Build();
        GlobalVariables globals = new();

        BattleUnit unit = new(new BattleUnitId(0), null, definition.Id, BattleSide.Defender, 100, 0, 0, 1, [], [new BattleFortificationStatusEffect(30)], null, [], null, false);
        BattleState state = new BattleStateBuilder(definitions).Add(unit, GridPosition.FrontBottom).Build(BattleModifiers.None);

        UnitStats stats = new UnitStatsProvider(definitions, globals, GameScenarioProvider.Empty.Definition).GetBattleUnitStats(state, unit.Id);

        Assert.That(stats, Is.EqualTo(new UnitStats(MaxHitpoints: 100, Armor: 45, Regeneration: 5, Initiative: 50, Damage: 25, Accuracy: 100, ExperienceKilled: 0, ExperienceNextLevel: 1000)));
    }

    [Test]
    public void FortificationShatterTest()
    {
        UnitDefinition definition = TestHelpers.SimpleUnitDefinition("unit", maxHitpoints: 100, initiative: 50, damage: 25) with { Armor = 45 };
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder().AddUnit(definition).Build();
        GlobalVariables globals = new();

        BattleUnit unit = new(new BattleUnitId(0), null, definition.Id, BattleSide.Defender, 100, 0, 0, 1, [], [new BattleFortificationStatusEffect(30), new BattleShatterArmorStatusEffect(45)], null, [], null, false);
        BattleState state = new BattleStateBuilder(definitions).Add(unit, GridPosition.FrontBottom).Build(BattleModifiers.None);

        UnitStats stats = new UnitStatsProvider(definitions, globals, GameScenarioProvider.Empty.Definition).GetBattleUnitStats(state, unit.Id);

        Assert.That(stats, Is.EqualTo(new UnitStats(MaxHitpoints: 100, Armor: 30, Regeneration: 5, Initiative: 50, Damage: 25, Accuracy: 100, ExperienceKilled: 0, ExperienceNextLevel: 1000)));
    }

    [Test]
    public void DefendTest()
    {
        UnitDefinition definition = TestHelpers.SimpleUnitDefinition("unit", maxHitpoints: 100, initiative: 50, damage: 25) with { Armor = 20 };
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder().AddUnit(definition).Build();
        GlobalVariables globals = new();

        BattleUnit unit = new(new BattleUnitId(0), null, definition.Id, BattleSide.Defender, 100, 0, 0, 1, [], [new BattleDefendStatusEffect()], null, [], null, false);
        BattleState state = new BattleStateBuilder(definitions).Add(unit, GridPosition.FrontBottom).Build(BattleModifiers.None);

        UnitStats stats = new UnitStatsProvider(definitions, globals, GameScenarioProvider.Empty.Definition).GetBattleUnitStats(state, unit.Id);

        Assert.That(stats, Is.EqualTo(new UnitStats(MaxHitpoints: 100, Armor: 60, Regeneration: 5, Initiative: 50, Damage: 25, Accuracy: 100, ExperienceKilled: 0, ExperienceNextLevel: 1000)));
    }

    [Test]
    public void DefendCappedTest()
    {
        UnitDefinition definition = TestHelpers.SimpleUnitDefinition("unit", maxHitpoints: 100, initiative: 50, damage: 25) with { Armor = 85 };
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder().AddUnit(definition).Build();
        GlobalVariables globals = new();

        BattleUnit unit = new(new BattleUnitId(0), null, definition.Id, BattleSide.Defender, 100, 0, 0, 1, [], [new BattleDefendStatusEffect()], null, [], null, false);
        BattleState state = new BattleStateBuilder(definitions).Add(unit, GridPosition.FrontBottom).Build(BattleModifiers.None);

        UnitStats stats = new UnitStatsProvider(definitions, globals, GameScenarioProvider.Empty.Definition).GetBattleUnitStats(state, unit.Id);

        Assert.That(stats, Is.EqualTo(new UnitStats(MaxHitpoints: 100, Armor: 90, Regeneration: 5, Initiative: 50, Damage: 25, Accuracy: 100, ExperienceKilled: 0, ExperienceNextLevel: 1000)));
    }

    [Test]
    public void DefendWithoutArmorTest()
    {
        UnitDefinition definition = TestHelpers.SimpleUnitDefinition("unit", maxHitpoints: 100, initiative: 50, damage: 25);
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder().AddUnit(definition).Build();
        GlobalVariables globals = new();

        BattleUnit unit = new(new BattleUnitId(0), null, definition.Id, BattleSide.Defender, 100, 0, 0, 1, [], [new BattleDefendStatusEffect()], null, [], null, false);
        BattleState state = new BattleStateBuilder(definitions).Add(unit, GridPosition.FrontBottom).Build(BattleModifiers.None);

        UnitStats stats = new UnitStatsProvider(definitions, globals, GameScenarioProvider.Empty.Definition).GetBattleUnitStats(state, unit.Id);

        Assert.That(stats, Is.EqualTo(new UnitStats(MaxHitpoints: 100, Armor: 50, Regeneration: 5, Initiative: 50, Damage: 25, Accuracy: 100, ExperienceKilled: 0, ExperienceNextLevel: 1000)));
    }

    [Test]
    public void DefendAfterFortificationTest()
    {
        UnitDefinition definition = TestHelpers.SimpleUnitDefinition("unit", maxHitpoints: 100, initiative: 50, damage: 25) with { Armor = 10 };
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder().AddUnit(definition).Build();
        GlobalVariables globals = new();

        BattleUnit unit = new(new BattleUnitId(0), null, definition.Id, BattleSide.Defender, 100, 0, 0, 1, [], [new BattleFortificationStatusEffect(50), new BattleDefendStatusEffect()], null, [], null, false);
        BattleState state = new BattleStateBuilder(definitions).Add(unit, GridPosition.FrontBottom).Build(BattleModifiers.None);

        UnitStats stats = new UnitStatsProvider(definitions, globals, GameScenarioProvider.Empty.Definition).GetBattleUnitStats(state, unit.Id);

        Assert.That(stats, Is.EqualTo(new UnitStats(MaxHitpoints: 100, Armor: 75, Regeneration: 5, Initiative: 50, Damage: 25, Accuracy: 100, ExperienceKilled: 0, ExperienceNextLevel: 1000)));
    }

    [Test]
    public void BoostAndLowerDamageTest()
    {
        UnitDefinition definition = TestHelpers.SimpleUnitDefinition("unit", maxHitpoints: 100, initiative: 50, damage: 101);
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder().AddUnit(definition).Build();
        GlobalVariables globals = new();

        BattleUnit unit = new(new BattleUnitId(0), null, definition.Id, BattleSide.Attacker, 100, 0, 0, 1, [], [new BattleIncreaseDamageStatusEffect(25, false), new BattleReduceDamageStatusEffect(50, false)], null, [], null, false);
        BattleState state = new BattleStateBuilder(definitions).Add(unit, GridPosition.FrontBottom).Build(BattleModifiers.None);

        UnitAttackStats stats = new UnitStatsProvider(definitions, globals, GameScenarioProvider.Empty.Definition).GetBattleAttackStats(state, unit.Id, definition.Attack);

        Assert.That(stats, Is.EqualTo(new UnitAttackStats(Accuracy: 100, Damage: 76, DrainPercent: 0)));
    }

    [Test]
    public void ReduceInitiativeTest()
    {
        UnitDefinition definition = TestHelpers.SimpleUnitDefinition("unit", maxHitpoints: 100, initiative: 50, damage: 10);
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder().AddUnit(definition).Build();
        GlobalVariables globals = new();

        BattleUnit unit = new(new BattleUnitId(0), null, definition.Id, BattleSide.Attacker, 100, 0, 0, 1, [], [new BattleReduceInitiativeStatusEffect(50, false)], null, [], null, false);
        BattleState state = new BattleStateBuilder(definitions).Add(unit, GridPosition.FrontBottom).Build(BattleModifiers.None);
        UnitStatsProvider provider = new(definitions, globals, GameScenarioProvider.Empty.Definition);

        Assert.That(provider.GetBattleUnitInitiative(state, unit.Id, 9), Is.EqualTo(30));
        Assert.That(provider.GetBattleUnitInitiative(state, unit.Id, 0), Is.EqualTo(25));
    }

    [Test]
    public void BoostedEquipmentAttackTest()
    {
        UnitDefinition definition = TestHelpers.SimpleUnitDefinition("unit", maxHitpoints: 100, initiative: 50, damage: 10);
        UnitAttackDefinition weapon = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("weapon"), initiative: 50, damage: 41, accuracy: 80, reach: UnitAttackReach.Any);
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder().AddUnit(definition).Build();
        GlobalVariables globals = new();

        BattleUnit unit = new(new BattleUnitId(0), null, definition.Id, BattleSide.Attacker, 100, 0, 0, 1, [], [new BattleIncreaseDamageStatusEffect(50, false)], null, [weapon.Id], null, false);
        BattleState state = new BattleStateBuilder(definitions).Add(unit, GridPosition.FrontBottom).Build(BattleModifiers.None);

        UnitAttackStats stats = new UnitStatsProvider(definitions, globals, GameScenarioProvider.Empty.Definition).GetBattleAttackStats(state, unit.Id, weapon);

        Assert.That(stats, Is.EqualTo(new UnitAttackStats(Accuracy: 80, Damage: 61, DrainPercent: 0)));
    }

    [Test]
    public void BoostedItemAttackTest()
    {
        UnitDefinition definition = TestHelpers.SimpleUnitDefinition("unit", maxHitpoints: 100, initiative: 50, damage: 10);
        UnitAttackDefinition weapon = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("weapon"), initiative: 50, damage: 41, accuracy: 80, reach: UnitAttackReach.Any);
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder().AddUnit(definition).Build();
        GlobalVariables globals = new();

        BattleUnit unit = new(new BattleUnitId(0), null, definition.Id, BattleSide.Attacker, 100, 0, 0, 1, [], [new BattleIncreaseDamageStatusEffect(50, false)], null, [], null, false);
        BattleState state = new BattleStateBuilder(definitions).Add(unit, GridPosition.FrontBottom).Build(BattleModifiers.None);

        UnitAttackStats stats = new UnitStatsProvider(definitions, globals, GameScenarioProvider.Empty.Definition).GetBattleAttackStats(state, unit.Id, weapon);

        Assert.That(stats, Is.EqualTo(new UnitAttackStats(Accuracy: 80, Damage: 61, DrainPercent: 0)));
    }

    [Test]
    public void ItemAttackSideAccuracyTest()
    {
        UnitDefinition definition = TestHelpers.SimpleUnitDefinition("unit", maxHitpoints: 100, initiative: 50, damage: 10);
        UnitAttackDefinition orb = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("orb"), initiative: 50, damage: 40, accuracy: 80, reach: UnitAttackReach.Any);
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder().AddUnit(definition).Build();
        GlobalVariables globals = new();
        BattleModifiers modifiers = new(
            Attacker: new BattleSideModifiers(ExperiencePercent: 0, AccuracyBonus: -15, RandomDamageBonus: 0),
            Defender: BattleSideModifiers.None);

        BattleUnit unit = new(new BattleUnitId(0), null, definition.Id, BattleSide.Attacker, 100, 0, 0, 1, [], [], null, [], null, false);
        BattleState state = new BattleStateBuilder(definitions).Add(unit, GridPosition.FrontBottom).Build(modifiers);

        UnitAttackStats stats = new UnitStatsProvider(definitions, globals, GameScenarioProvider.Empty.Definition).GetBattleAttackStats(state, unit.Id, orb);

        Assert.That(stats, Is.EqualTo(new UnitAttackStats(Accuracy: 65, Damage: 40, DrainPercent: 0)));
    }

    [Test]
    public void TransformedExperienceTest()
    {
        UnitDefinition originalDefinition = TestHelpers.SimpleUnitDefinition("original", maxHitpoints: 100, initiative: 50, damage: 25);
        UnitDefinition transformedDefinition = TestHelpers.SimpleUnitDefinition("transformed", maxHitpoints: 30, initiative: 40, damage: 5) with { ExperienceNextLevel = 100 };
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder().AddUnit(originalDefinition).AddUnit(transformedDefinition).Build();
        GlobalVariables globals = new();

        BattleUnit unit = new(new BattleUnitId(0), null, transformedDefinition.Id, BattleSide.Defender, 30, 500, 0, 1, [], [new BattleTransformStatusEffect(BattleTransformType.Transform, originalDefinition.Id, 1, [], null)], null, [], null, false);
        BattleState state = new BattleStateBuilder(definitions).Add(unit, GridPosition.FrontBottom).Build(BattleModifiers.None);

        UnitExperience experience = new UnitStatsProvider(definitions, globals, GameScenarioProvider.Empty.Definition).GetBattleUnitExperience(state, unit.Id);

        Assert.That(experience, Is.EqualTo(new UnitExperience(Current: 0, NextLevel: 100, CanGainExperience: true, IsMaximumLevel: false, IsPendingUpgrade: false)));
    }
}
