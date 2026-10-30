using Gallean.Entities.GameData;

namespace Gallean.Engine.Tests;

public sealed class GameDefinitionProviderTests
{
    private static GameDefinitionProvider CreateProvider()
    {
        GlobalDatabase globalDb = GlobalDatabaseLoader.Load(RiseOfTheElves.GlobalDatabasePath);

        return new GameDefinitionProvider(globalDb);
    }

    private static IReadOnlyList<string> FormatSpellResearchCosts(IReadOnlyDictionary<WorldSpellDefinitionId, WorldResourceSet> costs)
    {
        List<string> rows = [];

        foreach (KeyValuePair<WorldSpellDefinitionId, WorldResourceSet> pair in costs.OrderBy(pair => pair.Key.ToString(), StringComparer.Ordinal))
        {
            rows.Add($"{pair.Key} {pair.Value}");
        }

        return rows;
    }

    private static IReadOnlyList<string> FormatLeaderUpgrades(IReadOnlyList<LeaderUpgradeDefinition> upgrades)
    {
        List<string> rows = [];

        foreach (LeaderUpgradeDefinition upgrade in upgrades)
        {
            rows.Add($"{upgrade.MinLevel} {upgrade.Priority} {upgrade.ModifierId} {upgrade.NameTextId}");
        }

        return rows;
    }

    private static IReadOnlyList<string> FormatLeaderNameCounts(GameDefinitionProvider provider)
    {
        List<string> rows = [];

        foreach (WorldRace race in Enum.GetValues<WorldRace>())
        {
            WorldRaceDefinition definition = provider.GetRace(race);

            rows.Add($"{race} {definition.MaleLeaderNames.Length} male, {definition.FemaleLeaderNames.Length} female");
        }

        return rows;
    }

    private static IReadOnlyList<string> FormatThiefActions(GameDefinitionProvider provider)
    {
        List<string> rows = [];

        foreach (WorldThiefActionType type in Enum.GetValues<WorldThiefActionType>())
        {
            WorldThiefActionDefinition definition = provider.GetThiefAction(type);
            rows.Add($"{definition.Type} {definition.Chance} {definition.NameTextId} {definition.DescriptionTextId}");
        }

        return rows;
    }

