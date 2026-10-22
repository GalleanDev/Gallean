namespace Gallean.Engine.Tests;

public sealed class UnitStatsCalculatorTests
{
    private static UnitDefinition BaseUnit()
    {
        return TestHelpers.SimpleUnitDefinition("unit", maxHitpoints: 100, initiative: 50, damage: 40) with
        {
            Armor = 10
        };
    }

    [Test]
    public void NoModifiersTest()
    {
        UnitDefinition definition = BaseUnit();
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder().AddUnit(definition).Build();

        UnitStats stats = UnitStatsCalculator.GetStats(definition, definition.Level, [], definitions, new GlobalVariables());

        Assert.That(stats, Is.EqualTo(new UnitStats(
            MaxHitpoints: 100,
            Armor: 10,
            Regeneration: 5,
            Initiative: 50,
            Damage: 40,
            Accuracy: 100,
            ExperienceKilled: 0,
            ExperienceNextLevel: 1000)));
    }

    [Test]
    public void FlatAndPercentModifiersTest()
    {
        UnitDefinition definition = BaseUnit();

        UnitModifierDefinition armorBonus = TestHelpers.SimpleModifier("armor") with
        {
            Armor = new UnitStatModifier(20, 0)
        };

        UnitModifierDefinition hitpointsBonus = TestHelpers.SimpleModifier("hitpoints") with
        {
            Hitpoints = new UnitStatModifier(0, 15)
        };

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(definition)
            .AddUnitModifier(armorBonus)
            .AddUnitModifier(hitpointsBonus)
            .Build();

        UnitStats stats = UnitStatsCalculator.GetStats(definition, definition.Level, [UnitModifier.Permanent(armorBonus.Id), UnitModifier.Permanent(hitpointsBonus.Id)], definitions, new GlobalVariables());

        Assert.That(stats, Is.EqualTo(new UnitStats(
            MaxHitpoints: 115,
            Armor: 30,
            Regeneration: 5,
            Initiative: 50,
            Damage: 40,
            Accuracy: 100,
            ExperienceKilled: 0,
            ExperienceNextLevel: 1000)));
    }

    [Test]
    public void ChainedPercentModifiersTest()
    {
        UnitDefinition definition = BaseUnit();

        UnitModifierDefinition initiativeBonus = TestHelpers.SimpleModifier("initiative") with
        {
            Initiative = new UnitStatModifier(0, 10)
        };

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(definition)
            .AddUnitModifier(initiativeBonus)
            .Build();

        UnitStats stats = UnitStatsCalculator.GetStats(
            definition,
            definition.Level,
            [UnitModifier.Permanent(initiativeBonus.Id), UnitModifier.Permanent(initiativeBonus.Id)],
            definitions,
            new GlobalVariables());

        Assert.That(stats, Is.EqualTo(new UnitStats(
            MaxHitpoints: 100,
            Armor: 10,
            Regeneration: 5,
            Initiative: 60,
            Damage: 40,
            Accuracy: 100,
            ExperienceKilled: 0,
            ExperienceNextLevel: 1000)));
    }

    [Test]
    public void WorldUnitStatsTest()
    {
        UnitDefinition definition = BaseUnit();

        UnitModifierDefinition damageBonus = TestHelpers.SimpleModifier("damage") with
        {
            Damage = new UnitStatModifier(0, 50)
        };

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(definition)
            .AddUnitModifier(damageBonus)
            .Build();

        IUnitStatsProvider statsProvider = new UnitStatsProvider(definitions, new GlobalVariables(), GameScenarioProvider.Empty.Definition);

        WorldUnit unit = new(new WorldUnitId("unit"), definition.Id, definition.MaxHitpoints, 0, definition.Level, [UnitModifier.Permanent(damageBonus.Id)], null, false);

        UnitStats stats = statsProvider.GetWorldUnitStats(unit);

        Assert.That(stats, Is.EqualTo(new UnitStats(
            MaxHitpoints: 100,
            Armor: 10,
            Regeneration: 5,
            Initiative: 50,
            Damage: 60,
            Accuracy: 100,
            ExperienceKilled: 0,
            ExperienceNextLevel: 1000)));
    }

