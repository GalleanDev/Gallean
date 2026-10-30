namespace Gallean.Engine.Tests;

public sealed class StackAutoFitCalculatorTests
{
    private static StackAutoFitCalculator.Result CheckCalculate(
        UnitGrid<WorldUnit> stackUnits,
        UnitGrid<WorldUnit> garrisonUnits,
        WorldInventory stackInventory,
        WorldInventory garrisonInventory,
        LeaderEquipment equipment,
        int garrisonCapacity,
        IGameDefinitionProvider definitions)
    {
        StackAutoFitCalculator.Result result1 = StackAutoFitCalculator.Calculate(
            stackUnits,
            garrisonUnits,
            stackInventory,
            garrisonInventory,
            equipment,
            garrisonCapacity,
            definitions);

        StackAutoFitCalculator.Result result2 = StackAutoFitCalculator.Calculate(
            stackUnits,
            garrisonUnits,
            stackInventory,
            garrisonInventory,
            equipment,
            garrisonCapacity,
            definitions);

        Assert.That(result1, Is.EqualTo(result2));

        return result1;
    }

    private static UnitDefinition CreateUnitDefinition(
        string id,
        int gold,
        bool isLarge,
        UnitAttackReach reach)
    {
        return TestHelpers.SimpleUnitDefinition(id, maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId(id), initiative: 50, damage: 10, accuracy: 80, reach: reach),
            IsLarge = isLarge,
            PurchaseCost = new WorldResourceSet(gold, 0, 0, 0, 0, 0)
        };
    }

    private static UnitDefinition CreateLeaderDefinition(LeaderAbility abilities)
    {
        return CreateUnitDefinition("leader", 100, false, UnitAttackReach.Adjacent) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Leadership = 4,
            LeaderAbilities = abilities
        };
    }

    private static ItemDefinition CreateItem(
        string id,
        ItemCategory category,
        int value,
        UnitModifierDefinitionId? modifier)
    {
        return TestHelpers.SimpleItemDefinition(new ItemDefinitionId(id), category) with
        {
            Value = new WorldResourceSet(value, 0, 0, 0, 0, 0),
            EquipmentModifierId = modifier
        };
    }

    [Test]
    public void LargeUnitBeatsSmallTest()
    {
        UnitDefinition leaderDefinition = CreateUnitDefinition("leader", 100, false, UnitAttackReach.Adjacent) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Leadership = 3
        };
        UnitDefinition smallDefinition = CreateUnitDefinition("small", 60, false, UnitAttackReach.Adjacent);
        UnitDefinition largeDefinition = CreateUnitDefinition("large", 130, true, UnitAttackReach.Any);
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .AddUnit(smallDefinition)
            .AddUnit(largeDefinition)
            .Build();

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 100, 0, 1, [], null, false);
        WorldUnit small1 = new(new WorldUnitId("small1"), smallDefinition.Id, 100, 0, 1, [], null, false);
        WorldUnit small2 = new(new WorldUnitId("small2"), smallDefinition.Id, 100, 0, 1, [], null, false);
        WorldUnit large = new(new WorldUnitId("large"), largeDefinition.Id, 100, 0, 1, [], null, false);

        UnitGrid<WorldUnit> stack = new UnitGrid<WorldUnit>.Builder()
            .Place(leader, GridPosition.FrontBottom, false)
            .Build();

        UnitGrid<WorldUnit> garrison = new UnitGrid<WorldUnit>.Builder()
            .Place(small1, GridPosition.FrontBottom, false)
            .Place(small2, GridPosition.FrontCenter, false)
            .Place(large, GridPosition.FrontTop, true)
            .Build();

        StackAutoFitCalculator.Result result = CheckCalculate(
            stack,
            garrison,
            WorldInventory.Empty,
            WorldInventory.Empty,
            LeaderEquipment.Empty,
            GridPosition.Count,
            definitions);

        Assert.That(result, Is.EqualTo(new StackAutoFitCalculator.Result(
            StackUnits: new UnitGrid<WorldUnit>.Builder()
                .Place(large, GridPosition.FrontCenter, true)
                .Place(leader, GridPosition.FrontBottom, false)
                .Build(),
            GarrisonUnits: new UnitGrid<WorldUnit>.Builder()
                .Place(small1, GridPosition.FrontCenter, false)
                .Place(small2, GridPosition.FrontBottom, false)
                .Build(),
            StackInventory: WorldInventory.Empty,
            GarrisonInventory: WorldInventory.Empty,
            Equipment: LeaderEquipment.Empty
        )));
    }

    [Test]
    public void VillageCapacityTest()
    {
        UnitDefinition leaderDefinition = CreateUnitDefinition("leader", 100, false, UnitAttackReach.Adjacent) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Leadership = 3
        };
        UnitDefinition largeDefinition = CreateUnitDefinition("large", 50, true, UnitAttackReach.Adjacent);
        UnitDefinition priceyDefinition = CreateUnitDefinition("pricey", 300, false, UnitAttackReach.Adjacent);
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .AddUnit(largeDefinition)
            .AddUnit(priceyDefinition)
            .Build();

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 100, 0, 1, [], null, false);
        WorldUnit large = new(new WorldUnitId("large"), largeDefinition.Id, 100, 0, 1, [], null, false);
        WorldUnit pricey = new(new WorldUnitId("pricey"), priceyDefinition.Id, 100, 0, 1, [], null, false);

        UnitGrid<WorldUnit> stack = new UnitGrid<WorldUnit>.Builder()
            .Place(leader, GridPosition.FrontBottom, false)
            .Place(large, GridPosition.FrontCenter, true)
            .Build();

        UnitGrid<WorldUnit> garrison = new UnitGrid<WorldUnit>.Builder()
            .Place(pricey, GridPosition.FrontBottom, false)
            .Build();

        StackAutoFitCalculator.Result roomy = CheckCalculate(
            stack,
            garrison,
            WorldInventory.Empty,
            WorldInventory.Empty,
            LeaderEquipment.Empty,
            GridPosition.Count,
            definitions);

        Assert.That(roomy, Is.EqualTo(new StackAutoFitCalculator.Result(
            StackUnits: new UnitGrid<WorldUnit>.Builder()
                .Place(leader, GridPosition.FrontCenter, false)
                .Place(pricey, GridPosition.FrontBottom, false)
                .Build(),
            GarrisonUnits: new UnitGrid<WorldUnit>.Builder()
                .Place(large, GridPosition.FrontCenter, true)
                .Build(),
            StackInventory: WorldInventory.Empty,
            GarrisonInventory: WorldInventory.Empty,
            Equipment: LeaderEquipment.Empty
        )));

        StackAutoFitCalculator.Result cramped = CheckCalculate(
            stack,
            garrison,
            WorldInventory.Empty,
            WorldInventory.Empty,
            LeaderEquipment.Empty,
            1,
            definitions);

        Assert.That(cramped, Is.EqualTo(new StackAutoFitCalculator.Result(
            StackUnits: new UnitGrid<WorldUnit>.Builder()
                .Place(large, GridPosition.FrontCenter, true)
                .Place(leader, GridPosition.FrontBottom, false)
                .Build(),
            GarrisonUnits: new UnitGrid<WorldUnit>.Builder()
                .Place(pricey, GridPosition.FrontCenter, false)
                .Build(),
            StackInventory: WorldInventory.Empty,
            GarrisonInventory: WorldInventory.Empty,
            Equipment: LeaderEquipment.Empty
        )));
    }

    [Test]
    public void ReachAwareFormationTest()
    {
        UnitDefinition leaderDefinition = CreateUnitDefinition("leader", 100, false, UnitAttackReach.Adjacent) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Leadership = 4
        };
        UnitDefinition adjacentDefinition = CreateUnitDefinition("adjacent", 50, false, UnitAttackReach.Adjacent);
        UnitDefinition rangedDefinition = CreateUnitDefinition("ranged", 50, false, UnitAttackReach.Any);
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .AddUnit(adjacentDefinition)
            .AddUnit(rangedDefinition)
            .Build();

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 100, 0, 1, [], null, false);
        WorldUnit adjacent = new(new WorldUnitId("adjacent"), adjacentDefinition.Id, 100, 0, 1, [], null, false);
        WorldUnit ranged = new(new WorldUnitId("ranged"), rangedDefinition.Id, 100, 0, 1, [], null, false);

        UnitGrid<WorldUnit> stack = new UnitGrid<WorldUnit>.Builder()
            .Place(leader, GridPosition.FrontBottom, false)
            .Build();

        UnitGrid<WorldUnit> garrison = new UnitGrid<WorldUnit>.Builder()
            .Place(adjacent, GridPosition.FrontBottom, false)
            .Place(ranged, GridPosition.FrontCenter, false)
            .Build();

        StackAutoFitCalculator.Result result = CheckCalculate(
            stack,
            garrison,
            WorldInventory.Empty,
            WorldInventory.Empty,
            LeaderEquipment.Empty,
            GridPosition.Count,
            definitions);

        Assert.That(result, Is.EqualTo(new StackAutoFitCalculator.Result(
            StackUnits: new UnitGrid<WorldUnit>.Builder()
                .Place(adjacent, GridPosition.FrontCenter, false)
                .Place(leader, GridPosition.FrontBottom, false)
                .Place(ranged, GridPosition.BackCenter, false)
                .Build(),
            GarrisonUnits: UnitGrid<WorldUnit>.Empty,
            StackInventory: WorldInventory.Empty,
            GarrisonInventory: WorldInventory.Empty,
            Equipment: LeaderEquipment.Empty
        )));
    }

    [Test]
    public void LeadershipLimitsSelectionTest()
    {
        UnitDefinition leaderDefinition = CreateUnitDefinition("leader", 100, false, UnitAttackReach.Adjacent) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Leadership = 2
        };
        UnitDefinition specialDefinition = CreateUnitDefinition("special", 1000, false, UnitAttackReach.Adjacent);
        UnitDefinition soldierDefinition = CreateUnitDefinition("soldier", 60, false, UnitAttackReach.Adjacent);
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .AddUnit(specialDefinition)
            .AddUnit(soldierDefinition)
            .Build();

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 100, 0, 1, [], null, false);
        WorldUnit special = new(new WorldUnitId("special"), specialDefinition.Id, 100, 0, 1, [], null, false);
        WorldUnit soldier1 = new(new WorldUnitId("soldier1"), soldierDefinition.Id, 100, 0, 1, [], null, false);
        WorldUnit soldier2 = new(new WorldUnitId("soldier2"), soldierDefinition.Id, 100, 0, 1, [], null, false);

        UnitGrid<WorldUnit> stack = new UnitGrid<WorldUnit>.Builder()
            .Place(leader, GridPosition.FrontBottom, false)
            .Build();

        UnitGrid<WorldUnit> garrison = new UnitGrid<WorldUnit>.Builder()
            .Place(special, GridPosition.FrontBottom, false)
            .Place(soldier1, GridPosition.FrontCenter, false)
            .Place(soldier2, GridPosition.FrontTop, false)
            .Build();

        StackAutoFitCalculator.Result result = CheckCalculate(
            stack,
            garrison,
            WorldInventory.Empty,
            WorldInventory.Empty,
            LeaderEquipment.Empty,
            GridPosition.Count,
            definitions);

        Assert.That(result, Is.EqualTo(new StackAutoFitCalculator.Result(
            StackUnits: new UnitGrid<WorldUnit>.Builder()
                .Place(leader, GridPosition.FrontCenter, false)
                .Place(special, GridPosition.FrontBottom, false)
                .Build(),
            GarrisonUnits: new UnitGrid<WorldUnit>.Builder()
                .Place(soldier1, GridPosition.FrontCenter, false)
                .Place(soldier2, GridPosition.FrontBottom, false)
                .Build(),
            StackInventory: WorldInventory.Empty,
            GarrisonInventory: WorldInventory.Empty,
            Equipment: LeaderEquipment.Empty
        )));
    }

    [Test]
    public void ExperiencedDeadUnitTest()
    {
        UnitDefinition leaderDefinition = CreateUnitDefinition("leader", 100, false, UnitAttackReach.Adjacent) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Leadership = 2
        };
        UnitDefinition soldierDefinition = CreateUnitDefinition("soldier", 60, false, UnitAttackReach.Adjacent);
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .AddUnit(soldierDefinition)
            .Build();

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 100, 0, 1, [], null, false);
        WorldUnit experiencedDead = new(new WorldUnitId("experiencedDead"), soldierDefinition.Id, 0, 50, 1, [], null, false);
        WorldUnit fresh = new(new WorldUnitId("fresh"), soldierDefinition.Id, 100, 0, 1, [], null, false);

        UnitGrid<WorldUnit> stack = new UnitGrid<WorldUnit>.Builder()
            .Place(leader, GridPosition.FrontBottom, false)
            .Build();

        UnitGrid<WorldUnit> garrison = new UnitGrid<WorldUnit>.Builder()
            .Place(experiencedDead, GridPosition.FrontBottom, false)
            .Place(fresh, GridPosition.FrontCenter, false)
            .Build();

        StackAutoFitCalculator.Result result = CheckCalculate(
            stack,
            garrison,
            WorldInventory.Empty,
            WorldInventory.Empty,
            LeaderEquipment.Empty,
            GridPosition.Count,
            definitions);

        Assert.That(result, Is.EqualTo(new StackAutoFitCalculator.Result(
            StackUnits: new UnitGrid<WorldUnit>.Builder()
                .Place(leader, GridPosition.FrontCenter, false)
                .Place(experiencedDead, GridPosition.FrontBottom, false)
                .Build(),
            GarrisonUnits: new UnitGrid<WorldUnit>.Builder()
                .Place(fresh, GridPosition.FrontCenter, false)
                .Build(),
            StackInventory: WorldInventory.Empty,
            GarrisonInventory: WorldInventory.Empty,
            Equipment: LeaderEquipment.Empty
        )));
    }

    [Test]
    public void GuardianCannotBeSelectedTest()
    {
        UnitDefinition leaderDefinition = CreateUnitDefinition("leader", 100, false, UnitAttackReach.Adjacent) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Leadership = 2
        };
        UnitDefinition guardianDefinition = CreateUnitDefinition("guardian", 1000, false, UnitAttackReach.Adjacent) with
        {
            Category = UnitCategory.Guardian
        };
        UnitDefinition soldierDefinition = CreateUnitDefinition("soldier", 60, false, UnitAttackReach.Adjacent);
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .AddUnit(guardianDefinition)
            .AddUnit(soldierDefinition)
            .Build();

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 100, 0, 1, [], null, false);
        WorldUnit guardian = new(new WorldUnitId("guardian"), guardianDefinition.Id, 100, 0, 1, [], null, false);
        WorldUnit soldier = new(new WorldUnitId("soldier"), soldierDefinition.Id, 100, 0, 1, [], null, false);

        UnitGrid<WorldUnit> stack = new UnitGrid<WorldUnit>.Builder()
            .Place(leader, GridPosition.FrontBottom, false)
            .Build();

        UnitGrid<WorldUnit> garrison = new UnitGrid<WorldUnit>.Builder()
            .Place(guardian, GridPosition.FrontBottom, false)
            .Place(soldier, GridPosition.FrontCenter, false)
            .Build();

        StackAutoFitCalculator.Result result = CheckCalculate(
            stack,
            garrison,
            WorldInventory.Empty,
            WorldInventory.Empty,
            LeaderEquipment.Empty,
            GridPosition.Count,
            definitions);

        Assert.That(result, Is.EqualTo(new StackAutoFitCalculator.Result(
            StackUnits: new UnitGrid<WorldUnit>.Builder()
                .Place(leader, GridPosition.FrontCenter, false)
                .Place(soldier, GridPosition.FrontBottom, false)
                .Build(),
            GarrisonUnits: new UnitGrid<WorldUnit>.Builder()
                .Place(guardian, GridPosition.FrontCenter, false)
                .Build(),
            StackInventory: WorldInventory.Empty,
            GarrisonInventory: WorldInventory.Empty,
            Equipment: LeaderEquipment.Empty
        )));
    }

    [Test]
    public void BetterLeaderSelectedTest()
    {
        UnitDefinition leaderDefinition = CreateUnitDefinition("leader", 100, false, UnitAttackReach.Adjacent) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Leadership = 3
        };
        UnitDefinition garrisonLeaderDefinition = CreateUnitDefinition("garrison-leader", 1000, false, UnitAttackReach.Adjacent) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Leadership = 6
        };

        UnitDefinition soldierDefinition = CreateUnitDefinition("soldier", 60, false, UnitAttackReach.Adjacent);
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .AddUnit(garrisonLeaderDefinition)
            .AddUnit(soldierDefinition)
            .Build();

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 100, 0, 1, [], null, false);
        WorldUnit garrisonLeader = new(new WorldUnitId("garrisonLeader"), garrisonLeaderDefinition.Id, 100, 0, 1, [], null, false);
        WorldUnit soldier1 = new(new WorldUnitId("soldier1"), soldierDefinition.Id, 100, 0, 1, [], null, false);
        WorldUnit soldier2 = new(new WorldUnitId("soldier2"), soldierDefinition.Id, 100, 0, 1, [], null, false);

        UnitGrid<WorldUnit> stack = new UnitGrid<WorldUnit>.Builder()
            .Place(leader, GridPosition.FrontBottom, false)
            .Build();

        UnitGrid<WorldUnit> garrison = new UnitGrid<WorldUnit>.Builder()
            .Place(garrisonLeader, GridPosition.FrontBottom, false)
            .Place(soldier1, GridPosition.FrontCenter, false)
            .Place(soldier2, GridPosition.FrontTop, false)
            .Build();

        StackAutoFitCalculator.Result result = CheckCalculate(
            stack,
            garrison,
            WorldInventory.Empty,
            WorldInventory.Empty,
            LeaderEquipment.Empty,
            GridPosition.Count,
            definitions);

        Assert.That(result, Is.EqualTo(new StackAutoFitCalculator.Result(
            StackUnits: new UnitGrid<WorldUnit>.Builder()
                .Place(garrisonLeader, GridPosition.FrontCenter, false)
                .Place(soldier1, GridPosition.FrontBottom, false)
                .Place(soldier2, GridPosition.FrontTop, false)
                .Build(),
            GarrisonUnits: new UnitGrid<WorldUnit>.Builder()
                .Place(leader, GridPosition.FrontCenter, false)
                .Build(),
            StackInventory: WorldInventory.Empty,
            GarrisonInventory: WorldInventory.Empty,
            Equipment: LeaderEquipment.Empty
        )));
    }

    [Test]
    public void HighestScoringArtifactsTest()
    {
        UnitDefinition leaderDefinition = CreateLeaderDefinition(LeaderAbility.WeaponArmorUse);
        ItemDefinition strongWeapon = CreateItem("weapon", ItemCategory.Weapon, 300, modifier: null);
        ItemDefinition mediumArmor = CreateItem("armor-medium", ItemCategory.Armor, 200, modifier: null);
        ItemDefinition weakArmor = CreateItem("armor-weak", ItemCategory.Armor, 100, modifier: null);
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .AddItem(strongWeapon)
            .AddItem(mediumArmor)
            .AddItem(weakArmor)
            .Build();

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 100, 0, 1, [], null, false);
        WorldItem strongWeaponItem = new(new WorldItemId("StrongWeapon"), strongWeapon.Id, Charges: null);
        WorldItem mediumArmorItem = new(new WorldItemId("MediumArmor"), mediumArmor.Id, Charges: null);
        WorldItem weakArmorItem = new(new WorldItemId("WeakArmor"), weakArmor.Id, Charges: null);

        UnitGrid<WorldUnit> stack = new UnitGrid<WorldUnit>.Builder()
            .Place(leader, GridPosition.FrontBottom, false)
            .Build();

        StackAutoFitCalculator.Result result = CheckCalculate(
            stack,
            UnitGrid<WorldUnit>.Empty,
            new WorldInventory([strongWeaponItem, mediumArmorItem, weakArmorItem]),
            WorldInventory.Empty,
            LeaderEquipment.Empty,
            GridPosition.Count,
            definitions);

        Assert.That(result, Is.EqualTo(new StackAutoFitCalculator.Result(
            StackUnits: new UnitGrid<WorldUnit>.Builder()
                .Place(leader, GridPosition.FrontCenter, false)
                .Build(),
            GarrisonUnits: UnitGrid<WorldUnit>.Empty,
            StackInventory: new WorldInventory([weakArmorItem]),
            GarrisonInventory: WorldInventory.Empty,
            Equipment: LeaderEquipment.Empty
                .With(LeaderEquipmentSlot.Artifact1, strongWeaponItem)
                .With(LeaderEquipmentSlot.Artifact2, mediumArmorItem)
        )));
    }

    [Test]
    public void RefitsStrongerBaggedItemTest()
    {
        UnitModifierDefinitionId strongModifier = new("strong-mod");
        UnitModifierDefinitionId mediumModifier = new("medium-mod");
        UnitModifierDefinitionId weakModifier = new("weak-mod");
        UnitDefinition leaderDefinition = CreateLeaderDefinition(LeaderAbility.WeaponArmorUse);
        ItemDefinition strongWeapon = CreateItem("weapon-strong", ItemCategory.Weapon, 300, modifier: strongModifier);
        ItemDefinition mediumWeapon = CreateItem("weapon-medium", ItemCategory.Weapon, 200, modifier: mediumModifier);
        ItemDefinition weakWeapon = CreateItem("weapon-weak", ItemCategory.Weapon, 100, modifier: weakModifier);
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .AddUnitModifier(TestHelpers.SimpleModifier(strongModifier.Value))
            .AddUnitModifier(TestHelpers.SimpleModifier(mediumModifier.Value))
            .AddUnitModifier(TestHelpers.SimpleModifier(weakModifier.Value))
            .AddItem(strongWeapon)
            .AddItem(mediumWeapon)
            .AddItem(weakWeapon)
            .Build();

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 100, 0, 1, [UnitModifier.FromEquipment(weakModifier)], null, false);
        WorldItem strongWeaponItem = new(new WorldItemId("StrongWeapon"), strongWeapon.Id, Charges: null);
        WorldItem mediumWeaponItem = new(new WorldItemId("MediumWeapon"), mediumWeapon.Id, Charges: null);
        WorldItem weakWeaponItem = new(new WorldItemId("WeakWeapon"), weakWeapon.Id, Charges: null);

        UnitGrid<WorldUnit> stack = new UnitGrid<WorldUnit>.Builder()
            .Place(leader, GridPosition.FrontBottom, false)
            .Build();

        StackAutoFitCalculator.Result result = CheckCalculate(
            stack,
            UnitGrid<WorldUnit>.Empty,
            new WorldInventory([strongWeaponItem, mediumWeaponItem]),
            WorldInventory.Empty,
            LeaderEquipment.Empty.With(LeaderEquipmentSlot.Artifact1, weakWeaponItem),
            GridPosition.Count,
            definitions);

        Assert.That(result, Is.EqualTo(new StackAutoFitCalculator.Result(
            StackUnits: new UnitGrid<WorldUnit>.Builder()
                .Place(leader with { Modifiers = [UnitModifier.FromEquipment(strongModifier), UnitModifier.FromEquipment(mediumModifier)] }, GridPosition.FrontCenter, false)
                .Build(),
            GarrisonUnits: UnitGrid<WorldUnit>.Empty,
            StackInventory: new WorldInventory([weakWeaponItem]),
            GarrisonInventory: WorldInventory.Empty,
            Equipment: LeaderEquipment.Empty
                .With(LeaderEquipmentSlot.Artifact1, strongWeaponItem)
                .With(LeaderEquipmentSlot.Artifact2, mediumWeaponItem)
        )));
    }

    [Test]
    public void SkipsUnusableItemsTest()
    {
        UnitDefinition leaderDefinition = CreateLeaderDefinition(LeaderAbility.None);
        ItemDefinition weapon = CreateItem("weapon", ItemCategory.Weapon, 300, modifier: null);
        ItemDefinition potion = CreateItem("potion", ItemCategory.PotionHeal, 50, modifier: null);
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .AddItem(weapon)
            .AddItem(potion)
            .Build();

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 100, 0, 1, [], null, false);
        WorldItem weaponItem = new(new WorldItemId("Weapon"), weapon.Id, Charges: null);
        WorldItem potionItem = new(new WorldItemId("Potion"), potion.Id, Charges: null);

        UnitGrid<WorldUnit> stack = new UnitGrid<WorldUnit>.Builder()
            .Place(leader, GridPosition.FrontBottom, false)
            .Build();

        StackAutoFitCalculator.Result result = CheckCalculate(
            stack,
            UnitGrid<WorldUnit>.Empty,
            new WorldInventory([weaponItem, potionItem]),
            WorldInventory.Empty,
            LeaderEquipment.Empty,
            GridPosition.Count,
            definitions);

        Assert.That(result, Is.EqualTo(new StackAutoFitCalculator.Result(
            StackUnits: new UnitGrid<WorldUnit>.Builder()
                .Place(leader, GridPosition.FrontCenter, false)
                .Build(),
            GarrisonUnits: UnitGrid<WorldUnit>.Empty,
            StackInventory: new WorldInventory([weaponItem]),
            GarrisonInventory: WorldInventory.Empty,
            Equipment: LeaderEquipment.Empty.With(LeaderEquipmentSlot.Battle1, potionItem)
        )));
    }

    [Test]
    public void GarrisonInventoryItemTest()
    {
        UnitDefinition leaderDefinition = CreateLeaderDefinition(LeaderAbility.WeaponArmorUse);
        ItemDefinition garrisonWeapon = CreateItem("garrison-weapon", ItemCategory.Weapon, 300, modifier: null);
        ItemDefinition garrisonJewel = CreateItem("garrison-jewel", ItemCategory.Jewel, 100, modifier: null);
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .AddItem(garrisonWeapon)
            .AddItem(garrisonJewel)
            .Build();

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 100, 0, 1, [], null, false);
        WorldItem garrisonWeaponItem = new(new WorldItemId("GarrisonWeapon"), garrisonWeapon.Id, Charges: null);
        WorldItem garrisonJewelItem = new(new WorldItemId("GarrisonJewel"), garrisonJewel.Id, Charges: null);

        UnitGrid<WorldUnit> stack = new UnitGrid<WorldUnit>.Builder()
            .Place(leader, GridPosition.FrontBottom, false)
            .Build();

        StackAutoFitCalculator.Result result = CheckCalculate(
            stack,
            UnitGrid<WorldUnit>.Empty,
            WorldInventory.Empty,
            new WorldInventory([garrisonWeaponItem, garrisonJewelItem]),
            LeaderEquipment.Empty,
            GridPosition.Count,
            definitions);

        Assert.That(result, Is.EqualTo(new StackAutoFitCalculator.Result(
            StackUnits: new UnitGrid<WorldUnit>.Builder()
                .Place(leader, GridPosition.FrontCenter, false)
                .Build(),
            GarrisonUnits: UnitGrid<WorldUnit>.Empty,
            StackInventory: new WorldInventory([garrisonJewelItem]),
            GarrisonInventory: WorldInventory.Empty,
            Equipment: LeaderEquipment.Empty.With(LeaderEquipmentSlot.Artifact1, garrisonWeaponItem)
        )));
    }

    [Test]
    public void UnequippedGarrisonItemTest()
    {
        UnitDefinition leaderDefinition = CreateLeaderDefinition(LeaderAbility.WeaponArmorUse);
        ItemDefinition strongStackWeapon = CreateItem("stack-weapon-strong", ItemCategory.Weapon, 300, modifier: null);
        ItemDefinition mediumStackWeapon = CreateItem("stack-weapon-medium", ItemCategory.Weapon, 200, modifier: null);
        ItemDefinition weakGarrisonWeapon = CreateItem("garrison-weapon", ItemCategory.Weapon, 100, modifier: null);
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .AddItem(strongStackWeapon)
            .AddItem(mediumStackWeapon)
            .AddItem(weakGarrisonWeapon)
            .Build();

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 100, 0, 1, [], null, false);
        WorldItem strongStackWeaponItem = new(new WorldItemId("StrongStackWeapon"), strongStackWeapon.Id, Charges: null);
        WorldItem mediumStackWeaponItem = new(new WorldItemId("MediumStackWeapon"), mediumStackWeapon.Id, Charges: null);
        WorldItem weakGarrisonWeaponItem = new(new WorldItemId("WeakGarrisonWeapon"), weakGarrisonWeapon.Id, Charges: null);

        UnitGrid<WorldUnit> stack = new UnitGrid<WorldUnit>.Builder()
            .Place(leader, GridPosition.FrontBottom, false)
            .Build();

        StackAutoFitCalculator.Result result = CheckCalculate(
            stack,
            UnitGrid<WorldUnit>.Empty,
            new WorldInventory([strongStackWeaponItem, mediumStackWeaponItem]),
            new WorldInventory([weakGarrisonWeaponItem]),
            LeaderEquipment.Empty,
            GridPosition.Count,
            definitions);

        Assert.That(result, Is.EqualTo(new StackAutoFitCalculator.Result(
            StackUnits: new UnitGrid<WorldUnit>.Builder()
                .Place(leader, GridPosition.FrontCenter, false)
                .Build(),
            GarrisonUnits: UnitGrid<WorldUnit>.Empty,
            StackInventory: new WorldInventory([weakGarrisonWeaponItem]),
            GarrisonInventory: WorldInventory.Empty,
            Equipment: LeaderEquipment.Empty
                .With(LeaderEquipmentSlot.Artifact1, strongStackWeaponItem)
                .With(LeaderEquipmentSlot.Artifact2, mediumStackWeaponItem)
        )));
    }

    [Test]
    public void NoValidPartitionTest()
    {
        UnitDefinition leaderDefinition = CreateUnitDefinition("leader", 100, false, UnitAttackReach.Adjacent) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Leadership = 2
        };
        UnitDefinition soldierDefinition = CreateUnitDefinition("soldier", 60, false, UnitAttackReach.Adjacent);
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .AddUnit(soldierDefinition)
            .Build();

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 100, 0, 1, [], null, false);
        WorldUnit soldier1 = new(new WorldUnitId("soldier1"), soldierDefinition.Id, 100, 0, 1, [], null, false);
        WorldUnit soldier2 = new(new WorldUnitId("soldier2"), soldierDefinition.Id, 100, 0, 1, [], null, false);
        WorldUnit soldier3 = new(new WorldUnitId("soldier3"), soldierDefinition.Id, 100, 0, 1, [], null, false);
        WorldUnit soldier4 = new(new WorldUnitId("soldier4"), soldierDefinition.Id, 100, 0, 1, [], null, false);
        WorldUnit soldier5 = new(new WorldUnitId("soldier5"), soldierDefinition.Id, 100, 0, 1, [], null, false);
        WorldUnit soldier6 = new(new WorldUnitId("soldier6"), soldierDefinition.Id, 100, 0, 1, [], null, false);
        WorldUnit soldier7 = new(new WorldUnitId("soldier7"), soldierDefinition.Id, 100, 0, 1, [], null, false);
        WorldUnit soldier8 = new(new WorldUnitId("soldier8"), soldierDefinition.Id, 100, 0, 1, [], null, false);
        WorldUnit soldier9 = new(new WorldUnitId("soldier9"), soldierDefinition.Id, 100, 0, 1, [], null, false);

        UnitGrid<WorldUnit> stack = new UnitGrid<WorldUnit>.Builder()
            .Place(leader, GridPosition.FrontBottom, false)
            .Place(soldier7, GridPosition.FrontCenter, false)
            .Place(soldier8, GridPosition.FrontTop, false)
            .Place(soldier9, GridPosition.BackBottom, false)
            .Build();

        UnitGrid<WorldUnit> garrison = new UnitGrid<WorldUnit>.Builder()
            .Place(soldier1, GridPosition.FrontBottom, false)
            .Place(soldier2, GridPosition.FrontCenter, false)
            .Place(soldier3, GridPosition.FrontTop, false)
            .Place(soldier4, GridPosition.BackBottom, false)
            .Place(soldier5, GridPosition.BackCenter, false)
            .Place(soldier6, GridPosition.BackTop, false)
            .Build();

        StackAutoFitCalculator.Result result = CheckCalculate(
            stack,
            garrison,
            WorldInventory.Empty,
            WorldInventory.Empty,
            LeaderEquipment.Empty,
            GridPosition.Count,
            definitions);

        Assert.That(result, Is.EqualTo(new StackAutoFitCalculator.Result(stack, garrison, WorldInventory.Empty, WorldInventory.Empty, LeaderEquipment.Empty)));
    }

    [Test]
    public void BackLineOverflowTest()
    {
        UnitDefinition leaderDefinition = CreateUnitDefinition("leader", 100, false, UnitAttackReach.Any) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Leadership = 6
        };
        UnitDefinition soldierDefinition = CreateUnitDefinition("soldier", 60, false, UnitAttackReach.Any);
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .AddUnit(soldierDefinition)
            .Build();

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 100, 0, 1, [], null, false);
        WorldUnit soldier1 = new(new WorldUnitId("soldier1"), soldierDefinition.Id, 10, 0, 1, [], null, false);
        WorldUnit soldier2 = new(new WorldUnitId("soldier2"), soldierDefinition.Id, 20, 0, 1, [], null, false);
        WorldUnit soldier3 = new(new WorldUnitId("soldier3"), soldierDefinition.Id, 30, 0, 1, [], null, false);
        WorldUnit soldier4 = new(new WorldUnitId("soldier4"), soldierDefinition.Id, 40, 0, 1, [], null, false);

        UnitGrid<WorldUnit> stack = new UnitGrid<WorldUnit>.Builder()
            .Place(leader, GridPosition.FrontBottom, false)
            .Place(soldier1, GridPosition.FrontCenter, false)
            .Place(soldier2, GridPosition.FrontTop, false)
            .Place(soldier3, GridPosition.BackBottom, false)
            .Place(soldier4, GridPosition.BackCenter, false)
            .Build();

        StackAutoFitCalculator.Result result = CheckCalculate(
            stack,
            UnitGrid<WorldUnit>.Empty,
            WorldInventory.Empty,
            WorldInventory.Empty,
            LeaderEquipment.Empty,
            GridPosition.Count,
            definitions);

        Assert.That(result, Is.EqualTo(new StackAutoFitCalculator.Result(
            StackUnits: new UnitGrid<WorldUnit>.Builder()
                .Place(soldier2, GridPosition.FrontCenter, false)
                .Place(soldier1, GridPosition.FrontBottom, false)
                .Place(leader, GridPosition.BackCenter, false)
                .Place(soldier4, GridPosition.BackBottom, false)
                .Place(soldier3, GridPosition.BackTop, false)
                .Build(),
            GarrisonUnits: UnitGrid<WorldUnit>.Empty,
            StackInventory: WorldInventory.Empty,
            GarrisonInventory: WorldInventory.Empty,
            Equipment: LeaderEquipment.Empty
        )));
    }

    [Test]
    public void EquippedDuplicateItemsTest()
    {
        UnitDefinition leaderDefinition = CreateLeaderDefinition(LeaderAbility.WeaponArmorUse);
        ItemDefinition weapon = CreateItem("weapon", ItemCategory.Weapon, 300, modifier: null);
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .AddItem(weapon)
            .Build();

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 100, 0, 1, [], null, false);
        WorldItem stackWeaponItem = new(new WorldItemId("StackWeapon"), weapon.Id, Charges: null);
        WorldItem garrisonWeaponItem = new(new WorldItemId("GarrisonWeapon"), weapon.Id, Charges: null);
        WorldItem equippedWeaponItem = new(new WorldItemId("EquippedWeapon"), weapon.Id, Charges: null);

        UnitGrid<WorldUnit> stack = new UnitGrid<WorldUnit>.Builder()
            .Place(leader, GridPosition.FrontBottom, false)
            .Build();

        StackAutoFitCalculator.Result result = CheckCalculate(
            stack,
            UnitGrid<WorldUnit>.Empty,
            new WorldInventory([stackWeaponItem]),
            new WorldInventory([garrisonWeaponItem]),
            LeaderEquipment.Empty.With(LeaderEquipmentSlot.Artifact1, equippedWeaponItem),
            GridPosition.Count,
            definitions);

        Assert.That(result, Is.EqualTo(new StackAutoFitCalculator.Result(
            StackUnits: new UnitGrid<WorldUnit>.Builder()
                .Place(leader, GridPosition.FrontCenter, false)
                .Build(),
            GarrisonUnits: UnitGrid<WorldUnit>.Empty,
            StackInventory: new WorldInventory([garrisonWeaponItem]),
            GarrisonInventory: WorldInventory.Empty,
            Equipment: LeaderEquipment.Empty
                .With(LeaderEquipmentSlot.Artifact1, equippedWeaponItem)
                .With(LeaderEquipmentSlot.Artifact2, stackWeaponItem)
        )));
    }

    [Test]
    public void FillsEveryCompatibleSlotTest()
    {
        UnitDefinition leaderDefinition = CreateLeaderDefinition(
            LeaderAbility.WeaponArmorUse |
            LeaderAbility.JewelryUse |
            LeaderAbility.BannerUse |
            LeaderAbility.TravelItemUse);
        ItemDefinition weapon = CreateItem("weapon", ItemCategory.Weapon, 300, modifier: null);
        ItemDefinition armor = CreateItem("armor", ItemCategory.Armor, 250, modifier: null);
        ItemDefinition banner = CreateItem("banner", ItemCategory.Banner, 200, modifier: null);
        ItemDefinition healPotion = CreateItem("potion-heal", ItemCategory.PotionHeal, 150, modifier: null);
        ItemDefinition boostPotion = CreateItem("potion-boost", ItemCategory.PotionTemporaryBoost, 120, modifier: null);
        ItemDefinition boots = CreateItem("boots", ItemCategory.TravelItem, 100, modifier: null);
        ItemDefinition jewel = CreateItem("jewel", ItemCategory.Jewel, 50, modifier: null);
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .AddItem(weapon)
            .AddItem(armor)
            .AddItem(banner)
            .AddItem(healPotion)
            .AddItem(boostPotion)
            .AddItem(boots)
            .AddItem(jewel)
            .Build();

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 100, 0, 1, [], null, false);
        WorldItem weaponItem = new(new WorldItemId("Weapon"), weapon.Id, Charges: null);
        WorldItem armorItem = new(new WorldItemId("Armor"), armor.Id, Charges: null);
        WorldItem bannerItem = new(new WorldItemId("Banner"), banner.Id, Charges: null);
        WorldItem healPotionItem = new(new WorldItemId("HealPotion"), healPotion.Id, Charges: null);
        WorldItem boostPotionItem = new(new WorldItemId("BoostPotion"), boostPotion.Id, Charges: null);
        WorldItem bootsItem = new(new WorldItemId("Boots"), boots.Id, Charges: null);
        WorldItem jewelItem = new(new WorldItemId("Jewel"), jewel.Id, Charges: null);

        UnitGrid<WorldUnit> stack = new UnitGrid<WorldUnit>.Builder()
            .Place(leader, GridPosition.FrontBottom, false)
            .Build();

        StackAutoFitCalculator.Result result = CheckCalculate(
            stack,
            UnitGrid<WorldUnit>.Empty,
            new WorldInventory([
                weaponItem,
                armorItem,
                bannerItem,
                healPotionItem,
                boostPotionItem,
                bootsItem,
                jewelItem
            ]),
            WorldInventory.Empty,
            LeaderEquipment.Empty,
            GridPosition.Count,
            definitions);

        Assert.That(result, Is.EqualTo(new StackAutoFitCalculator.Result(
            StackUnits: new UnitGrid<WorldUnit>.Builder()
                .Place(leader, GridPosition.FrontCenter, false)
                .Build(),
            GarrisonUnits: UnitGrid<WorldUnit>.Empty,
            StackInventory: WorldInventory.Empty,
            GarrisonInventory: WorldInventory.Empty,
            Equipment: LeaderEquipment.Empty
                .With(LeaderEquipmentSlot.Banner, bannerItem)
                .With(LeaderEquipmentSlot.Tome, jewelItem)
                .With(LeaderEquipmentSlot.Battle1, healPotionItem)
                .With(LeaderEquipmentSlot.Battle2, boostPotionItem)
                .With(LeaderEquipmentSlot.Artifact1, weaponItem)
                .With(LeaderEquipmentSlot.Artifact2, armorItem)
                .With(LeaderEquipmentSlot.Boots, bootsItem)
        )));
    }

    [Test]
    public void LowerScoringItemOwnSlotTest()
    {
        UnitDefinition leaderDefinition = CreateLeaderDefinition(LeaderAbility.WeaponArmorUse | LeaderAbility.BannerUse);
        ItemDefinition strongWeapon = CreateItem("weapon-strong", ItemCategory.Weapon, 300, modifier: null);
        ItemDefinition mediumArmor = CreateItem("armor-medium", ItemCategory.Armor, 200, modifier: null);
        ItemDefinition weakBanner = CreateItem("banner-weak", ItemCategory.Banner, 10, modifier: null);
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .AddItem(strongWeapon)
            .AddItem(mediumArmor)
            .AddItem(weakBanner)
            .Build();

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 100, 0, 1, [], null, false);
        WorldItem strongWeaponItem = new(new WorldItemId("StrongWeapon"), strongWeapon.Id, Charges: null);
        WorldItem mediumArmorItem = new(new WorldItemId("MediumArmor"), mediumArmor.Id, Charges: null);
        WorldItem weakBannerItem = new(new WorldItemId("WeakBanner"), weakBanner.Id, Charges: null);

        UnitGrid<WorldUnit> stack = new UnitGrid<WorldUnit>.Builder()
            .Place(leader, GridPosition.FrontBottom, false)
            .Build();

        StackAutoFitCalculator.Result result = CheckCalculate(
            stack,
            UnitGrid<WorldUnit>.Empty,
            new WorldInventory([strongWeaponItem, mediumArmorItem, weakBannerItem]),
            WorldInventory.Empty,
            LeaderEquipment.Empty,
            GridPosition.Count,
            definitions);

        Assert.That(result, Is.EqualTo(new StackAutoFitCalculator.Result(
            StackUnits: new UnitGrid<WorldUnit>.Builder()
                .Place(leader, GridPosition.FrontCenter, false)
                .Build(),
            GarrisonUnits: UnitGrid<WorldUnit>.Empty,
            StackInventory: WorldInventory.Empty,
            GarrisonInventory: WorldInventory.Empty,
            Equipment: LeaderEquipment.Empty
                .With(LeaderEquipmentSlot.Banner, weakBannerItem)
                .With(LeaderEquipmentSlot.Artifact1, strongWeaponItem)
                .With(LeaderEquipmentSlot.Artifact2, mediumArmorItem)
        )));
    }

    [Test]
    public void NewItemModifiersTest()
    {
        UnitModifierDefinitionId weaponModifier = new("weapon-mod");
        UnitModifierDefinitionId bannerModifier = new("banner-mod");
        UnitModifierDefinitionId bootsModifier = new("boots-mod");
        UnitDefinition leaderDefinition = CreateLeaderDefinition(
            LeaderAbility.WeaponArmorUse | LeaderAbility.BannerUse | LeaderAbility.TravelItemUse);
        ItemDefinition weapon = CreateItem("weapon", ItemCategory.Weapon, 300, modifier: weaponModifier);
        ItemDefinition banner = CreateItem("banner", ItemCategory.Banner, 200, modifier: bannerModifier);
        ItemDefinition boots = CreateItem("boots", ItemCategory.TravelItem, 100, modifier: bootsModifier);
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .AddUnitModifier(TestHelpers.SimpleModifier(weaponModifier.Value))
            .AddUnitModifier(TestHelpers.SimpleModifier(bannerModifier.Value))
            .AddUnitModifier(TestHelpers.SimpleModifier(bootsModifier.Value))
            .AddItem(weapon)
            .AddItem(banner)
            .AddItem(boots)
            .Build();

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 100, 0, 1, [], null, false);
        WorldItem weaponItem = new(new WorldItemId("Weapon"), weapon.Id, Charges: null);
        WorldItem bannerItem = new(new WorldItemId("Banner"), banner.Id, Charges: null);
        WorldItem bootsItem = new(new WorldItemId("Boots"), boots.Id, Charges: null);

        UnitGrid<WorldUnit> stack = new UnitGrid<WorldUnit>.Builder()
            .Place(leader, GridPosition.FrontBottom, false)
            .Build();

        StackAutoFitCalculator.Result result = CheckCalculate(
            stack,
            UnitGrid<WorldUnit>.Empty,
            new WorldInventory([weaponItem, bannerItem, bootsItem]),
            WorldInventory.Empty,
            LeaderEquipment.Empty,
            GridPosition.Count,
            definitions);

        Assert.That(result, Is.EqualTo(new StackAutoFitCalculator.Result(
            StackUnits: new UnitGrid<WorldUnit>.Builder()
                .Place(leader with { Modifiers = [UnitModifier.FromEquipment(bannerModifier), UnitModifier.FromEquipment(weaponModifier), UnitModifier.FromEquipment(bootsModifier)] }, GridPosition.FrontCenter, false)
                .Build(),
            GarrisonUnits: UnitGrid<WorldUnit>.Empty,
            StackInventory: WorldInventory.Empty,
            GarrisonInventory: WorldInventory.Empty,
            Equipment: LeaderEquipment.Empty
                .With(LeaderEquipmentSlot.Banner, bannerItem)
                .With(LeaderEquipmentSlot.Artifact1, weaponItem)
                .With(LeaderEquipmentSlot.Boots, bootsItem)
        )));
    }

    [Test]
    public void ActiveItemTest()
    {
        UnitDefinition leaderDefinition = CreateLeaderDefinition(LeaderAbility.OrbUse | LeaderAbility.TalismanUse);
        ItemDefinition orb = CreateItem("orb", ItemCategory.Orb, 40, modifier: null);
        ItemDefinition talisman = CreateItem("talisman", ItemCategory.Talisman, 50, modifier: null);
        ItemDefinition healPotion = CreateItem("potion-heal", ItemCategory.PotionHeal, 300, modifier: null);
        ItemDefinition boostPotion = CreateItem("potion-boost", ItemCategory.PotionTemporaryBoost, 400, modifier: null);
        ItemDefinition revivePotion = CreateItem("potion-revive", ItemCategory.PotionRevive, 500, modifier: null);
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .AddItem(orb)
            .AddItem(talisman)
            .AddItem(healPotion)
            .AddItem(boostPotion)
            .AddItem(revivePotion)
            .Build();

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 100, 0, 1, [], null, false);
        WorldItem orbItem = new(new WorldItemId("Orb"), orb.Id, Charges: null);
        WorldItem talismanItem = new(new WorldItemId("Talisman"), talisman.Id, Charges: 3);
        WorldItem healPotionItem = new(new WorldItemId("HealPotion"), healPotion.Id, Charges: null);
        WorldItem boostPotionItem = new(new WorldItemId("BoostPotion"), boostPotion.Id, Charges: null);
        WorldItem revivePotionItem = new(new WorldItemId("RevivePotion"), revivePotion.Id, Charges: null);

        UnitGrid<WorldUnit> stack = new UnitGrid<WorldUnit>.Builder()
            .Place(leader, GridPosition.FrontBottom, false)
            .Build();

        StackAutoFitCalculator.Result result = CheckCalculate(
            stack,
            UnitGrid<WorldUnit>.Empty,
            new WorldInventory([revivePotionItem, boostPotionItem, healPotionItem, orbItem]),
            WorldInventory.Empty,
            LeaderEquipment.Empty.With(LeaderEquipmentSlot.Battle1, talismanItem),
            GridPosition.Count,
            definitions);

        Assert.That(result, Is.EqualTo(new StackAutoFitCalculator.Result(
            StackUnits: new UnitGrid<WorldUnit>.Builder()
                .Place(leader, GridPosition.FrontCenter, false)
                .Build(),
            GarrisonUnits: UnitGrid<WorldUnit>.Empty,
            StackInventory: new WorldInventory([revivePotionItem, boostPotionItem, healPotionItem]),
            GarrisonInventory: WorldInventory.Empty,
            Equipment: LeaderEquipment.Empty
                .With(LeaderEquipmentSlot.Battle1, talismanItem)
                .With(LeaderEquipmentSlot.Battle2, orbItem)
        )));
    }

    [Test]
    public void HealingItemTest()
    {
        UnitDefinition leaderDefinition = CreateLeaderDefinition(LeaderAbility.None);
        ItemDefinition healPotion = CreateItem("potion-heal", ItemCategory.PotionHeal, 50, modifier: null);
        ItemDefinition boostPotion = CreateItem("potion-boost", ItemCategory.PotionTemporaryBoost, 400, modifier: null);
        ItemDefinition permanentPotion = CreateItem("potion-permanent", ItemCategory.PotionPermanentBoost, 300, modifier: null);
        ItemDefinition revivePotion = CreateItem("potion-revive", ItemCategory.PotionRevive, 500, modifier: null);
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .AddItem(healPotion)
            .AddItem(boostPotion)
            .AddItem(permanentPotion)
            .AddItem(revivePotion)
            .Build();

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 100, 0, 1, [], null, false);
        WorldItem healPotionItem = new(new WorldItemId("HealPotion"), healPotion.Id, Charges: null);
        WorldItem boostPotionItem = new(new WorldItemId("BoostPotion"), boostPotion.Id, Charges: null);
        WorldItem permanentPotionItem = new(new WorldItemId("PermanentPotion"), permanentPotion.Id, Charges: null);
        WorldItem revivePotionItem = new(new WorldItemId("RevivePotion"), revivePotion.Id, Charges: null);

        UnitGrid<WorldUnit> stack = new UnitGrid<WorldUnit>.Builder()
            .Place(leader, GridPosition.FrontBottom, false)
            .Build();

        StackAutoFitCalculator.Result result = CheckCalculate(
            stack,
            UnitGrid<WorldUnit>.Empty,
            new WorldInventory([permanentPotionItem, boostPotionItem, healPotionItem]),
            WorldInventory.Empty,
            LeaderEquipment.Empty.With(LeaderEquipmentSlot.Battle1, revivePotionItem),
            GridPosition.Count,
            definitions);

        Assert.That(result, Is.EqualTo(new StackAutoFitCalculator.Result(
            StackUnits: new UnitGrid<WorldUnit>.Builder()
                .Place(leader, GridPosition.FrontCenter, false)
                .Build(),
            GarrisonUnits: UnitGrid<WorldUnit>.Empty,
            StackInventory: new WorldInventory([revivePotionItem, permanentPotionItem]),
            GarrisonInventory: WorldInventory.Empty,
            Equipment: LeaderEquipment.Empty
                .With(LeaderEquipmentSlot.Battle1, healPotionItem)
                .With(LeaderEquipmentSlot.Battle2, boostPotionItem)
        )));
    }

    [Test]
    public void LeaderAbilityTest()
    {
        UnitDefinition leaderDefinition = CreateLeaderDefinition(LeaderAbility.None);
        UnitModifierDefinition bannerLore = TestHelpers.SimpleModifier("banner-lore") with { LeaderAbilities = LeaderAbility.BannerUse };
        ItemDefinition banner = CreateItem("banner", ItemCategory.Banner, 200, modifier: null);
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .AddUnitModifier(bannerLore)
            .AddItem(banner)
            .Build();

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 100, 0, 1, [UnitModifier.Permanent(bannerLore.Id)], null, false);
        WorldItem bannerItem = new(new WorldItemId("Banner"), banner.Id, Charges: null);

        UnitGrid<WorldUnit> stack = new UnitGrid<WorldUnit>.Builder()
            .Place(leader, GridPosition.FrontBottom, false)
            .Build();

        StackAutoFitCalculator.Result result = CheckCalculate(
            stack,
            UnitGrid<WorldUnit>.Empty,
            new WorldInventory([bannerItem]),
            WorldInventory.Empty,
            LeaderEquipment.Empty,
            GridPosition.Count,
            definitions);

        Assert.That(result, Is.EqualTo(new StackAutoFitCalculator.Result(
            StackUnits: new UnitGrid<WorldUnit>.Builder()
                .Place(leader, GridPosition.FrontCenter, false)
                .Build(),
            GarrisonUnits: UnitGrid<WorldUnit>.Empty,
            StackInventory: WorldInventory.Empty,
            GarrisonInventory: WorldInventory.Empty,
            Equipment: LeaderEquipment.Empty.With(LeaderEquipmentSlot.Banner, bannerItem)
        )));
    }

    [Test]
    public void EquipmentAbilityTest()
    {
        UnitDefinition leaderDefinition = CreateLeaderDefinition(LeaderAbility.None);
        UnitModifierDefinition bannerLore = TestHelpers.SimpleModifier("banner-lore") with { LeaderAbilities = LeaderAbility.BannerUse };
        ItemDefinition tome = CreateItem("tome", ItemCategory.Jewel, 300, bannerLore.Id);
        ItemDefinition banner = CreateItem("banner", ItemCategory.Banner, 200, modifier: null);
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .AddUnitModifier(bannerLore)
            .AddItem(tome)
            .AddItem(banner)
            .Build();

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 100, 0, 1, [UnitModifier.FromEquipment(bannerLore.Id)], null, false);
        WorldItem tomeItem = new(new WorldItemId("Tome"), tome.Id, Charges: null);
        WorldItem bannerItem = new(new WorldItemId("Banner"), banner.Id, Charges: null);

        UnitGrid<WorldUnit> stack = new UnitGrid<WorldUnit>.Builder()
            .Place(leader, GridPosition.FrontBottom, false)
            .Build();

        StackAutoFitCalculator.Result result = CheckCalculate(
            stack,
            UnitGrid<WorldUnit>.Empty,
            new WorldInventory([bannerItem]),
            WorldInventory.Empty,
            LeaderEquipment.Empty.With(LeaderEquipmentSlot.Tome, tomeItem),
            GridPosition.Count,
            definitions);

        Assert.That(result, Is.EqualTo(new StackAutoFitCalculator.Result(
            StackUnits: new UnitGrid<WorldUnit>.Builder()
                .Place(leader with { Modifiers = [] }, GridPosition.FrontCenter, false)
                .Build(),
            GarrisonUnits: UnitGrid<WorldUnit>.Empty,
            StackInventory: new WorldInventory([tomeItem, bannerItem]),
            GarrisonInventory: WorldInventory.Empty,
            Equipment: LeaderEquipment.Empty
        )));
    }
}