    [Test]
    public void GetUnitTest()
    {
        GameDefinitionProvider provider = CreateProvider();

        UnitDefinition actual = provider.GetUnit(new UnitDefinitionId("G000UU0001"));

        UnitDefinition expected = TestHelpers.SimpleUnitDefinition("G000UU0001", maxHitpoints: 100, initiative: 50, damage: 25) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("g000aa0001"), initiative: 50, damage: 25, accuracy: 80, reach: UnitAttackReach.Adjacent),
            NameTextId = new GlobalTextId("x000tg2001"),
            DescriptionTextId = new GlobalTextId("x000tg2501"),
            AbilityTextId = new GlobalTextId("x000tgb006"),
            Regeneration = 5,
            Subrace = WorldSubraceType.Human,
            DeathAnimation = UnitDeathAnimation.Human,
            TrainingCost = new WorldResourceSet(4, 0, 0, 0, 0, 0),
            ExperienceKilled = 20,
            ExperienceNextLevel = 80
        };

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void GetSummonUnitTest()
    {
        GameDefinitionProvider provider = CreateProvider();

        UnitDefinition actual = provider.GetUnit(new UnitDefinitionId("g000uu0024"));

        UnitDefinition expected = TestHelpers.SimpleUnitDefinition("g000uu0024", maxHitpoints: 175, initiative: 55, damage: 65) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("g000aa0024"), initiative: 55, damage: 65, accuracy: 80, reach: UnitAttackReach.Adjacent),
            NameTextId = new GlobalTextId("x000tg2024"),
            DescriptionTextId = new GlobalTextId("x000tg2524"),
            AbilityTextId = new GlobalTextId("x000tgb006"),
            Regeneration = 5,
            Category = UnitCategory.Summon,
            Subrace = WorldSubraceType.Human,
            DeathAnimation = UnitDeathAnimation.Human,
            IsLarge = true,
            VisionRadius = 5,
            PurchaseCost = new WorldResourceSet(1700, 0, 0, 0, 0, 0),
            ReviveCost = new WorldResourceSet(200, 0, 0, 0, 0, 0),
            HealCost = new WorldResourceSet(2, 0, 0, 0, 0, 0),
            TrainingCost = new WorldResourceSet(4, 0, 0, 0, 0, 0),
            ExperienceKilled = 95,
            ExperienceNextLevel = 375,
            Leadership = 1,
            Negotiate = 30,
            MovementPoints = 20,
            Lifetime = 1,
            Wards =
            [
                new UnitAttackClassWard(UnitAttackType.Poison, UnitWardCategory.Always),
                new UnitAttackSourceWard(UnitAttackSource.Mind, UnitWardCategory.Always)
            ]
        };

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void GetUnitSecondaryAttackTest()
    {
        GameDefinitionProvider provider = CreateProvider();

        UnitDefinition actual = provider.GetUnit(new UnitDefinitionId("g000uu0069"));

        Assert.That(actual, Is.EqualTo(new UnitDefinition(
            Id: new UnitDefinitionId("g000uu0069"),
            NameTextId: new GlobalTextId("x000tg2069"),
            DescriptionTextId: new GlobalTextId("x000tg2569"),
            AbilityTextId: new GlobalTextId("x000tgb006"),
            MaxHitpoints: 250,
            Armor: 0,
            Regeneration: 5,
            Attack: new UnitAttackDefinition(
                Id: new UnitAttackDefinitionId("g000aa0069"),
                Type: UnitAttackType.Damage,
                Source: UnitAttackSource.Weapon,
                Initiative: 50,
                Accuracy: 80,
                Reach: UnitAttackReach.Adjacent,
                Damage: 60,
                Heal: 0,
                Level: 0,
                IsInfinite: false,
                SummonUnitIds: [],
                SmallTransformUnitId: null,
                LargeTransformUnitId: null,
                HasCriticalHit: false,
                AlternativeAttackId: null,
                WardModifierIds: []),
            SecondaryAttack: new UnitAttackDefinition(
                Id: new UnitAttackDefinitionId("g000aa1069"),
                Type: UnitAttackType.Poison,
                Source: UnitAttackSource.Death,
                Initiative: 50,
                Accuracy: 40,
                Reach: UnitAttackReach.Adjacent,
                Damage: 15,
                Heal: 0,
                Level: 0,
                IsInfinite: true,
                SummonUnitIds: [],
                SmallTransformUnitId: null,
                LargeTransformUnitId: null,
                HasCriticalHit: false,
                AlternativeAttackId: null,
                WardModifierIds: []),
            AttacksTwice: false,
            Category: UnitCategory.Soldier,
            LeaderCategory: null,
            Race: WorldRace.Heretic,
            Subrace: WorldSubraceType.Heretic,
            IsMale: true,
            DeathAnimation: UnitDeathAnimation.Heretic,
            IsLarge: true,
            IsWaterOnly: false,
            MoveAbility: UnitMoveAbility.None,
            VisionRadius: null,
            PurchaseCost: new WorldResourceSet(300, 0, 0, 0, 0, 0),
            ReviveCost: new WorldResourceSet(200, 0, 0, 0, 0, 0),
            HealCost: new WorldResourceSet(2, 0, 0, 0, 0, 0),
            TrainingCost: new WorldResourceSet(4, 0, 0, 0, 0, 0),
            RequiredBuildingId: new WorldPlayerBuildingDefinitionId("g000bb0071"),
            ExperienceKilled: 120,
            ExperienceNextLevel: 475,
            Level: 1,
            Leadership: null,
            Negotiate: null,
            MovementPoints: null,
            Lifetime: null,
            LeaderAbilities: LeaderAbility.None,
            PreviousUnitId: null,
            Wards: [])));
    }

    [Test]
    public void GetUnitSummonAttackTest()
    {
        GameDefinitionProvider provider = CreateProvider();

        UnitDefinition actual = provider.GetUnit(new UnitDefinitionId("g000uu0153"));

        Assert.That(actual.Attack, Is.EqualTo(new UnitAttackDefinition(
            Id: new UnitAttackDefinitionId("g000aa0153"),
            Type: UnitAttackType.Summon,
            Source: UnitAttackSource.Air,
            Initiative: 40,
            Accuracy: 100,
            Reach: UnitAttackReach.Any,
            Damage: 0,
            Heal: 0,
            Level: 0,
            IsInfinite: true,
            SummonUnitIds: [new UnitDefinitionId("g000uu6103")],
            SmallTransformUnitId: null,
            LargeTransformUnitId: null,
            HasCriticalHit: false,
            AlternativeAttackId: null,
            WardModifierIds: [])));
    }

    [Test]
    public void GetAttackSummonUnitIdsTest()
    {
        GameDefinitionProvider provider = CreateProvider();

        UnitAttackDefinition actual = provider.GetUnit(new UnitDefinitionId("g000uu6014")).Attack;

        Assert.That(actual, Is.EqualTo(new UnitAttackDefinition(
            Id: new UnitAttackDefinitionId("g000aa6014"),
            Type: UnitAttackType.Summon,
            Source: UnitAttackSource.Death,
            Initiative: 90,
            Accuracy: 100,
            Reach: UnitAttackReach.All,
            Damage: 0,
            Heal: 0,
            Level: 0,
            IsInfinite: true,
            SummonUnitIds:
            [
                new UnitDefinitionId("g000uu0083"),
                new UnitDefinitionId("g000uu0084"),
                new UnitDefinitionId("g000uu0085"),
                new UnitDefinitionId("g000uu0178"),
                new UnitDefinitionId("g000uu0174"),
                new UnitDefinitionId("g000uu0095"),
                new UnitDefinitionId("g000uu0181")
            ],
            SmallTransformUnitId: null,
            LargeTransformUnitId: null,
            HasCriticalHit: false,
            AlternativeAttackId: null,
            WardModifierIds: [])));
    }

    [Test]
    public void GetGiveProtectionAttackTest()
    {
        GameDefinitionProvider provider = CreateProvider();

        UnitAttackDefinition actual = provider.GetAttack(new UnitAttackDefinitionId("g000aa8133"));

        Assert.That(actual, Is.EqualTo(new UnitAttackDefinition(
            Id: new UnitAttackDefinitionId("g000aa8133"),
            Type: UnitAttackType.GiveProtection,
            Source: UnitAttackSource.Life,
            Initiative: 10,
            Accuracy: 100,
            Reach: UnitAttackReach.Any,
            Damage: 0,
            Heal: 0,
            Level: 0,
            IsInfinite: false,
            SummonUnitIds: [],
            SmallTransformUnitId: null,
            LargeTransformUnitId: null,
            HasCriticalHit: false,
            AlternativeAttackId: null,
            WardModifierIds:
            [
                new UnitModifierDefinitionId("g000um0001"),
                new UnitModifierDefinitionId("g000um0016"),
                new UnitModifierDefinitionId("g000um0010"),
                new UnitModifierDefinitionId("g000um0005")
            ])));
    }

    [Test]
    public void GetCriticalAttackTest()
    {
        GameDefinitionProvider provider = CreateProvider();

        UnitAttackDefinition actual = provider.GetAttack(new UnitAttackDefinitionId("g000aa8017"));

        Assert.That(actual, Is.EqualTo(new UnitAttackDefinition(
            Id: new UnitAttackDefinitionId("g000aa8017"),
            Type: UnitAttackType.Damage,
            Source: UnitAttackSource.Weapon,
            Initiative: 35,
            Accuracy: 80,
            Reach: UnitAttackReach.Adjacent,
            Damage: 100,
            Heal: 0,
            Level: 0,
            IsInfinite: false,
            SummonUnitIds: [],
            SmallTransformUnitId: null,
            LargeTransformUnitId: null,
            HasCriticalHit: true,
            AlternativeAttackId: null,
            WardModifierIds: [])));
    }

    [Test]
    public void GetUnitTransformOtherAttackTest()
    {
        GameDefinitionProvider provider = CreateProvider();

        UnitAttackDefinition actual = provider.GetUnit(new UnitDefinitionId("g000uu0067")).Attack;

        Assert.That(actual, Is.EqualTo(new UnitAttackDefinition(
            Id: new UnitAttackDefinitionId("g000aa0067"),
            Type: UnitAttackType.TransformEnemy,
            Source: UnitAttackSource.Mind,
            Initiative: 20,
            Accuracy: 80,
            Reach: UnitAttackReach.Any,
            Damage: 0,
            Heal: 0,
            Level: 0,
            IsInfinite: true,
            SummonUnitIds: [],
            SmallTransformUnitId: new UnitDefinitionId("g000uu5034"),
            LargeTransformUnitId: new UnitDefinitionId("g000uu6104"),
            HasCriticalHit: false,
            AlternativeAttackId: null,
            WardModifierIds: [])));
    }

    [Test]
    public void GetAlternativeAttacksTest()
    {
        GameDefinitionProvider provider = CreateProvider();

        UnitDefinition first = provider.GetUnit(new UnitDefinitionId("g000uu0171"));
        UnitDefinition second = provider.GetUnit(new UnitDefinitionId("g000uu0164"));

        Assert.That(first.Attack.AlternativeAttackId, Is.EqualTo(new UnitAttackDefinitionId("g000aa1171")));
        Assert.That(second.Attack.AlternativeAttackId, Is.EqualTo(new UnitAttackDefinitionId("g000aa1164")));
    }

    [Test]
    public void GetUnitUpgradesTest()
    {
        GameDefinitionProvider provider = CreateProvider();

        IReadOnlyList<UnitUpgradeDefinition> upgrades = provider.GetUnitUpgrades(new UnitDefinitionId("G000UU0001"));

        Assert.That(upgrades, Is.EqualTo([
            new UnitUpgradeDefinition(
                new UnitDefinitionId("g000uu0001"),
                new UnitDefinitionId("g000uu0002"),
                new WorldPlayerBuildingDefinitionId("g000bb0001")),
            new UnitUpgradeDefinition(
                new UnitDefinitionId("g000uu0001"),
                new UnitDefinitionId("g000uu0004"),
                new WorldPlayerBuildingDefinitionId("g000bb0007"))
        ]));

        Assert.That(provider.GetUnitUpgrades(new UnitDefinitionId("g000uu0002")), Is.EqualTo([
            new UnitUpgradeDefinition(
                new UnitDefinitionId("g000uu0002"),
                new UnitDefinitionId("g000uu0003"),
                new WorldPlayerBuildingDefinitionId("g000bb0002"))
        ]));

        Assert.That(provider.GetUnitUpgrades(new UnitDefinitionId("INVALID")), Is.Empty);
    }

    [Test]
    public void GetUnitDynamicUpgradeTest()
    {
        GameDefinitionProvider provider = CreateProvider();

        UnitDynamicUpgradeDefinition dynamicUpgrade = provider.GetUnitDynamicUpgrade(new UnitDefinitionId("G000UU0001"));

        Assert.That(dynamicUpgrade, Is.EqualTo(new UnitDynamicUpgradeDefinition(
            new UnitDefinitionId("G000UU0001"),
            UpgradeChangeLevel: 10,
            new UnitDynamicUpgrade(
                MaxHitpoints: 10,
                Armor: 0,
                Regeneration: 0,
                Initiative: 0,
                Damage: 3,
                Heal: 0,
                Accuracy: 1,
                ExperienceKilled: 2,
                ExperienceNextLevel: 0,
                MovementPoints: 1,
                Negotiate: 0,
                PurchaseCost: new WorldResourceSet(Gold: 5, DeathMana: 0, RuneMana: 0, LifeMana: 0, InfernalMana: 0, GroveMana: 0),
                ReviveCost: WorldResourceSet.Empty,
                HealCost: WorldResourceSet.Empty,
                TrainingCost: WorldResourceSet.Empty),
            new UnitDynamicUpgrade(
                MaxHitpoints: 5,
                Armor: 0,
                Regeneration: 0,
                Initiative: 0,
                Damage: 1,
                Heal: 0,
                Accuracy: 0,
                ExperienceKilled: 1,
                ExperienceNextLevel: 0,
                MovementPoints: 0,
                Negotiate: 0,
                PurchaseCost: new WorldResourceSet(Gold: 3, DeathMana: 0, RuneMana: 0, LifeMana: 0, InfernalMana: 0, GroveMana: 0),
                ReviveCost: WorldResourceSet.Empty,
                HealCost: WorldResourceSet.Empty,
                TrainingCost: WorldResourceSet.Empty))));
    }

    [Test]
    public void GetUnitNotFoundTest()
    {
        GameDefinitionProvider provider = CreateProvider();

        Assert.Throws<KeyNotFoundException>(() => provider.GetUnit(new UnitDefinitionId("INVALID")));
    }

    [Test]
    public void GetUnitModifierTest()
    {
        GameDefinitionProvider provider = CreateProvider();

        Assert.That(provider.GetUnitModifier(new UnitModifierDefinitionId("G000UM9036")), Is.EqualTo(new UnitModifierDefinition(
            new UnitModifierDefinitionId("G000UM9036"),
            DescriptionTextId: new GlobalTextId("x000tg6036"),
            MoveAbility: UnitMoveAbility.Forest,
            MaxMovement: default,
            Leadership: default,
            VisionRadius: default,
            Hitpoints: default,
            RegenerationPercent: default,
            Armor: default,
            Initiative: default,
            Damage: default,
            Accuracy: default,
            DrainPercent: 0,
            FastRetreat: false,
            PurchaseDiscountPercent: 0,
            LeaderAbilities: LeaderAbility.None,
            Wards: [])));

        Assert.That(provider.GetUnitModifier(new UnitModifierDefinitionId("G000UM9037")), Is.EqualTo(new UnitModifierDefinition(
            new UnitModifierDefinitionId("G000UM9037"),
            DescriptionTextId: new GlobalTextId("x000tg6037"),
            MoveAbility: UnitMoveAbility.Water,
            MaxMovement: default,
            Leadership: default,
            VisionRadius: default,
            Hitpoints: default,
            RegenerationPercent: default,
            Armor: default,
            Initiative: default,
            Damage: default,
            Accuracy: default,
            DrainPercent: 0,
            FastRetreat: false,
            PurchaseDiscountPercent: 0,
            LeaderAbilities: LeaderAbility.None,
            Wards: [])));

        Assert.That(provider.GetUnitModifier(new UnitModifierDefinitionId("G000UM9032")), Is.EqualTo(new UnitModifierDefinition(
            new UnitModifierDefinitionId("G000UM9032"),
            DescriptionTextId: new GlobalTextId("x000tg6032"),
            MoveAbility: UnitMoveAbility.None,
            MaxMovement: default,
            Leadership: new UnitStatModifier(-1, 0),
            VisionRadius: default,
            Hitpoints: default,
            RegenerationPercent: default,
            Armor: default,
            Initiative: default,
            Damage: default,
            Accuracy: default,
            DrainPercent: 0,
            FastRetreat: false,
            PurchaseDiscountPercent: 0,
            LeaderAbilities: LeaderAbility.None,
            Wards: [])));
    }

    [Test]
    public void GetUnitModifierStatsTest()
    {
        GameDefinitionProvider provider = CreateProvider();

        Assert.That(provider.GetUnitModifier(new UnitModifierDefinitionId("G000UM0012")), Is.EqualTo(new UnitModifierDefinition(
            new UnitModifierDefinitionId("G000UM0012"),
            DescriptionTextId: new GlobalTextId("x000tgc012"),
            MoveAbility: UnitMoveAbility.None,
            MaxMovement: default,
            Leadership: default,
            VisionRadius: default,
            Hitpoints: default,
            RegenerationPercent: default,
            Armor: new UnitStatModifier(20, 0),
            Initiative: default,
            Damage: default,
            Accuracy: default,
            DrainPercent: 0,
            FastRetreat: false,
            PurchaseDiscountPercent: 0,
            LeaderAbilities: LeaderAbility.None,
            Wards: [])));

        Assert.That(provider.GetUnitModifier(new UnitModifierDefinitionId("G000UM2007")), Is.EqualTo(new UnitModifierDefinition(
            new UnitModifierDefinitionId("G000UM2007"),
            DescriptionTextId: new GlobalTextId("x000tg6000"),
            MoveAbility: UnitMoveAbility.None,
            MaxMovement: default,
            Leadership: default,
            VisionRadius: default,
            Hitpoints: new UnitStatModifier(0, 15),
            RegenerationPercent: default,
            Armor: default,
            Initiative: default,
            Damage: default,
            Accuracy: default,
            DrainPercent: 0,
            FastRetreat: false,
            PurchaseDiscountPercent: 0,
            LeaderAbilities: LeaderAbility.None,
            Wards: [])));

        Assert.That(provider.GetUnitModifier(new UnitModifierDefinitionId("G000UM0003")), Is.EqualTo(new UnitModifierDefinition(
            new UnitModifierDefinitionId("G000UM0003"),
            DescriptionTextId: new GlobalTextId("x000tgc003"),
            MoveAbility: UnitMoveAbility.None,
            MaxMovement: default,
            Leadership: default,
            VisionRadius: default,
            Hitpoints: default,
            RegenerationPercent: default,
            Armor: default,
            Initiative: default,
            Damage: new UnitStatModifier(0, 10),
            Accuracy: default,
            DrainPercent: 0,
            FastRetreat: false,
            PurchaseDiscountPercent: 0,
            LeaderAbilities: LeaderAbility.None,
            Wards: [])));

        Assert.That(provider.GetUnitModifier(new UnitModifierDefinitionId("G000UM0002")), Is.EqualTo(new UnitModifierDefinition(
            new UnitModifierDefinitionId("G000UM0002"),
            DescriptionTextId: new GlobalTextId("x000tgc002"),
            MoveAbility: UnitMoveAbility.None,
            MaxMovement: default,
            Leadership: default,
            VisionRadius: default,
            Hitpoints: default,
            RegenerationPercent: default,
            Armor: default,
            Initiative: new UnitStatModifier(0, 10),
            Damage: default,
            Accuracy: default,
            DrainPercent: 0,
            FastRetreat: false,
            PurchaseDiscountPercent: 0,
            LeaderAbilities: LeaderAbility.None,
            Wards: [])));

        Assert.That(provider.GetUnitModifier(new UnitModifierDefinitionId("G000UM0036")), Is.EqualTo(new UnitModifierDefinition(
            new UnitModifierDefinitionId("G000UM0036"),
            DescriptionTextId: new GlobalTextId("x000tgc036"),
            MoveAbility: UnitMoveAbility.None,
            MaxMovement: default,
            Leadership: default,
            VisionRadius: default,
            Hitpoints: default,
            RegenerationPercent: default,
            Armor: default,
            Initiative: default,
            Damage: default,
            Accuracy: new UnitStatModifier(0, 33),
            DrainPercent: 0,
            FastRetreat: false,
            PurchaseDiscountPercent: 0,
            LeaderAbilities: LeaderAbility.None,
            Wards: [])));

        Assert.That(provider.GetUnitModifier(new UnitModifierDefinitionId("G000UM2503")), Is.EqualTo(new UnitModifierDefinition(
            new UnitModifierDefinitionId("G000UM2503"),
            DescriptionTextId: new GlobalTextId("x000tg6000"),
            MoveAbility: UnitMoveAbility.None,
            MaxMovement: new UnitStatModifier(0, 20),
            Leadership: default,
            VisionRadius: default,
            Hitpoints: default,
            RegenerationPercent: default,
            Armor: default,
            Initiative: default,
            Damage: default,
            Accuracy: default,
            DrainPercent: 0,
            FastRetreat: false,
            PurchaseDiscountPercent: 0,
            LeaderAbilities: LeaderAbility.None,
            Wards: [])));

        Assert.That(provider.GetUnitModifier(new UnitModifierDefinitionId("G000UM7546")), Is.EqualTo(new UnitModifierDefinition(
            new UnitModifierDefinitionId("G000UM7546"),
            DescriptionTextId: new GlobalTextId("x160tgc474"),
            MoveAbility: UnitMoveAbility.None,
            MaxMovement: default,
            Leadership: default,
            VisionRadius: default,
            Hitpoints: default,
            RegenerationPercent: default,
            Armor: default,
            Initiative: default,
            Damage: default,
            Accuracy: default,
            DrainPercent: 0,
            FastRetreat: true,
            PurchaseDiscountPercent: 0,
            LeaderAbilities: LeaderAbility.None,
            Wards: [])));

        Assert.That(provider.GetUnitModifier(new UnitModifierDefinitionId("G000UM7548")), Is.EqualTo(new UnitModifierDefinition(
            new UnitModifierDefinitionId("G000UM7548"),
            DescriptionTextId: new GlobalTextId("x160tgc488"),
            MoveAbility: UnitMoveAbility.None,
            MaxMovement: default,
            Leadership: default,
            VisionRadius: default,
            Hitpoints: default,
            RegenerationPercent: default,
            Armor: default,
            Initiative: default,
            Damage: default,
            Accuracy: default,
            DrainPercent: 0,
            FastRetreat: false,
            PurchaseDiscountPercent: 10,
            LeaderAbilities: LeaderAbility.None,
            Wards: [])));

        Assert.That(provider.GetUnitModifier(new UnitModifierDefinitionId("G000UM9031")), Is.EqualTo(new UnitModifierDefinition(
            new UnitModifierDefinitionId("G000UM9031"),
            DescriptionTextId: new GlobalTextId("x000tg6031"),
            MoveAbility: UnitMoveAbility.None,
            MaxMovement: default,
            Leadership: new UnitStatModifier(1, 0),
            VisionRadius: default,
            Hitpoints: default,
            RegenerationPercent: default,
            Armor: default,
            Initiative: default,
            Damage: default,
            Accuracy: default,
            DrainPercent: 0,
            FastRetreat: false,
            PurchaseDiscountPercent: 0,
            LeaderAbilities: LeaderAbility.None,
            Wards: [])));

        Assert.That(provider.GetUnitModifier(new UnitModifierDefinitionId("G000UM4015")), Is.EqualTo(new UnitModifierDefinition(
            new UnitModifierDefinitionId("G000UM4015"),
            DescriptionTextId: new GlobalTextId("x000tg6000"),
            MoveAbility: UnitMoveAbility.None,
            MaxMovement: default,
            Leadership: default,
            VisionRadius: new UnitStatModifier(3, 0),
            Hitpoints: default,
            RegenerationPercent: default,
            Armor: default,
            Initiative: default,
            Damage: default,
            Accuracy: default,
            DrainPercent: 0,
            FastRetreat: false,
            PurchaseDiscountPercent: 0,
            LeaderAbilities: LeaderAbility.None,
            Wards: [])));

        Assert.That(provider.GetUnitModifier(new UnitModifierDefinitionId("G000UM9035")), Is.EqualTo(new UnitModifierDefinition(
            new UnitModifierDefinitionId("G000UM9035"),
            DescriptionTextId: new GlobalTextId("x000tg6035"),
            MoveAbility: UnitMoveAbility.None,
            MaxMovement: default,
            Leadership: default,
            VisionRadius: default,
            Hitpoints: default,
            RegenerationPercent: 10,
            Armor: default,
            Initiative: default,
            Damage: default,
            Accuracy: default,
            DrainPercent: 0,
            FastRetreat: false,
            PurchaseDiscountPercent: 0,
            LeaderAbilities: LeaderAbility.None,
            Wards: [])));
    }

    [Test]
    public void GetUnitModifierDrainTest()
    {
        GameDefinitionProvider provider = CreateProvider();

        Assert.That(provider.GetUnitModifier(new UnitModifierDefinitionId("G000UM2317")), Is.EqualTo(new UnitModifierDefinition(
            new UnitModifierDefinitionId("G000UM2317"),
            DescriptionTextId: new GlobalTextId("x000tg8817"),
            MoveAbility: UnitMoveAbility.None,
            MaxMovement: default,
            Leadership: default,
            VisionRadius: default,
            Hitpoints: default,
            RegenerationPercent: default,
            Armor: default,
            Initiative: default,
            Damage: default,
            Accuracy: default,
            DrainPercent: 25,
            FastRetreat: false,
            PurchaseDiscountPercent: 0,
            LeaderAbilities: LeaderAbility.None,
            Wards: [])));
    }

    [Test]
    public void GetUnitModifierSeveralElementsTest()
    {
        GameDefinitionProvider provider = CreateProvider();

        Assert.That(provider.GetUnitModifier(new UnitModifierDefinitionId("G000UM2306")), Is.EqualTo(new UnitModifierDefinition(
            new UnitModifierDefinitionId("G000UM2306"),
            DescriptionTextId: new GlobalTextId("x000tg6000"),
            MoveAbility: UnitMoveAbility.None,
            MaxMovement: default,
            Leadership: default,
            VisionRadius: default,
            Hitpoints: default,
            RegenerationPercent: default,
            Armor: default,
            Initiative: new UnitStatModifier(0, 25),
            Damage: new UnitStatModifier(0, 40),
            Accuracy: default,
            DrainPercent: 0,
            FastRetreat: false,
            PurchaseDiscountPercent: 0,
            LeaderAbilities: LeaderAbility.None,
            Wards: [])));
    }

    [Test]
    public void GetUnitModifierHitpointsTest()
    {
        GameDefinitionProvider provider = CreateProvider();

        Assert.That(provider.GetUnitModifier(new UnitModifierDefinitionId("G000UM9001")), Is.EqualTo(new UnitModifierDefinition(
            new UnitModifierDefinitionId("G000UM9001"),
            DescriptionTextId: new GlobalTextId("x000tg6001"),
            MoveAbility: UnitMoveAbility.None,
            MaxMovement: default,
            Leadership: default,
            VisionRadius: default,
            Hitpoints: new UnitStatModifier(0, 20),
            RegenerationPercent: default,
            Armor: default,
            Initiative: default,
            Damage: default,
            Accuracy: default,
            DrainPercent: 0,
            FastRetreat: false,
            PurchaseDiscountPercent: 0,
            LeaderAbilities: LeaderAbility.None,
            Wards: [])));

        Assert.That(provider.GetUnitModifier(new UnitModifierDefinitionId("G000UM9002")), Is.EqualTo(new UnitModifierDefinition(
            new UnitModifierDefinitionId("G000UM9002"),
            DescriptionTextId: new GlobalTextId("x000tg6002"),
            MoveAbility: UnitMoveAbility.None,
            MaxMovement: default,
            Leadership: default,
            VisionRadius: default,
            Hitpoints: new UnitStatModifier(0, -20),
            RegenerationPercent: default,
            Armor: default,
            Initiative: default,
            Damage: default,
            Accuracy: default,
            DrainPercent: 0,
            FastRetreat: false,
            PurchaseDiscountPercent: 0,
            LeaderAbilities: LeaderAbility.None,
            Wards: [])));

        Assert.That(provider.GetUnitModifier(new UnitModifierDefinitionId("G000UM7545")), Is.EqualTo(new UnitModifierDefinition(
            new UnitModifierDefinitionId("G000UM7545"),
            DescriptionTextId: new GlobalTextId("x160tgc216"),
            MoveAbility: UnitMoveAbility.None,
            MaxMovement: default,
            Leadership: default,
            VisionRadius: default,
            Hitpoints: new UnitStatModifier(50, 0),
            RegenerationPercent: default,
            Armor: default,
            Initiative: default,
            Damage: default,
            Accuracy: default,
            DrainPercent: 0,
            FastRetreat: false,
            PurchaseDiscountPercent: 0,
            LeaderAbilities: LeaderAbility.None,
            Wards: [])));
    }

    [Test]
    public void GetUnitModifierNotFoundTest()
    {
        GameDefinitionProvider provider = CreateProvider();

        Assert.Throws<KeyNotFoundException>(() => provider.GetUnitModifier(new UnitModifierDefinitionId("INVALID")));
    }

    [Test]
    public void GetItemTest()
    {
        GameDefinitionProvider provider = CreateProvider();

        ItemDefinition actual = provider.GetItem(new ItemDefinitionId("G000IG0001"));

        Assert.That(actual, Is.EqualTo(new ItemDefinition(
            new ItemDefinitionId("G000IG0001"),
            new GlobalTextId("x000tg8001"),
            new GlobalTextId("x000tg8051"),
            ItemCategory.PotionRevive,
            new WorldResourceSet(400, 0, 0, 0, 0, 0),
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: new ReviveItemEffect(),
            AnimationUnitId: new UnitDefinitionId("g000uu0150"))));
    }

    [Test]
    public void GetOrbItemTest()
    {
        GameDefinitionProvider provider = CreateProvider();

        ItemDefinition actual = provider.GetItem(new ItemDefinitionId("G000IG9016"));

        Assert.That(actual, Is.EqualTo(new ItemDefinition(
            new ItemDefinitionId("G000IG9016"),
            new GlobalTextId("x000tg9116"),
            new GlobalTextId("x000tg9216"),
            ItemCategory.Orb,
            new WorldResourceSet(400, 0, 0, 0, 0, 0),
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: new AttackItemEffect(new UnitAttackDefinitionId("g000aa9116")),
            AnimationUnitId: new UnitDefinitionId("g000uu0012"))));

        UnitAttackDefinition attack = provider.GetAttack(new UnitAttackDefinitionId("g000aa9116"));

        Assert.That(attack, Is.EqualTo(new UnitAttackDefinition(
            Id: new UnitAttackDefinitionId("g000aa9116"),
            Type: UnitAttackType.Heal,
            Source: UnitAttackSource.Life,
            Initiative: 50,
            Accuracy: 100,
            Reach: UnitAttackReach.Any,
            Damage: 0,
            Heal: 100,
            Level: 0,
            IsInfinite: true,
            SummonUnitIds: [],
            SmallTransformUnitId: null,
            LargeTransformUnitId: null,
            HasCriticalHit: false,
            AlternativeAttackId: null,
            WardModifierIds: [])));
    }

    [Test]
    public void GetSummonTalismanItemTest()
    {
        GameDefinitionProvider provider = CreateProvider();

        ItemDefinition actual = provider.GetItem(new ItemDefinitionId("G000IG9101"));

        Assert.That(actual, Is.EqualTo(new ItemDefinition(
            new ItemDefinitionId("G000IG9101"),
            new GlobalTextId("x000tg9301"),
            new GlobalTextId("x000tg9201"),
            ItemCategory.Talisman,
            new WorldResourceSet(200, 0, 0, 0, 0, 0),
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: new AttackItemEffect(new UnitAttackDefinitionId("g000aa9101")),
            AnimationUnitId: new UnitDefinitionId("g000uu0153"))));

        UnitAttackDefinition attack = provider.GetAttack(new UnitAttackDefinitionId("g000aa9101"));

        Assert.That(attack, Is.EqualTo(new UnitAttackDefinition(
            Id: new UnitAttackDefinitionId("g000aa9101"),
            Type: UnitAttackType.Summon,
            Source: UnitAttackSource.Life,
            Initiative: 50,
            Accuracy: 100,
            Reach: UnitAttackReach.Any,
            Damage: 0,
            Heal: 0,
            Level: 0,
            IsInfinite: true,
            SummonUnitIds: [new UnitDefinitionId("g000uu0001")],
            SmallTransformUnitId: null,
            LargeTransformUnitId: null,
            HasCriticalHit: false,
            AlternativeAttackId: null,
            WardModifierIds: [])));
    }

    [Test]
    public void GetScrollItemTest()
    {
        GameDefinitionProvider provider = CreateProvider();

        ItemDefinition actual = provider.GetItem(new ItemDefinitionId("G000IG5007"));

        Assert.That(actual, Is.EqualTo(new ItemDefinition(
            new ItemDefinitionId("G000IG5007"),
            new GlobalTextId("x000tg8907"),
            new GlobalTextId("x000tg4207"),
            ItemCategory.Scroll,
            new WorldResourceSet(400, 0, 0, 0, 0, 0),
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: new SpellItemEffect(new WorldSpellDefinitionId("g000ss0007"), WorldResourceSet.Empty),
            AnimationUnitId: null)));

        WorldSpellDefinition spell = provider.GetSpell(new WorldSpellDefinitionId("g000ss0007"));

        Assert.That(spell, Is.EqualTo(new WorldSpellDefinition(
            new WorldSpellDefinitionId("g000ss0007"),
            new GlobalTextId("x000tg4007"),
            new GlobalTextId("x000tg4207"),
            new GlobalTextId("x000tgc007"),
            WorldRace.Human,
            Level: 2,
            CastingCost: new WorldResourceSet(0, 0, 0, 200, 0, 0),
            PurchaseCost: new WorldResourceSet(400, 0, 0, 0, 0, 0),
            AreaRadius: 0,
            Effect: new WorldHealSpellEffect(Hitpoints: 30),
            AiType: WorldAiSpellType.All)));
    }

    [Test]
    public void GetWandItemTest()
    {
        GameDefinitionProvider provider = CreateProvider();

        ItemDefinition actual = provider.GetItem(new ItemDefinitionId("G000IG6001"));

        Assert.That(actual, Is.EqualTo(new ItemDefinition(
            new ItemDefinitionId("G000IG6001"),
            new GlobalTextId("x000tg8601"),
            new GlobalTextId("x000tg4204"),
            ItemCategory.Wand,
            new WorldResourceSet(600, 0, 0, 0, 0, 0),
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: new SpellItemEffect(
                new WorldSpellDefinitionId("g000ss0004"),
                new WorldResourceSet(0, 0, 0, 100, 0, 0)),
            AnimationUnitId: null)));
    }

    [Test]
    public void GetWeaponArtifactItemTest()
    {
        GameDefinitionProvider provider = CreateProvider();

        ItemDefinition actual = provider.GetItem(new ItemDefinitionId("G000IG3019"));

        Assert.That(actual, Is.EqualTo(new ItemDefinition(
            new ItemDefinitionId("G000IG3019"),
            new GlobalTextId("x000tg8319"),
            new GlobalTextId("x000tg8369"),
            ItemCategory.Weapon,
            new WorldResourceSet(1500, 0, 0, 0, 0, 0),
            EquipmentModifierId: null,
            EquipmentAttackId: new UnitAttackDefinitionId("g000aa9019"),
            Effect: null,
            AnimationUnitId: null)));

        UnitAttackDefinition attack = provider.GetAttack(new UnitAttackDefinitionId("g000aa9019"));

        Assert.That(attack, Is.EqualTo(new UnitAttackDefinition(
            Id: new UnitAttackDefinitionId("g000aa9019"),
            Type: UnitAttackType.Poison,
            Source: UnitAttackSource.Death,
            Initiative: 30,
            Accuracy: 80,
            Reach: UnitAttackReach.Any,
            Damage: 20,
            Heal: 0,
            Level: 0,
            IsInfinite: true,
            SummonUnitIds: [],
            SmallTransformUnitId: null,
            LargeTransformUnitId: null,
            HasCriticalHit: false,
            AlternativeAttackId: null,
            WardModifierIds: [])));
    }

    [Test]
    public void GetWeaponModifierItemTest()
    {
        GameDefinitionProvider provider = CreateProvider();

        ItemDefinition actual = provider.GetItem(new ItemDefinitionId("G000IG3017"));

        Assert.That(actual, Is.EqualTo(new ItemDefinition(
            new ItemDefinitionId("G000IG3017"),
            new GlobalTextId("x000tg8317"),
            new GlobalTextId("x000tg8367"),
            ItemCategory.Weapon,
            new WorldResourceSet(1500, 0, 0, 0, 0, 0),
            EquipmentModifierId: new UnitModifierDefinitionId("g000um2317"),
            EquipmentAttackId: null,
            Effect: null,
            AnimationUnitId: null)));
    }

    [Test]
    public void GetItemNotFoundTest()
    {
        GameDefinitionProvider provider = CreateProvider();

        Assert.Throws<KeyNotFoundException>(() => provider.GetItem(new ItemDefinitionId("INVALID")));
    }

    [Test]
    public void GetUnitLeaderAbilitiesTest()
    {
        GameDefinitionProvider provider = CreateProvider();

        Assert.That(provider.GetUnit(new UnitDefinitionId("g000uu0021")).LeaderAbilities,
            Is.EqualTo(LeaderAbility.WandScrollUse | LeaderAbility.OrbUse));

        Assert.That(provider.GetUnit(new UnitDefinitionId("G000UU0001")).LeaderAbilities, Is.EqualTo(LeaderAbility.None));
    }

    [Test]
    public void GetUnitMoveAbilityTest()
    {
        GameDefinitionProvider provider = CreateProvider();

        Assert.That(provider.GetUnit(new UnitDefinitionId("g000uu0096")).MoveAbility, Is.EqualTo(UnitMoveAbility.Flying));
        Assert.That(provider.GetUnit(new UnitDefinitionId("g000uu0019")).MoveAbility, Is.EqualTo(UnitMoveAbility.Flying));
        Assert.That(provider.GetUnit(new UnitDefinitionId("g000uu0044")).MoveAbility, Is.EqualTo(UnitMoveAbility.None));
        Assert.That(provider.GetUnit(new UnitDefinitionId("G000UU0001")).MoveAbility, Is.EqualTo(UnitMoveAbility.None));
    }

    [Test]
    public void GetUnitWardsForSeveralImmunitiesTest()
    {
        GameDefinitionProvider provider = CreateProvider();

        UnitWard[] expectedArchlichWards =
        [
            new UnitAttackSourceWard(UnitAttackSource.Death, UnitWardCategory.Always)
        ];

        UnitWard[] expectedWraithWards =
        [
            new UnitAttackSourceWard(UnitAttackSource.Weapon, UnitWardCategory.Always),
            new UnitAttackSourceWard(UnitAttackSource.Death, UnitWardCategory.Always)
        ];

        Assert.That(provider.GetUnit(new UnitDefinitionId("g000uu0176")).Wards, Is.EqualTo(expectedArchlichWards));
        Assert.That(provider.GetUnit(new UnitDefinitionId("g000uu0085")).Wards, Is.EqualTo(expectedWraithWards));
    }

    [Test]
    public void GetUnitModifierWardsTest()
    {
        GameDefinitionProvider provider = CreateProvider();

        UnitModifierDefinition actual = provider.GetUnitModifier(new UnitModifierDefinitionId("g000um0001"));

        UnitWard[] expected =
        [
            new UnitAttackSourceWard(UnitAttackSource.Air, UnitWardCategory.Once)
        ];

        Assert.That(actual.Wards, Is.EqualTo(expected));
    }

    [Test]
    public void GetPlayerBuildingTest()
    {
        GameDefinitionProvider provider = CreateProvider();

        WorldPlayerBuildingDefinition actual = provider.GetPlayerBuilding(new WorldPlayerBuildingDefinitionId("G000BB0001"));

        Assert.That(actual, Is.EqualTo(new WorldPlayerBuildingDefinition(
            Id: new WorldPlayerBuildingDefinitionId("G000BB0001"),
            NameTextId: new GlobalTextId("x000tg1001"),
            DescriptionTextId: new GlobalTextId("x000tg1101"),
            RequiredId: null,
            Category: WorldPlayerBuildingCategory.Unit,
            UnitBranch: UnitBranch.Fighter,
            Level: 2,
            Cost: new WorldResourceSet(200, 0, 0, 0, 0, 0),
            UnitIds: [new UnitDefinitionId("g000uu0002")]
        )));
    }

    [Test]
    public void GetGuildPlayerBuildingTest()
    {
        GameDefinitionProvider provider = CreateProvider();

        WorldPlayerBuildingDefinition actual = provider.GetPlayerBuilding(new WorldPlayerBuildingDefinitionId("G000BB0025"));

        Assert.That(actual, Is.EqualTo(new WorldPlayerBuildingDefinition(
            Id: new WorldPlayerBuildingDefinitionId("G000BB0025"),
            NameTextId: new GlobalTextId("x000tg1025"),
            DescriptionTextId: new GlobalTextId("x000tg1125"),
            RequiredId: null,
            Category: WorldPlayerBuildingCategory.Guild,
            UnitBranch: null,
            Level: 2,
            Cost: new WorldResourceSet(150, 0, 0, 0, 0, 0),
            UnitIds: []
        )));
    }

    [Test]
    public void GetSpellPlayerBuildingTest()
    {
        GameDefinitionProvider provider = CreateProvider();

        WorldPlayerBuildingDefinition actual = provider.GetPlayerBuilding(new WorldPlayerBuildingDefinitionId("G000BB0024"));

        Assert.That(actual, Is.EqualTo(new WorldPlayerBuildingDefinition(
            Id: new WorldPlayerBuildingDefinitionId("G000BB0024"),
            NameTextId: new GlobalTextId("x000tg1024"),
            DescriptionTextId: new GlobalTextId("x000tg1124"),
            RequiredId: null,
            Category: WorldPlayerBuildingCategory.Spell,
            UnitBranch: null,
            Level: 2,
            Cost: new WorldResourceSet(150, 0, 0, 0, 0, 0),
            UnitIds: []
        )));
    }

    [Test]
    public void GetPlayerBuildingUnitsTest()
    {
        GameDefinitionProvider provider = CreateProvider();

        Assert.That(
            provider.GetPlayerBuilding(new WorldPlayerBuildingDefinitionId("G000BB0102")).UnitIds,
            Is.EqualTo([new UnitDefinitionId("g000uu8015")]));

        Assert.That(provider.GetPlayerBuilding(new WorldPlayerBuildingDefinitionId("G000BB0023")).UnitIds, Is.Empty);
    }

    [Test]
    public void GetSideshowPlayerBuildingTest()
    {
        GameDefinitionProvider provider = CreateProvider();

        WorldPlayerBuildingDefinition actual = provider.GetPlayerBuilding(new WorldPlayerBuildingDefinitionId("G000BB0022"));

        Assert.That(actual, Is.EqualTo(new WorldPlayerBuildingDefinition(
            Id: new WorldPlayerBuildingDefinitionId("G000BB0022"),
            NameTextId: new GlobalTextId("x000tg1022"),
            DescriptionTextId: new GlobalTextId("x000tg1122"),
            RequiredId: new WorldPlayerBuildingDefinitionId("g000bb0012"),
            Category: WorldPlayerBuildingCategory.Unit,
            UnitBranch: UnitBranch.Sideshow,
            Level: 3,
            Cost: new WorldResourceSet(750, 0, 0, 0, 0, 0),
            UnitIds: [new UnitDefinitionId("g000uu0018")]
        )));
    }

    [Test]
    public void GetPlayerBuildingNotFoundTest()
    {
        GameDefinitionProvider provider = CreateProvider();

        Assert.Throws<KeyNotFoundException>(() => provider.GetPlayerBuilding(new WorldPlayerBuildingDefinitionId("INVALID")));
    }

    [Test]
    public void GetCityTest()
    {
        GameDefinitionProvider provider = CreateProvider();

        WorldCityDefinition actual = provider.GetCity(new WorldCityDefinitionId(WorldRace.Human, 1));

        Assert.That(actual, Is.EqualTo(new WorldCityDefinition(
            Id: new WorldCityDefinitionId(WorldRace.Human, 1),
            Level: 1,
            Regeneration: 10,
            VisionRadius: 5,
            UpgradeCost: new WorldResourceSet(150, 0, 0, 0, 0, 0),
            ProtectionModifierId: new UnitModifierDefinitionId("g000um3001")
        )));
    }

    [TestCase(1, 150)]
    [TestCase(2, 250)]
    [TestCase(3, 500)]
    [TestCase(4, 750)]
    [TestCase(5, 0)]
    public void GetCityUpgradeCostTest(int level, int gold)
    {
        GameDefinitionProvider provider = CreateProvider();

        WorldCityDefinition actual = provider.GetCity(new WorldCityDefinitionId(WorldRace.Human, level));

        Assert.That(actual.UpgradeCost, Is.EqualTo(new WorldResourceSet(gold, 0, 0, 0, 0, 0)));
    }

    [Test]
    public void GetCityNotFoundTest()
    {
        GameDefinitionProvider provider = CreateProvider();

        Assert.Throws<KeyNotFoundException>(() => provider.GetCity(new WorldCityDefinitionId(WorldRace.Human, 99)));
    }

    [Test]
    public void GetRaceTest()
    {
        GameDefinitionProvider provider = CreateProvider();

        WorldRaceDefinition actual = provider.GetRace(WorldRace.Human);

        Assert.That(actual.Race, Is.EqualTo(WorldRace.Human));
        Assert.That(actual.Income, Is.EqualTo(new WorldResourceSet(100, 0, 0, 25, 0, 0)));
        Assert.That(actual.NativeTerrain, Is.EqualTo(WorldMapTerrain.Human));
        Assert.That(actual.IsPlayable, Is.True);
        Assert.That(actual.Cities, Is.EquivalentTo([
            new WorldCityDefinitionId(WorldRace.Human, 1),
            new WorldCityDefinitionId(WorldRace.Human, 2),
            new WorldCityDefinitionId(WorldRace.Human, 3),
            new WorldCityDefinitionId(WorldRace.Human, 4),
            new WorldCityDefinitionId(WorldRace.Human, 5)
        ]));
        Assert.That(actual.Units, Is.EquivalentTo([
            new UnitDefinitionId("g000uu0001"),
            new UnitDefinitionId("g000uu0006"),
            new UnitDefinitionId("g000uu0008"),
            new UnitDefinitionId("g000uu0011"),
            new UnitDefinitionId("g000uu0018")
        ]));
        Assert.That(actual.Leaders, Is.EquivalentTo([
            new UnitDefinitionId("g000uu0019"),
            new UnitDefinitionId("g000uu0020"),
            new UnitDefinitionId("g000uu0021"),
            new UnitDefinitionId("g000uu0022"),
            new UnitDefinitionId("g000uu0023")
        ]));
    }

    [Test]
    public void RaceLeaderNamesTest()
    {
        GameDefinitionProvider provider = CreateProvider();

        Assert.That(FormatLeaderNameCounts(provider), Is.EqualTo([
            "Human 30 male, 15 female",
            "Undead 29 male, 31 female",
            "Heretic 40 male, 20 female",
            "Dwarf 33 male, 8 female",
            "Neutral 1 male, 1 female",
            "Elf 23 male, 20 female"
        ]));
    }

    [Test]
    public void RaceLeaderNameSampleTest()
    {
        GameDefinitionProvider provider = CreateProvider();

        Assert.That(provider.GetRace(WorldRace.Human).MaleLeaderNames[0], Is.EqualTo("Konrad"));
        Assert.That(provider.GetRace(WorldRace.Human).FemaleLeaderNames[0], Is.EqualTo("Zolian"));
        Assert.That(provider.GetRace(WorldRace.Neutral).MaleLeaderNames, Is.EqualTo(["Neutrals"]));
        Assert.That(provider.GetRace(WorldRace.Neutral).FemaleLeaderNames, Is.EqualTo(["Neutrals"]));
    }

    [Test]
    public void GetRaceNotFoundTest()
    {
        GameDefinitionProvider provider = CreateProvider();

        Assert.Throws<KeyNotFoundException>(() => provider.GetRace((WorldRace)99));
    }

    [Test]
    public void GetSpellTest()
    {
        GameDefinitionProvider provider = CreateProvider();

        Assert.That(provider.GetSpell(new WorldSpellDefinitionId("g000ss0001")), Is.EqualTo(
            new WorldSpellDefinition(
                new WorldSpellDefinitionId("g000ss0001"),
                new GlobalTextId("x000tg4001"),
                new GlobalTextId("x000tg4201"),
                new GlobalTextId("x000tgc001"),
                WorldRace.Human,
                Level: 1,
                CastingCost: new WorldResourceSet(0, 0, 0, 100, 0, 0),
                PurchaseCost: new WorldResourceSet(200, 0, 0, 0, 0, 0),
                AreaRadius: 0,
                Effect: new WorldBoostSpellEffect(new UnitModifierDefinitionId("g000um0001")),
                AiType: WorldAiSpellType.None)));
    }

    [Test]
    public void GetSpellAreaRadiusTest()
    {
        GameDefinitionProvider provider = CreateProvider();

        Assert.That(provider.GetSpell(new WorldSpellDefinitionId("g000ss0082")), Is.EqualTo(
            new WorldSpellDefinition(
                new WorldSpellDefinitionId("g000ss0082"),
                new GlobalTextId("x000tg4082"),
                new GlobalTextId("x000tg4282"),
                new GlobalTextId("x000tgc082"),
                WorldRace.Human,
                Level: 4,
                CastingCost: new WorldResourceSet(0, 0, 150, 300, 150, 0),
                PurchaseCost: new WorldResourceSet(1200, 0, 0, 0, 0, 0),
                AreaRadius: 2,
                Effect: new WorldHealSpellEffect(Hitpoints: 60),
                AiType: WorldAiSpellType.All)));
    }

    [Test]
    public void GetSpellNoAreaTest()
    {
        GameDefinitionProvider provider = CreateProvider();

        Assert.That(provider.GetSpell(new WorldSpellDefinitionId("g000ss0083")), Is.EqualTo(
            new WorldSpellDefinition(
                new WorldSpellDefinitionId("g000ss0083"),
                new GlobalTextId("x000tg4083"),
                new GlobalTextId("x000tg4283"),
                new GlobalTextId("x000tgc083"),
                WorldRace.Human,
                Level: 4,
                CastingCost: new WorldResourceSet(0, 0, 200, 400, 200, 0),
                PurchaseCost: new WorldResourceSet(1600, 0, 0, 0, 0, 0),
                AreaRadius: null,
                Effect: new WorldUnfogSpellEffect(),
                AiType: WorldAiSpellType.All)));
    }

    [Test]
    public void GetSpellResearchCostsTest()
    {
        GameDefinitionProvider provider = CreateProvider();

        IReadOnlyDictionary<WorldSpellDefinitionId, WorldResourceSet> researchable = provider.GetSpellResearchCosts(WorldRace.Human);

        Assert.That(FormatSpellResearchCosts(researchable), Is.EqualTo([
            "g000ss0001 G0:DM0:RM0:LM200:IM0:GM0",
            "g000ss0002 G0:DM0:RM0:LM200:IM0:GM0",
            "g000ss0003 G0:DM0:RM0:LM200:IM0:GM0",
            "g000ss0004 G0:DM0:RM0:LM200:IM0:GM0",
            "g000ss0005 G0:DM0:RM0:LM200:IM0:GM0",
            "g000ss0006 G0:DM0:RM0:LM400:IM0:GM0",
            "g000ss0007 G0:DM0:RM0:LM400:IM0:GM0",
            "g000ss0008 G0:DM0:RM0:LM400:IM0:GM0",
            "g000ss0009 G0:DM0:RM0:LM400:IM0:GM0",
            "g000ss0010 G0:DM0:RM0:LM400:IM0:GM0",
            "g000ss0011 G0:DM0:RM450:LM450:IM0:GM0",
            "g000ss0012 G0:DM0:RM450:LM450:IM0:GM0",
            "g000ss0013 G0:DM0:RM0:LM450:IM450:GM0",
            "g000ss0014 G0:DM0:RM450:LM450:IM0:GM0",
            "g000ss0015 G0:DM0:RM400:LM800:IM400:GM0",
            "g000ss0016 G0:DM0:RM300:LM800:IM600:GM0",
            "g000ss0017 G0:DM0:RM400:LM800:IM400:GM0",
            "g000ss0018 G0:DM600:RM350:LM1250:IM300:GM0",
            "g000ss0019 G0:DM400:RM350:LM1250:IM500:GM0",
            "g000ss0020 G0:DM300:RM500:LM1250:IM450:GM0",
            "g000ss0081 G0:DM0:RM680:LM680:IM0:GM0",
            "g000ss0082 G0:DM0:RM600:LM1200:IM600:GM0",
            "g000ss0083 G0:DM0:RM800:LM1600:IM800:GM0",
            "g000ss0084 G0:DM800:RM900:LM2500:IM800:GM0"
        ]));
    }

    [Test]
    public void GetSpellResearchCostsUnknownRaceTest()
    {
        GameDefinitionProvider provider = CreateProvider();

        Assert.That(provider.GetSpellResearchCosts((WorldRace)99), Is.Empty);
    }

    [Test]
    public void GetLeaderAbilityUnitModifierTest()
    {
        GameDefinitionProvider provider = CreateProvider();

        Assert.That(provider.GetUnitModifier(new UnitModifierDefinitionId("G000UM4001")), Is.EqualTo(new UnitModifierDefinition(
            new UnitModifierDefinitionId("G000UM4001"),
            DescriptionTextId: new GlobalTextId("x000tg6000"),
            MoveAbility: UnitMoveAbility.None,
            MaxMovement: default,
            Leadership: default,
            VisionRadius: default,
            Hitpoints: default,
            RegenerationPercent: default,
            Armor: default,
            Initiative: default,
            Damage: default,
            Accuracy: default,
            DrainPercent: 0,
            FastRetreat: false,
            PurchaseDiscountPercent: 0,
            LeaderAbilities: LeaderAbility.WeaponArmorUse,
            Wards: [])));
    }

    [Test]
    public void GetLordTest()
    {
        GameDefinitionProvider provider = CreateProvider();

        Assert.That(provider.GetLord(new LordDefinitionId("G000LR0001")), Is.EqualTo(
            new LordDefinition(
                new LordDefinitionId("G000LR0001"),
                WorldRace.Human,
                LordCategory.Warrior,
                new GlobalTextId("x000tg3001"),
                new GlobalTextId("x000tg3101"),
                [
                    new WorldPlayerBuildingDefinitionId("G000BB0001"),
                    new WorldPlayerBuildingDefinitionId("G000BB0002"),
                    new WorldPlayerBuildingDefinitionId("G000BB0003"),
                    new WorldPlayerBuildingDefinitionId("G000BB0004"),
                    new WorldPlayerBuildingDefinitionId("G000BB0005"),
                    new WorldPlayerBuildingDefinitionId("G000BB0006"),
                    new WorldPlayerBuildingDefinitionId("G000BB0007"),
                    new WorldPlayerBuildingDefinitionId("G000BB0008"),
                    new WorldPlayerBuildingDefinitionId("G000BB0009"),
                    new WorldPlayerBuildingDefinitionId("G000BB0010"),
                    new WorldPlayerBuildingDefinitionId("G000BB0011"),
                    new WorldPlayerBuildingDefinitionId("G000BB0012"),
                    new WorldPlayerBuildingDefinitionId("G000BB0013"),
                    new WorldPlayerBuildingDefinitionId("G000BB0014"),
                    new WorldPlayerBuildingDefinitionId("G000BB0015"),
                    new WorldPlayerBuildingDefinitionId("G000BB0016"),
                    new WorldPlayerBuildingDefinitionId("G000BB0017"),
                    new WorldPlayerBuildingDefinitionId("G000BB0018"),
                    new WorldPlayerBuildingDefinitionId("G000BB0019"),
                    new WorldPlayerBuildingDefinitionId("G000BB0020"),
                    new WorldPlayerBuildingDefinitionId("G000BB0021"),
                    new WorldPlayerBuildingDefinitionId("G000BB0022"),
                    new WorldPlayerBuildingDefinitionId("G000BB0023"),
                    new WorldPlayerBuildingDefinitionId("G000BB0024"),
                    new WorldPlayerBuildingDefinitionId("G000BB0025")
                ])));

        Assert.That(provider.GetLord(new LordDefinitionId("G000LR0002")), Is.EqualTo(
            new LordDefinition(
                new LordDefinitionId("G000LR0002"),
                WorldRace.Human,
                LordCategory.Guildmaster,
                new GlobalTextId("x000tg3002"),
                new GlobalTextId("x000tg3102"),
                [
                    new WorldPlayerBuildingDefinitionId("G000BB0001"),
                    new WorldPlayerBuildingDefinitionId("G000BB0003"),
                    new WorldPlayerBuildingDefinitionId("G000BB0005"),
                    new WorldPlayerBuildingDefinitionId("G000BB0006"),
                    new WorldPlayerBuildingDefinitionId("G000BB0007"),
                    new WorldPlayerBuildingDefinitionId("G000BB0008"),
                    new WorldPlayerBuildingDefinitionId("G000BB0009"),
                    new WorldPlayerBuildingDefinitionId("G000BB0010"),
                    new WorldPlayerBuildingDefinitionId("G000BB0011"),
                    new WorldPlayerBuildingDefinitionId("G000BB0012"),
                    new WorldPlayerBuildingDefinitionId("G000BB0013"),
                    new WorldPlayerBuildingDefinitionId("G000BB0014"),
                    new WorldPlayerBuildingDefinitionId("G000BB0015"),
                    new WorldPlayerBuildingDefinitionId("G000BB0016"),
                    new WorldPlayerBuildingDefinitionId("G000BB0017"),
                    new WorldPlayerBuildingDefinitionId("G000BB0002"),
                    new WorldPlayerBuildingDefinitionId("G000BB0004"),
                    new WorldPlayerBuildingDefinitionId("G000BB0024"),
                    new WorldPlayerBuildingDefinitionId("G000BB0023"),
                    new WorldPlayerBuildingDefinitionId("G000BB0022"),
                    new WorldPlayerBuildingDefinitionId("G000BB0021"),
                    new WorldPlayerBuildingDefinitionId("G000BB0020"),
                    new WorldPlayerBuildingDefinitionId("G000BB0018"),
                    new WorldPlayerBuildingDefinitionId("G000BB0019"),
                    new WorldPlayerBuildingDefinitionId("G000BB0025")
                ])));

        Assert.That(provider.GetLord(new LordDefinitionId("G000LR0003")), Is.EqualTo(
            new LordDefinition(
                new LordDefinitionId("G000LR0003"),
                WorldRace.Human,
                LordCategory.Mage,
                new GlobalTextId("x000tg3003"),
                new GlobalTextId("x000tg3103"),
                [
                    new WorldPlayerBuildingDefinitionId("G000BB0001"),
                    new WorldPlayerBuildingDefinitionId("G000BB0002"),
                    new WorldPlayerBuildingDefinitionId("G000BB0003"),
                    new WorldPlayerBuildingDefinitionId("G000BB0004"),
                    new WorldPlayerBuildingDefinitionId("G000BB0005"),
                    new WorldPlayerBuildingDefinitionId("G000BB0006"),
                    new WorldPlayerBuildingDefinitionId("G000BB0007"),
                    new WorldPlayerBuildingDefinitionId("G000BB0008"),
                    new WorldPlayerBuildingDefinitionId("G000BB0009"),
                    new WorldPlayerBuildingDefinitionId("G000BB0010"),
                    new WorldPlayerBuildingDefinitionId("G000BB0011"),
                    new WorldPlayerBuildingDefinitionId("G000BB0012"),
                    new WorldPlayerBuildingDefinitionId("G000BB0013"),
                    new WorldPlayerBuildingDefinitionId("G000BB0014"),
                    new WorldPlayerBuildingDefinitionId("G000BB0015"),
                    new WorldPlayerBuildingDefinitionId("G000BB0016"),
                    new WorldPlayerBuildingDefinitionId("G000BB0017"),
                    new WorldPlayerBuildingDefinitionId("G000BB0019"),
                    new WorldPlayerBuildingDefinitionId("G000BB0020"),
                    new WorldPlayerBuildingDefinitionId("G000BB0025"),
                    new WorldPlayerBuildingDefinitionId("G000BB0022"),
                    new WorldPlayerBuildingDefinitionId("G000BB0018"),
                    new WorldPlayerBuildingDefinitionId("G000BB0023"),
                    new WorldPlayerBuildingDefinitionId("G000BB0021"),
                    new WorldPlayerBuildingDefinitionId("G000BB0024")
                ])));
    }

    [Test]
    public void GetLeaderUpgradesTest()
    {
        GameDefinitionProvider provider = CreateProvider();

        IReadOnlyList<LeaderUpgradeDefinition> upgrades = provider.GetLeaderUpgrades(new LordDefinitionId("G000LR0001"));

        Assert.That(FormatLeaderUpgrades(upgrades), Is.EqualTo([
            "2 90 g000um4001 x000tga001",
            "2 85 g000um4002 x000tga002",
            "2 85 g000um4003 x000tga003",
            "2 80 g000um4004 x000tga004",
            "3 99 g000um4006 x000tga006",
            "3 95 g000um4005 x000tga005",
            "3 85 g000um4007 x000tga007",
            "4 85 g000um4009 x000tga009",
            "4 60 g000um4008 x000tga008",
            "5 95 g000um4010 x000tga010",
            "5 70 g000um4011 x000tga011",
            "6 99 g000um4013 x000tga013",
            "6 80 g000um4012 x000tga012",
            "7 60 g000um4014 x000tga014",
            "7 60 g000um4015 x000tga015",
            "8 99 g000um4101 x000tga006",
            "8 80 g000um4016 x000tga016",
            "8 80 g000um4017 x000tga017",
            "9 80 g000um4018 x000tga018",
            "10 99 g000um4102 x000tga006",
            "10 80 g000um4019 x000tga019",
            "11 80 g000um4020 x000tga020",
            "12 80 g000um4021 x000tga021",
            "13 60 g000um4022 x000tga022",
            "14 80 g000um4023 x000tga023",
            "15 80 g000um4024 x000tga024",
            "15 80 g000um4103 x160tgc253"
        ]));
    }

    [Test]
    public void GetLeaderUpgradesUnknownLordTest()
    {
        GameDefinitionProvider provider = CreateProvider();

        Assert.That(provider.GetLeaderUpgrades(new LordDefinitionId("INVALID")), Is.Empty);
    }

    [Test]
    public void GetSpellEffectsTest()
    {
        GlobalDatabase globalDb = GlobalDatabaseLoader.Load(RiseOfTheElves.GlobalDatabasePath);
        GameDefinitionProvider provider = new(globalDb);

        Dictionary<string, int> countsByEffect = [];

        foreach (Db.Spell spell in globalDb.Spells.Values)
        {
            string name = provider.GetSpell(new WorldSpellDefinitionId(spell.SpellId)).Effect.GetType().Name;

            countsByEffect[name] = countsByEffect.GetValueOrDefault(name) + 1;
        }

        List<string> rows = [.. countsByEffect.OrderBy(entry => entry.Key).Select(entry => $"{entry.Key} {entry.Value}")];

        Assert.That(rows, Is.EqualTo([
            "WorldAttackSpellEffect 33",
            "WorldBoostSpellEffect 25",
            "WorldChangeTerrainSpellEffect 2",
            "WorldFogSpellEffect 4",
            "WorldHealSpellEffect 7",
            "WorldInvisibilitySpellEffect 2",
            "WorldRemoveRodSpellEffect 1",
            "WorldRestoreMovementSpellEffect 4",
            "WorldSummonStackSpellEffect 19",
            "WorldUnfogSpellEffect 3",
            "WorldWeakenSpellEffect 20"
        ]));
    }

    [Test]
    public void GetSpellAreasTest()
    {
        GlobalDatabase globalDb = GlobalDatabaseLoader.Load(RiseOfTheElves.GlobalDatabasePath);
        GameDefinitionProvider provider = new(globalDb);

        List<string> radiusRows = [];
        List<Db.Id> allegianceScopedSpells = [];
        int singleTileCount = 0;

        foreach (Db.Spell spell in globalDb.Spells.Values)
        {
            int? areaRadius = provider.GetSpell(new WorldSpellDefinitionId(spell.SpellId)).AreaRadius;

            if (areaRadius is null)
            {
                allegianceScopedSpells.Add(spell.SpellId);
            }
            else if (areaRadius == 0)
            {
                singleTileCount++;
            }
            else
            {
                radiusRows.Add($"{spell.SpellId} radius {areaRadius}");
            }
        }

        Assert.That(radiusRows, Is.EqualTo([
            "g000ss0009 radius 2",
            "g000ss0030 radius 2",
            "g000ss0052 radius 3",
            "g000ss0068 radius 3",
            "g000ss0074 radius 4",
            "g000ss0081 radius 1",
            "g000ss0082 radius 2",
            "g000ss0085 radius 2",
            "g000ss0086 radius 2",
            "g000ss0089 radius 2",
            "g000ss0090 radius 2",
            "g000ss0091 radius 3",
            "g000ss0093 radius 1",
            "g000ss0094 radius 3",
            "g000ss0096 radius 3",
            "g000ss0107 radius 1",
            "g000ss0110 radius 1",
            "g000ss0112 radius 2",
            "g000ss0120 radius 3"
        ]));

        Assert.That(allegianceScopedSpells, Is.EqualTo([
            new Db.Id("g000ss0083"),
            new Db.Id("g000ss0084"),
            new Db.Id("g000ss0087"),
            new Db.Id("g000ss0088"),
            new Db.Id("g000ss0092"),
            new Db.Id("g000ss0095")
        ]));

        Assert.That(singleTileCount, Is.EqualTo(95));
    }

    [Test]
    public void GetThiefActionTest()
    {
        GameDefinitionProvider provider = CreateProvider();

        Assert.That(FormatThiefActions(provider), Is.EqualTo([
            "PoisonStack 90 X000TG0100 X000TG0120",
            "Spy 100 X000TG0101 X000TG0121",
            "StealItem 80 X000TG0102 X000TG0122",
            "Assassinate 70 X000TG0103 X000TG0123",
            "CounterfeitOrders 90 X000TG0104 X000TG0124",
            "Duel 100 X000TG0105 X000TG0125",
            "PoisonCity 90 X000TG0106 X000TG0126",
            "StealSpell 80 X000TG0107 X000TG0127",
            "Bribe 100 X000TG0108 X000TG0128",
            "StealGold 80 X000TG0109 X000TG0129",
            "StealMerchant 60 X000TG0110 X000TG0130",
            "StealMage 60 X000TG0111 X000TG0131",
            "SpyRuin 90 X000TG0112 X000TG0132",
            "RiotCity 100 X000TG0113 X000TG0133"
        ]));
    }

    [Test]
    public void PegasusKnightUpgradesTest()
    {
        GameDefinitionProvider provider = CreateProvider();
        IUnitStatsProvider stats = new UnitStatsProvider(provider, new GlobalVariables(), GameScenarioProvider.Empty.Definition);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, TestHelpers.SimpleMap(3, 1)) with
        {
            LordId = new LordDefinitionId("G000LR0001")
        };
        UnitDefinition leaderDefinition = provider.GetUnit(new UnitDefinitionId("g000uu0019"));
        WorldUnit leader = TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition) with { Level = 15 };

        IReadOnlyList<LeaderUpgradeDefinition> upgrades = LeaderUpgradeCalculator.GetAvailableUpgrades(player, leader, provider, stats);

        Assert.That(FormatLeaderUpgrades(upgrades), Is.EqualTo([
            "2 85 g000um4002 x000tga002",
            "2 85 g000um4003 x000tga003",
            "2 80 g000um4004 x000tga004",
            "3 99 g000um4006 x000tga006",
            "3 95 g000um4005 x000tga005",
            "3 85 g000um4007 x000tga007",
            "4 85 g000um4009 x000tga009",
            "4 60 g000um4008 x000tga008",
            "5 95 g000um4010 x000tga010",
            "5 70 g000um4011 x000tga011",
            "6 99 g000um4013 x000tga013",
            "6 80 g000um4012 x000tga012",
            "7 60 g000um4014 x000tga014",
            "7 60 g000um4015 x000tga015",
            "8 99 g000um4101 x000tga006",
            "8 80 g000um4016 x000tga016",
            "8 80 g000um4017 x000tga017",
            "9 80 g000um4018 x000tga018",
            "10 99 g000um4102 x000tga006",
            "10 80 g000um4019 x000tga019",
            "12 80 g000um4021 x000tga021",
            "13 60 g000um4022 x000tga022",
            "14 80 g000um4023 x000tga023",
            "15 80 g000um4024 x000tga024",
            "15 80 g000um4103 x160tgc253"
        ]));
    }
}
