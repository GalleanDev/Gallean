using Gallean.Entities.GameData;
using Gallean.Tests.Common;

namespace Gallean.Engine.Tests;

public sealed class ItemDefinitionTests
{
    private static GameDefinitionProvider CreateProvider()
    {
        GlobalDatabase globalDb = GlobalDatabaseLoader.Load(RiseOfTheElves.GlobalDatabasePath);

        return new GameDefinitionProvider(globalDb);
    }

    [Test]
    public void WorldAndBattleBonusTest()
    {
        GameDefinitionProvider definitions = CreateProvider();
        UnitStatsProvider statsProvider = new(definitions, new GlobalVariables(), GameScenarioProvider.Empty.Definition);

        ItemDefinition weapon = definitions.GetItem(new ItemDefinitionId("G000IG3006"));
        UnitModifierDefinitionId itemModifier = Assert.NotNull(weapon.EquipmentModifierId);

        UnitDefinition leaderDefinition = definitions.GetUnit(new UnitDefinitionId("g000uu0019"));
        WorldUnit bareLeader = new(new WorldUnitId("bareLeader"), leaderDefinition.Id, leaderDefinition.MaxHitpoints, 0, leaderDefinition.Level, [], null, false);
        WorldUnit equippedLeader = bareLeader with { Modifiers = [UnitModifier.Permanent(itemModifier)] };

        UnitStats expectedBare = new(
            MaxHitpoints: 150,
            Armor: 0,
            Regeneration: 5,
            Initiative: 50,
            Damage: 50,
            Accuracy: 80,
            ExperienceKilled: 60,
            ExperienceNextLevel: 150);

        UnitStats expectedEquipped = expectedBare with
        {
            Initiative = 62,
            Damage = 70
        };

        Assert.That(statsProvider.GetWorldUnitStats(bareLeader), Is.EqualTo(expectedBare));
        Assert.That(statsProvider.GetWorldUnitStats(equippedLeader), Is.EqualTo(expectedEquipped));

        BattleUnit battleLeader = new(
            new BattleUnitId(0),
            new WorldUnitId("bareLeader"),
            leaderDefinition.Id,
            BattleSide.Attacker,
            leaderDefinition.MaxHitpoints,
            0,
            0,
            leaderDefinition.Level,
            [UnitModifier.Permanent(itemModifier)],
            [],
            null,
            [],
            Name: null,
            IsTypeLocked: false);
        BattleState battleState = new BattleStateBuilder(definitions).Add(battleLeader).Build(BattleModifiers.None);

        Assert.That(statsProvider.GetBattleUnitStats(battleState, battleLeader.Id), Is.EqualTo(expectedEquipped));
    }

    [Test]
    public void DrainBonusTest()
    {
        GameDefinitionProvider definitions = CreateProvider();
        UnitStatsProvider statsProvider = new(definitions, new GlobalVariables(), GameScenarioProvider.Empty.Definition);

        ItemDefinition unholyDagger = definitions.GetItem(new ItemDefinitionId("G000IG3017"));
        UnitModifierDefinitionId itemModifier = Assert.NotNull(unholyDagger.EquipmentModifierId);

        UnitDefinition leaderDefinition = definitions.GetUnit(new UnitDefinitionId("g000uu0019"));
        WorldUnit bareLeader = new(new WorldUnitId("bareLeader"), leaderDefinition.Id, leaderDefinition.MaxHitpoints, 0, leaderDefinition.Level, [], null, false);
        WorldUnit equippedLeader = bareLeader with { Modifiers = [UnitModifier.Permanent(itemModifier)] };

        UnitStats expectedStats = new(
            MaxHitpoints: 150,
            Armor: 0,
            Regeneration: 5,
            Initiative: 50,
            Damage: 50,
            Accuracy: 80,
            ExperienceKilled: 60,
            ExperienceNextLevel: 150);

        Assert.That(statsProvider.GetWorldUnitStats(bareLeader), Is.EqualTo(expectedStats));
        Assert.That(statsProvider.GetWorldUnitStats(equippedLeader), Is.EqualTo(expectedStats));

        BattleUnit bareBattleLeader = new(
            new BattleUnitId(0),
            new WorldUnitId("bareLeader"),
            leaderDefinition.Id,
            BattleSide.Attacker,
            leaderDefinition.MaxHitpoints,
            0,
            0,
            leaderDefinition.Level,
            [],
            [],
            null,
            [],
            Name: null,
            IsTypeLocked: false);
        BattleUnit equippedBattleLeader = bareBattleLeader with { Id = new BattleUnitId(1), Modifiers = [UnitModifier.Permanent(itemModifier)] };
        BattleState battleState = new BattleStateBuilder(definitions).Add(bareBattleLeader).Add(equippedBattleLeader).Build(BattleModifiers.None);

        Assert.That(statsProvider.GetBattleAttackStats(battleState, bareBattleLeader.Id, leaderDefinition.Attack), Is.EqualTo(new UnitAttackStats(80, 50, 0)));
        Assert.That(statsProvider.GetBattleAttackStats(battleState, equippedBattleLeader.Id, leaderDefinition.Attack), Is.EqualTo(new UnitAttackStats(80, 50, 25)));
    }