    [Test]
    public void ArmorCapTest()
    {
        UnitDefinition definition = BaseUnit() with { Armor = 80 };

        UnitModifierDefinition armorBonus = TestHelpers.SimpleModifier("armor") with
        {
            Armor = new UnitStatModifier(20, 0)
        };

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(definition)
            .AddUnitModifier(armorBonus)
            .Build();

        UnitStats stats = UnitStatsCalculator.GetStats(definition, definition.Level, [UnitModifier.Permanent(armorBonus.Id)], definitions, new GlobalVariables());

        Assert.That(stats, Is.EqualTo(new UnitStats(
            MaxHitpoints: 100,
            Armor: 90,
            Regeneration: 5,
            Initiative: 50,
            Damage: 40,
            Accuracy: 100,
            ExperienceKilled: 0,
            ExperienceNextLevel: 1000)));
    }

    [Test]
    public void ArmorUnderCapTest()
    {
        UnitDefinition definition = BaseUnit() with { Armor = 80 };

        UnitModifierDefinition armorBonus = TestHelpers.SimpleModifier("armor") with
        {
            Armor = new UnitStatModifier(9, 0)
        };

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(definition)
            .AddUnitModifier(armorBonus)
            .Build();

        UnitStats stats = UnitStatsCalculator.GetStats(definition, definition.Level, [UnitModifier.Permanent(armorBonus.Id)], definitions, new GlobalVariables());

        Assert.That(stats, Is.EqualTo(new UnitStats(
            MaxHitpoints: 100,
            Armor: 89,
            Regeneration: 5,
            Initiative: 50,
            Damage: 40,
            Accuracy: 100,
            ExperienceKilled: 0,
            ExperienceNextLevel: 1000)));
    }

    [Test]
    public void DamageCapTest()
    {
        UnitDefinition definition = TestHelpers.SimpleUnitDefinition("unit", maxHitpoints: 100, initiative: 50, damage: 250);

        UnitModifierDefinition damageBonus = TestHelpers.SimpleModifier("damage") with
        {
            Damage = new UnitStatModifier(100, 0)
        };

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(definition)
            .AddUnitModifier(damageBonus)
            .Build();

        UnitStats stats = UnitStatsCalculator.GetStats(definition, definition.Level, [UnitModifier.Permanent(damageBonus.Id)], definitions, new GlobalVariables());

        Assert.That(stats, Is.EqualTo(new UnitStats(
            MaxHitpoints: 100,
            Armor: 0,
            Regeneration: 5,
            Initiative: 50,
            Damage: 300,
            Accuracy: 100,
            ExperienceKilled: 0,
            ExperienceNextLevel: 1000)));
    }

    [Test]
    public void DamageUnderCapTest()
    {
        UnitDefinition definition = TestHelpers.SimpleUnitDefinition("unit", maxHitpoints: 100, initiative: 50, damage: 250);

        UnitModifierDefinition damageBonus = TestHelpers.SimpleModifier("damage") with
        {
            Damage = new UnitStatModifier(49, 0)
        };

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(definition)
            .AddUnitModifier(damageBonus)
            .Build();

        UnitStats stats = UnitStatsCalculator.GetStats(definition, definition.Level, [UnitModifier.Permanent(damageBonus.Id)], definitions, new GlobalVariables());

        Assert.That(stats, Is.EqualTo(new UnitStats(
            MaxHitpoints: 100,
            Armor: 0,
            Regeneration: 5,
            Initiative: 50,
            Damage: 299,
            Accuracy: 100,
            ExperienceKilled: 0,
            ExperienceNextLevel: 1000)));
    }