    [Test]
    public void MaxMovementBonusTest()
    {
        GameDefinitionProvider definitions = CreateProvider();
        UnitStatsProvider statsProvider = new(definitions, new GlobalVariables(), GameScenarioProvider.Empty.Definition);

        ItemDefinition boots = definitions.GetItem(new ItemDefinitionId("G000IG8003"));
        UnitModifierDefinitionId itemModifier = Assert.NotNull(boots.EquipmentModifierId);

        UnitDefinition leaderDefinition = definitions.GetUnit(new UnitDefinitionId("g000uu0020"));

        WorldStack bareStack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            new WorldPlayerId(1),
            new WorldPosition(0, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 0);

        WorldStack equippedStack = bareStack with
        {
            Units = new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader2"), leaderDefinition) with { Modifiers = [UnitModifier.Permanent(itemModifier)] }, GridPosition.FrontBottom, false)
                .Build()
        };

        Assert.That(statsProvider.GetWorldStackStats(bareStack), Is.EqualTo(new StackStats(MaxMovementPoints: 35, Leadership: 4, VisionRadius: 6, Negotiate: 10)));
        Assert.That(statsProvider.GetWorldStackStats(equippedStack), Is.EqualTo(new StackStats(MaxMovementPoints: 42, Leadership: 4, VisionRadius: 6, Negotiate: 10)));
    }

    [Test]
    public void PotionEffectsTest()
    {
        GameDefinitionProvider definitions = CreateProvider();

        ItemDefinition boostPotion = definitions.GetItem(new ItemDefinitionId("G000IG0015"));
        Assert.That(boostPotion, Is.EqualTo(new ItemDefinition(
            new ItemDefinitionId("G000IG0015"),
            new GlobalTextId("x000tg8015"),
            new GlobalTextId("x000tg8065"),
            ItemCategory.PotionTemporaryBoost,
            new WorldResourceSet(450, 0, 0, 0, 0, 0),
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: new TemporaryBoostItemEffect(new UnitModifierDefinitionId("g000um2015")),
            AnimationUnitId: new UnitDefinitionId("g000uu0161"))));

        UnitModifierDefinition boostModifier = definitions.GetUnitModifier(new UnitModifierDefinitionId("g000um2015"));
        Assert.That(boostModifier, Is.EqualTo(new UnitModifierDefinition(
            new UnitModifierDefinitionId("g000um2015"),
            DescriptionTextId: new GlobalTextId("x000tg6000"),
            MoveAbility: UnitMoveAbility.None,
            MaxMovement: default,
            Leadership: default,
            VisionRadius: default,
            Hitpoints: default,
            RegenerationPercent: default,
            Armor: default,
            Initiative: default,
            Damage: new UnitStatModifier(0, 30),
            Accuracy: default,
            DrainPercent: 0,
            FastRetreat: false,
            PurchaseDiscountPercent: 0,
            LeaderAbilities: LeaderAbility.None,
            Wards: [])));

        ItemDefinition permanentPotion = definitions.GetItem(new ItemDefinitionId("G000IG0016"));
        Assert.That(permanentPotion, Is.EqualTo(new ItemDefinition(
            new ItemDefinitionId("G000IG0016"),
            new GlobalTextId("x000tg8016"),
            new GlobalTextId("x000tg8066"),
            ItemCategory.PotionPermanentBoost,
            new WorldResourceSet(800, 0, 0, 0, 0, 0),
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: new PermanentBoostItemEffect(new UnitModifierDefinitionId("g000um2016")),
            AnimationUnitId: new UnitDefinitionId("g000uu0161"))));

        UnitModifierDefinition permanentModifier = definitions.GetUnitModifier(new UnitModifierDefinitionId("g000um2016"));
        Assert.That(permanentModifier, Is.EqualTo(new UnitModifierDefinition(
            new UnitModifierDefinitionId("g000um2016"),
            DescriptionTextId: new GlobalTextId("x000tg6000"),
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

        ItemDefinition wardingPotion = definitions.GetItem(new ItemDefinitionId("G000IG0024"));
        Assert.That(wardingPotion, Is.EqualTo(new ItemDefinition(
            new ItemDefinitionId("G000IG0024"),
            new GlobalTextId("x000tg8024"),
            new GlobalTextId("x000tg8074"),
            ItemCategory.PotionTemporaryBoost,
            new WorldResourceSet(400, 0, 0, 0, 0, 0),
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: new TemporaryBoostItemEffect(new UnitModifierDefinitionId("g000um2024")),
            AnimationUnitId: new UnitDefinitionId("g000uu0161"))));

        UnitModifierDefinition wardingModifier = definitions.GetUnitModifier(new UnitModifierDefinitionId("g000um2024"));
        Assert.That(wardingModifier.Wards, Is.EqualTo(new UnitWard[] { new UnitAttackSourceWard(UnitAttackSource.Fire, UnitWardCategory.Once) }));
        Assert.That(wardingModifier with { Wards = [] }, Is.EqualTo(new UnitModifierDefinition(
            new UnitModifierDefinitionId("g000um2024"),
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
            LeaderAbilities: LeaderAbility.None,
            Wards: [])));
    }
}