    [Test]
    public void VisionRadiusCapTest()
    {
        UnitDefinition definition = BaseUnit() with { VisionRadius = 5 };

        UnitModifierDefinition visionBonus = TestHelpers.SimpleModifier("vision") with
        {
            VisionRadius = new UnitStatModifier(5, 0)
        };

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(definition)
            .AddUnitModifier(visionBonus)
            .Build();

        int visionRadius = UnitStatsCalculator.GetVisionRadius(definition, [UnitModifier.Permanent(visionBonus.Id)], definitions, new GlobalVariables());

        Assert.That(visionRadius, Is.EqualTo(8));
    }

    [Test]
    public void VisionRadiusUnderCapTest()
    {
        UnitDefinition definition = BaseUnit() with { VisionRadius = 5 };

        UnitModifierDefinition visionBonus = TestHelpers.SimpleModifier("vision") with
        {
            VisionRadius = new UnitStatModifier(2, 0)
        };

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(definition)
            .AddUnitModifier(visionBonus)
            .Build();

        int visionRadius = UnitStatsCalculator.GetVisionRadius(definition, [UnitModifier.Permanent(visionBonus.Id)], definitions, new GlobalVariables());

        Assert.That(visionRadius, Is.EqualTo(7));
    }

    [Test]
    public void HitpointsCapTest()
    {
        UnitDefinition definition = TestHelpers.SimpleUnitDefinition("unit", maxHitpoints: 9000, initiative: 50, damage: 40);

        UnitModifierDefinition hitpointsBonus = TestHelpers.SimpleModifier("hitpoints") with
        {
            Hitpoints = new UnitStatModifier(1500, 0)
        };

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(definition)
            .AddUnitModifier(hitpointsBonus)
            .Build();

        UnitStats stats = UnitStatsCalculator.GetStats(definition, definition.Level, [UnitModifier.Permanent(hitpointsBonus.Id)], definitions, new GlobalVariables());

        Assert.That(stats, Is.EqualTo(new UnitStats(
            MaxHitpoints: 9999,
            Armor: 0,
            Regeneration: 5,
            Initiative: 50,
            Damage: 40,
            Accuracy: 100,
            ExperienceKilled: 0,
            ExperienceNextLevel: 1000)));
    }

    [Test]
    public void HitpointsUnderCapTest()
    {
        UnitDefinition definition = TestHelpers.SimpleUnitDefinition("unit", maxHitpoints: 9000, initiative: 50, damage: 40);

        UnitModifierDefinition hitpointsBonus = TestHelpers.SimpleModifier("hitpoints") with
        {
            Hitpoints = new UnitStatModifier(998, 0)
        };

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(definition)
            .AddUnitModifier(hitpointsBonus)
            .Build();

        UnitStats stats = UnitStatsCalculator.GetStats(definition, definition.Level, [UnitModifier.Permanent(hitpointsBonus.Id)], definitions, new GlobalVariables());

        Assert.That(stats, Is.EqualTo(new UnitStats(
            MaxHitpoints: 9998,
            Armor: 0,
            Regeneration: 5,
            Initiative: 50,
            Damage: 40,
            Accuracy: 100,
            ExperienceKilled: 0,
            ExperienceNextLevel: 1000)));
    }

    [Test]
    public void InitiativeCapTest()
    {
        UnitDefinition definition = TestHelpers.SimpleUnitDefinition("unit", maxHitpoints: 100, initiative: 120, damage: 40);

        UnitModifierDefinition initiativeBonus = TestHelpers.SimpleModifier("initiative") with
        {
            Initiative = new UnitStatModifier(50, 0)
        };

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(definition)
            .AddUnitModifier(initiativeBonus)
            .Build();

        UnitStats stats = UnitStatsCalculator.GetStats(definition, definition.Level, [UnitModifier.Permanent(initiativeBonus.Id)], definitions, new GlobalVariables());

        Assert.That(stats, Is.EqualTo(new UnitStats(
            MaxHitpoints: 100,
            Armor: 0,
            Regeneration: 5,
            Initiative: 150,
            Damage: 40,
            Accuracy: 100,
            ExperienceKilled: 0,
            ExperienceNextLevel: 1000)));
    }

    [Test]
    public void InitiativeUnderCapTest()
    {
        UnitDefinition definition = TestHelpers.SimpleUnitDefinition("unit", maxHitpoints: 100, initiative: 120, damage: 40);

        UnitModifierDefinition initiativeBonus = TestHelpers.SimpleModifier("initiative") with
        {
            Initiative = new UnitStatModifier(29, 0)
        };

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(definition)
            .AddUnitModifier(initiativeBonus)
            .Build();

        UnitStats stats = UnitStatsCalculator.GetStats(definition, definition.Level, [UnitModifier.Permanent(initiativeBonus.Id)], definitions, new GlobalVariables());

        Assert.That(stats, Is.EqualTo(new UnitStats(
            MaxHitpoints: 100,
            Armor: 0,
            Regeneration: 5,
            Initiative: 149,
            Damage: 40,
            Accuracy: 100,
            ExperienceKilled: 0,
            ExperienceNextLevel: 1000)));
    }

    [Test]
    public void AccuracyCapTest()
    {
        UnitDefinition definition = TestHelpers.SimpleUnitDefinition("unit", maxHitpoints: 100, initiative: 50, damage: 40) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("unit"), initiative: 50, damage: 40, accuracy: 80, reach: UnitAttackReach.Any)
        };

        UnitModifierDefinition accuracyBonus = TestHelpers.SimpleModifier("accuracy") with
        {
            Accuracy = new UnitStatModifier(30, 0)
        };

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(definition)
            .AddUnitModifier(accuracyBonus)
            .Build();

        UnitStats stats = UnitStatsCalculator.GetStats(definition, definition.Level, [UnitModifier.Permanent(accuracyBonus.Id)], definitions, new GlobalVariables());
        UnitAttackStats attackStats = UnitAttackStatsCalculator.CalculateForUnit(definition, definition.Attack, definition.Level, [UnitModifier.Permanent(accuracyBonus.Id)], definitions, new GlobalVariables());

        Assert.That(stats, Is.EqualTo(new UnitStats(
            MaxHitpoints: 100,
            Armor: 0,
            Regeneration: 5,
            Initiative: 50,
            Damage: 40,
            Accuracy: 100,
            ExperienceKilled: 0,
            ExperienceNextLevel: 1000)));

        Assert.That(attackStats, Is.EqualTo(new UnitAttackStats(100, 40, 0)));
    }

    [Test]
    public void AccuracyUnderCapTest()
    {
        UnitDefinition definition = TestHelpers.SimpleUnitDefinition("unit", maxHitpoints: 100, initiative: 50, damage: 40) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("unit"), initiative: 50, damage: 40, accuracy: 80, reach: UnitAttackReach.Any)
        };

        UnitModifierDefinition accuracyBonus = TestHelpers.SimpleModifier("accuracy") with
        {
            Accuracy = new UnitStatModifier(19, 0)
        };

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(definition)
            .AddUnitModifier(accuracyBonus)
            .Build();

        UnitStats stats = UnitStatsCalculator.GetStats(definition, definition.Level, [UnitModifier.Permanent(accuracyBonus.Id)], definitions, new GlobalVariables());
        UnitAttackStats attackStats = UnitAttackStatsCalculator.CalculateForUnit(definition, definition.Attack, definition.Level, [UnitModifier.Permanent(accuracyBonus.Id)], definitions, new GlobalVariables());

        Assert.That(stats, Is.EqualTo(new UnitStats(
            MaxHitpoints: 100,
            Armor: 0,
            Regeneration: 5,
            Initiative: 50,
            Damage: 40,
            Accuracy: 99,
            ExperienceKilled: 0,
            ExperienceNextLevel: 1000)));

        Assert.That(attackStats, Is.EqualTo(new UnitAttackStats(99, 40, 0)));
    }

    [Test]
    public void DrainModifiersTest()
    {
        UnitDefinition definition = BaseUnit();

        UnitModifierDefinition weaponDrain = TestHelpers.SimpleModifier("weapondrain") with
        {
            DrainPercent = 25
        };

        UnitModifierDefinition amuletDrain = TestHelpers.SimpleModifier("amuletdrain") with
        {
            DrainPercent = 10
        };

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(definition)
            .AddUnitModifier(weaponDrain)
            .AddUnitModifier(amuletDrain)
            .Build();

        UnitAttackStats attackStats = UnitAttackStatsCalculator.CalculateForUnit(
            definition,
            definition.Attack,
            definition.Level,
            [UnitModifier.Permanent(weaponDrain.Id), UnitModifier.Permanent(amuletDrain.Id)],
            definitions,
            new GlobalVariables());

        Assert.That(attackStats, Is.EqualTo(new UnitAttackStats(100, 40, 35)));
    }

    [Test]
    public void RegenerationModifierTest()
    {
        UnitDefinition definition = BaseUnit();

        UnitModifierDefinition regenerationBonus = TestHelpers.SimpleModifier("regeneration") with
        {
            RegenerationPercent = 10
        };

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(definition)
            .AddUnitModifier(regenerationBonus)
            .Build();

        UnitStats stats = UnitStatsCalculator.GetStats(definition, definition.Level, [UnitModifier.Permanent(regenerationBonus.Id)], definitions, new GlobalVariables());

        Assert.That(stats, Is.EqualTo(new UnitStats(
            MaxHitpoints: 100,
            Armor: 10,
            Regeneration: 10,
            Initiative: 50,
            Damage: 40,
            Accuracy: 100,
            ExperienceKilled: 0,
            ExperienceNextLevel: 1000)));
    }

    [Test]
    public void RegenerationModifierOnZeroRateTest()
    {
        UnitDefinition definition = BaseUnit() with { Regeneration = 0 };

        UnitModifierDefinition regenerationBonus = TestHelpers.SimpleModifier("regeneration") with
        {
            RegenerationPercent = 10
        };

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(definition)
            .AddUnitModifier(regenerationBonus)
            .Build();

        UnitStats stats = UnitStatsCalculator.GetStats(definition, definition.Level, [UnitModifier.Permanent(regenerationBonus.Id)], definitions, new GlobalVariables());

        Assert.That(stats, Is.EqualTo(new UnitStats(
            MaxHitpoints: 100,
            Armor: 10,
            Regeneration: 10,
            Initiative: 50,
            Damage: 40,
            Accuracy: 100,
            ExperienceKilled: 0,
            ExperienceNextLevel: 1000)));
    }

    [Test]
    public void RegenerationModifiersKeepHighestTest()
    {
        UnitDefinition definition = BaseUnit();

        UnitModifierDefinition lowerRegeneration = TestHelpers.SimpleModifier("lower-regeneration") with
        {
            RegenerationPercent = 10
        };

        UnitModifierDefinition higherRegeneration = TestHelpers.SimpleModifier("higher-regeneration") with
        {
            RegenerationPercent = 15
        };

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(definition)
            .AddUnitModifier(lowerRegeneration)
            .AddUnitModifier(higherRegeneration)
            .Build();

        UnitStats stats = UnitStatsCalculator.GetStats(
            definition,
            definition.Level,
            [UnitModifier.Permanent(higherRegeneration.Id), UnitModifier.Permanent(lowerRegeneration.Id)],
            definitions,
            new GlobalVariables());

        Assert.That(stats, Is.EqualTo(new UnitStats(
            MaxHitpoints: 100,
            Armor: 10,
            Regeneration: 15,
            Initiative: 50,
            Damage: 40,
            Accuracy: 100,
            ExperienceKilled: 0,
            ExperienceNextLevel: 1000)));
    }

    [Test]
    public void RegenerationAboveModifierTest()
    {
        UnitDefinition definition = BaseUnit() with { Regeneration = 20 };

        UnitModifierDefinition regenerationBonus = TestHelpers.SimpleModifier("regeneration") with
        {
            RegenerationPercent = 10
        };

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(definition)
            .AddUnitModifier(regenerationBonus)
            .Build();

        UnitStats stats = UnitStatsCalculator.GetStats(definition, definition.Level, [UnitModifier.Permanent(regenerationBonus.Id)], definitions, new GlobalVariables());

        Assert.That(stats, Is.EqualTo(new UnitStats(
            MaxHitpoints: 100,
            Armor: 10,
            Regeneration: 20,
            Initiative: 50,
            Damage: 40,
            Accuracy: 100,
            ExperienceKilled: 0,
            ExperienceNextLevel: 1000)));
    }

    [Test]
    public void RegenerationScalesWithLevelTest()
    {
        UnitDefinition definition = BaseUnit();

        UnitDynamicUpgradeDefinition dynamicUpgrade = new(
            definition.Id,
            UpgradeChangeLevel: 5,
            LowLevelUpgrade: UnitDynamicUpgrade.Empty with
            {
                MaxHitpoints = 10,
                Armor = 1,
                Regeneration = 1,
                Initiative = 2,
                Damage = 3,
                Accuracy = 1,
                ExperienceKilled = 5,
                ExperienceNextLevel = 100
            },
            HighLevelUpgrade: UnitDynamicUpgrade.Empty with
            {
                MaxHitpoints = 20,
                Armor = 2,
                Regeneration = 2,
                Initiative = 4,
                Damage = 6,
                Accuracy = 2,
                ExperienceKilled = 10,
                ExperienceNextLevel = 200
            });

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(definition)
            .AddUnitDynamicUpgrade(dynamicUpgrade)
            .Build();

        UnitStats stats = UnitStatsCalculator.GetStats(definition, definition.Level + 2, [], definitions, new GlobalVariables());

        Assert.That(stats, Is.EqualTo(new UnitStats(
            MaxHitpoints: 120,
            Armor: 12,
            Regeneration: 7,
            Initiative: 54,
            Damage: 46,
            Accuracy: 100,
            ExperienceKilled: 10,
            ExperienceNextLevel: 1200)));
    }

    [Test]
    public void NegotiateTest()
    {
        UnitDefinition definition = BaseUnit() with { Negotiate = 10 };

        UnitDynamicUpgradeDefinition dynamicUpgrade = new(
            definition.Id,
            UpgradeChangeLevel: 5,
            LowLevelUpgrade: UnitDynamicUpgrade.Empty with { Negotiate = 5 },
            HighLevelUpgrade: UnitDynamicUpgrade.Empty);

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(definition)
            .AddUnitDynamicUpgrade(dynamicUpgrade)
            .Build();

        Assert.That(UnitStatsCalculator.GetNegotiate(definition, definition.Level, definitions), Is.EqualTo(10));
        Assert.That(UnitStatsCalculator.GetNegotiate(definition, definition.Level + 3, definitions), Is.EqualTo(25));
        Assert.That(UnitStatsCalculator.GetNegotiate(definition, definition.Level + 6, definitions), Is.EqualTo(30));
    }

    [Test]
    public void NegotiateWithoutValueTest()
    {
        UnitDefinition definition = BaseUnit();
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder().AddUnit(definition).Build();

        Assert.That(UnitStatsCalculator.GetNegotiate(definition, definition.Level, definitions), Is.Zero);
    }

    [Test]
    public void WorldUnitWardsTest()
    {
        UnitDefinition definition = BaseUnit() with
        {
            Wards = [new UnitAttackSourceWard(UnitAttackSource.Death, UnitWardCategory.Always)]
        };

        UnitModifierDefinition fireWard = TestHelpers.SimpleModifier("fireward") with
        {
            Wards = [new UnitAttackSourceWard(UnitAttackSource.Fire, UnitWardCategory.Once)]
        };

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(definition)
            .AddUnitModifier(fireWard)
            .Build();

        IUnitStatsProvider statsProvider = new UnitStatsProvider(definitions, new GlobalVariables(), GameScenarioProvider.Empty.Definition);

        WorldUnit unit = new(new WorldUnitId("unit"), definition.Id, definition.MaxHitpoints, 0, definition.Level, [UnitModifier.Permanent(fireWard.Id)], null, false);

        Assert.That(statsProvider.GetWorldUnitWards(unit), Is.EqualTo(new UnitWard[]
        {
            new UnitAttackSourceWard(UnitAttackSource.Death, UnitWardCategory.Always),
            new UnitAttackSourceWard(UnitAttackSource.Fire, UnitWardCategory.Once)
        }));
    }

    [Test]
    public void MovementPointsScaleWithLevelTest()
    {
        UnitDefinition definition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 3);

        UnitDynamicUpgradeDefinition dynamicUpgrade = new(
            definition.Id,
            UpgradeChangeLevel: 5,
            LowLevelUpgrade: UnitDynamicUpgrade.Empty with { MovementPoints = 1 },
            HighLevelUpgrade: UnitDynamicUpgrade.Empty with { MovementPoints = 2 });

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(definition)
            .AddUnitDynamicUpgrade(dynamicUpgrade)
            .Build();

        Assert.That(UnitStatsCalculator.GetMaxMovementPoints(definition, 1, [], definitions, new GlobalVariables()), Is.EqualTo(6));
        Assert.That(UnitStatsCalculator.GetMaxMovementPoints(definition, 3, [], definitions, new GlobalVariables()), Is.EqualTo(8));
        Assert.That(UnitStatsCalculator.GetMaxMovementPoints(definition, 7, [], definitions, new GlobalVariables()), Is.EqualTo(14));
    }

    [Test]
    public void CostsScaleWithLevelTest()
    {
        UnitDefinition definition = BaseUnit();

        UnitDynamicUpgradeDefinition dynamicUpgrade = new(
            definition.Id,
            UpgradeChangeLevel: 5,
            LowLevelUpgrade: UnitDynamicUpgrade.Empty with
            {
                PurchaseCost = new WorldResourceSet(Gold: 15, DeathMana: 1, RuneMana: 0, LifeMana: 0, InfernalMana: 0, GroveMana: 0),
                ReviveCost = new WorldResourceSet(Gold: 10, DeathMana: 0, RuneMana: 0, LifeMana: 0, InfernalMana: 0, GroveMana: 0),
                HealCost = new WorldResourceSet(Gold: 1, DeathMana: 0, RuneMana: 0, LifeMana: 0, InfernalMana: 0, GroveMana: 0),
                TrainingCost = new WorldResourceSet(Gold: 2, DeathMana: 0, RuneMana: 0, LifeMana: 0, InfernalMana: 0, GroveMana: 0)
            },
            HighLevelUpgrade: UnitDynamicUpgrade.Empty with
            {
                PurchaseCost = new WorldResourceSet(Gold: 30, DeathMana: 2, RuneMana: 0, LifeMana: 0, InfernalMana: 0, GroveMana: 0),
                ReviveCost = new WorldResourceSet(Gold: 20, DeathMana: 0, RuneMana: 0, LifeMana: 0, InfernalMana: 0, GroveMana: 0),
                HealCost = new WorldResourceSet(Gold: 2, DeathMana: 0, RuneMana: 0, LifeMana: 0, InfernalMana: 0, GroveMana: 0),
                TrainingCost = new WorldResourceSet(Gold: 4, DeathMana: 0, RuneMana: 0, LifeMana: 0, InfernalMana: 0, GroveMana: 0)
            });

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(definition)
            .AddUnitDynamicUpgrade(dynamicUpgrade)
            .Build();

        Assert.That(UnitStatsCalculator.GetCosts(definition, 1, definitions), Is.EqualTo(new UnitCosts(
            PurchaseCost: new WorldResourceSet(Gold: 50, DeathMana: 0, RuneMana: 0, LifeMana: 0, InfernalMana: 0, GroveMana: 0),
            ReviveCost: new WorldResourceSet(Gold: 50, DeathMana: 0, RuneMana: 0, LifeMana: 0, InfernalMana: 0, GroveMana: 0),
            HealCost: new WorldResourceSet(Gold: 1, DeathMana: 0, RuneMana: 0, LifeMana: 0, InfernalMana: 0, GroveMana: 0),
            TrainingCost: WorldResourceSet.Empty)));

        Assert.That(UnitStatsCalculator.GetCosts(definition, 7, definitions), Is.EqualTo(new UnitCosts(
            PurchaseCost: new WorldResourceSet(Gold: 170, DeathMana: 8, RuneMana: 0, LifeMana: 0, InfernalMana: 0, GroveMana: 0),
            ReviveCost: new WorldResourceSet(Gold: 130, DeathMana: 0, RuneMana: 0, LifeMana: 0, InfernalMana: 0, GroveMana: 0),
            HealCost: new WorldResourceSet(Gold: 9, DeathMana: 0, RuneMana: 0, LifeMana: 0, InfernalMana: 0, GroveMana: 0),
            TrainingCost: new WorldResourceSet(Gold: 16, DeathMana: 0, RuneMana: 0, LifeMana: 0, InfernalMana: 0, GroveMana: 0))));
    }

    [Test]
    public void ChainedFlatAndPercentHitpointsModifiersTest()
    {
        UnitDefinition definition = BaseUnit();

        UnitModifierDefinition flatHitpoints = TestHelpers.SimpleModifier("flatHitpoints") with
        {
            Hitpoints = new UnitStatModifier(50, 0)
        };

        UnitModifierDefinition percentHitpoints = TestHelpers.SimpleModifier("percentHitpoints") with
        {
            Hitpoints = new UnitStatModifier(0, 20)
        };

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(definition)
            .AddUnitModifier(flatHitpoints)
            .AddUnitModifier(percentHitpoints)
            .Build();

        UnitStats stats = UnitStatsCalculator.GetStats(
            definition,
            definition.Level,
            [UnitModifier.Permanent(flatHitpoints.Id), UnitModifier.Permanent(percentHitpoints.Id)],
            definitions,
            new GlobalVariables());

        Assert.That(stats, Is.EqualTo(new UnitStats(
            MaxHitpoints: 180,
            Armor: 10,
            Regeneration: 5,
            Initiative: 50,
            Damage: 40,
            Accuracy: 100,
            ExperienceKilled: 0,
            ExperienceNextLevel: 1000)));
    }

    [Test]
    public void ArmorClampedPerModifierTest()
    {
        UnitDefinition definition = BaseUnit() with { Armor = 0 };

        UnitModifierDefinition armorLoss = TestHelpers.SimpleModifier("armorLoss") with
        {
            Armor = new UnitStatModifier(-50, 0)
        };

        UnitModifierDefinition armorGain = TestHelpers.SimpleModifier("armorGain") with
        {
            Armor = new UnitStatModifier(20, 0)
        };

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(definition)
            .AddUnitModifier(armorLoss)
            .AddUnitModifier(armorGain)
            .Build();

        UnitStats stats = UnitStatsCalculator.GetStats(
            definition,
            definition.Level,
            [UnitModifier.Permanent(armorLoss.Id), UnitModifier.Permanent(armorGain.Id)],
            definitions,
            new GlobalVariables());

        Assert.That(stats, Is.EqualTo(new UnitStats(
            MaxHitpoints: 100,
            Armor: 20,
            Regeneration: 5,
            Initiative: 50,
            Damage: 40,
            Accuracy: 100,
            ExperienceKilled: 0,
            ExperienceNextLevel: 1000)));
    }

    [Test]
    public void LeadershipFloorTest()
    {
        UnitDefinition definition = BaseUnit() with { Leadership = 1 };

        UnitModifierDefinition leadershipLoss = TestHelpers.SimpleModifier("leadershipLoss") with
        {
            Leadership = new UnitStatModifier(-1, 0)
        };

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(definition)
            .AddUnitModifier(leadershipLoss)
            .Build();

        int leadership = UnitStatsCalculator.GetLeadership(definition, [UnitModifier.Permanent(leadershipLoss.Id)], definitions);

        Assert.That(leadership, Is.EqualTo(1));
    }
}
