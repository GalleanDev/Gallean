namespace Gallean.Engine.Tests;

public sealed class WorldEngineEquipmentTests
{
    [Test]
    public void EquipItemTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 10, leadership: 6) with
            {
                LeaderAbilities = LeaderAbility.WeaponArmorUse
            };

        UnitModifierDefinition weaponModifier = TestHelpers.SimpleModifier("weapon-mod") with
        {
            Damage = new UnitStatModifier(20, 0)
        };

        ItemDefinition weapon = new(
            new ItemDefinitionId("weapon1"),
            new GlobalTextId("name"),
            new GlobalTextId("desc"),
            ItemCategory.Weapon,
            WorldResourceSet.Empty,
            EquipmentModifierId: weaponModifier.Id,
            EquipmentAttackId: null,
            Effect: null,
            AnimationUnitId: null);

        WorldItem weaponItem = new(new WorldItemId("Weapon"), weapon.Id, Charges: null);

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Stack1"),
                player.Id,
                new WorldPosition(0, 0),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom, false)
                    .Build(),
                movementPoints: 10) with
            {
                Inventory = new WorldInventory([weaponItem])
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(weaponModifier)
            .WithDefinition(weapon)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldEquipItemCommand(stack.Id, weaponItem.Id, LeaderEquipmentSlot.Artifact1),
            [new WorldItemEquippedEvent(stack.Id, weaponItem, LeaderEquipmentSlot.Artifact1)]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Inventory = WorldInventory.Empty,
                    Equipment = LeaderEquipment.Empty.With(LeaderEquipmentSlot.Artifact1, weaponItem),
                    Units = new UnitGrid<WorldUnit>.Builder()
                        .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition) with { Modifiers = [UnitModifier.FromEquipment(weaponModifier.Id)] }, GridPosition.FrontBottom, false)
                        .Build()
                })
            }));
    }

    [Test]
    public void UnequipItemTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 10, leadership: 6);

        UnitModifierDefinition weaponModifier = TestHelpers.SimpleModifier("weapon-mod") with
        {
            Damage = new UnitStatModifier(20, 0)
        };

        ItemDefinition weapon = new(
            new ItemDefinitionId("weapon1"),
            new GlobalTextId("name"),
            new GlobalTextId("desc"),
            ItemCategory.Weapon,
            WorldResourceSet.Empty,
            EquipmentModifierId: weaponModifier.Id,
            EquipmentAttackId: null,
            Effect: null,
            AnimationUnitId: null);

        WorldItem weaponItem = new(new WorldItemId("Weapon"), weapon.Id, Charges: null);

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Stack1"),
                player.Id,
                new WorldPosition(0, 0),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition) with { Modifiers = [UnitModifier.FromEquipment(weaponModifier.Id)] }, GridPosition.FrontBottom, false)
                    .Build(),
                movementPoints: 10) with
            {
                Equipment = LeaderEquipment.Empty.With(LeaderEquipmentSlot.Artifact1, weaponItem)
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(weaponModifier)
            .WithDefinition(weapon)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldUnequipItemCommand(stack.Id, LeaderEquipmentSlot.Artifact1),
            [new WorldItemUnequippedEvent(stack.Id, LeaderEquipmentSlot.Artifact1)]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Inventory = new WorldInventory([weaponItem]),
                    Equipment = LeaderEquipment.Empty,
                    Units = new UnitGrid<WorldUnit>.Builder()
                        .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom, false)
                        .Build()
                })
            }));
    }

    [Test]
    public void EquipItemWithoutLeaderAbilityTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 10, leadership: 6);

        ItemDefinition weapon = new(
            new ItemDefinitionId("weapon1"),
            new GlobalTextId("name"),
            new GlobalTextId("desc"),
            ItemCategory.Weapon,
            WorldResourceSet.Empty,
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: null,
            AnimationUnitId: null);

        WorldItem weaponItem = new(new WorldItemId("Weapon"), weapon.Id, Charges: null);

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Stack1"),
                player.Id,
                new WorldPosition(0, 0),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom, false)
                    .Build(),
                movementPoints: 10) with
            {
                Inventory = new WorldInventory([weaponItem])
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(weapon)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(
            new WorldEquipItemCommand(stack.Id, weaponItem.Id, LeaderEquipmentSlot.Artifact1),
            WorldCommandError.MissingLeaderAbility);
    }

    [Test]
    public void EquipIntoWrongSlotTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 10, leadership: 6) with
            {
                LeaderAbilities = LeaderAbility.WeaponArmorUse
            };

        ItemDefinition weapon = new(
            new ItemDefinitionId("weapon1"),
            new GlobalTextId("name"),
            new GlobalTextId("desc"),
            ItemCategory.Weapon,
            WorldResourceSet.Empty,
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: null,
            AnimationUnitId: null);

        WorldItem weaponItem = new(new WorldItemId("Weapon"), weapon.Id, Charges: null);

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Stack1"),
                player.Id,
                new WorldPosition(0, 0),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom, false)
                    .Build(),
                movementPoints: 10) with
            {
                Inventory = new WorldInventory([weaponItem])
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(weapon)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(
            new WorldEquipItemCommand(stack.Id, weaponItem.Id, LeaderEquipmentSlot.Banner),
            WorldCommandError.ItemSlotMismatch);
    }

    [Test]
    public void EquipItemNotFoundTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 10, leadership: 6);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(0, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 10);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(
            new WorldEquipItemCommand(stack.Id, new WorldItemId("Weapon"), LeaderEquipmentSlot.Artifact1),
            WorldCommandError.ItemNotFound);
    }

    [Test]
    public void EquipItemToOccupiedSlotTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 10, leadership: 6) with
            {
                LeaderAbilities = LeaderAbility.WeaponArmorUse
            };

        ItemDefinition equippedWeapon = new(
            new ItemDefinitionId("weapon1"),
            new GlobalTextId("name"),
            new GlobalTextId("desc"),
            ItemCategory.Weapon,
            WorldResourceSet.Empty,
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: null,
            AnimationUnitId: null);

        WorldItem equippedWeaponItem = new(new WorldItemId("EquippedWeapon"), equippedWeapon.Id, Charges: null);

        ItemDefinition spareWeapon = new(
            new ItemDefinitionId("weapon2"),
            new GlobalTextId("name"),
            new GlobalTextId("desc"),
            ItemCategory.Weapon,
            WorldResourceSet.Empty,
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: null,
            AnimationUnitId: null);

        WorldItem spareWeaponItem = new(new WorldItemId("SpareWeapon"), spareWeapon.Id, Charges: null);

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Stack1"),
                player.Id,
                new WorldPosition(0, 0),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom, false)
                    .Build(),
                movementPoints: 10) with
            {
                Inventory = new WorldInventory([spareWeaponItem]),
                Equipment = LeaderEquipment.Empty.With(LeaderEquipmentSlot.Artifact1, equippedWeaponItem)
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(equippedWeapon)
            .WithDefinition(spareWeapon)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(
            new WorldEquipItemCommand(stack.Id, spareWeaponItem.Id, LeaderEquipmentSlot.Artifact1),
            WorldCommandError.EquipmentSlotOccupied);
    }

    [Test]
    public void EmptySlotTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 10, leadership: 6);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(0, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 10);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(
            new WorldUnequipItemCommand(stack.Id, LeaderEquipmentSlot.Artifact1),
            WorldCommandError.EquipmentSlotEmpty);
    }

    [Test]
    public void EquipTomeTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 10, leadership: 6) with
            {
                LeaderAbilities = LeaderAbility.JewelryUse
            };

        UnitModifierDefinition tomeModifier = TestHelpers.SimpleModifier("tome-mod") with
        {
            Damage = new UnitStatModifier(15, 0)
        };

        ItemDefinition tome = new(
            new ItemDefinitionId("tome1"),
            new GlobalTextId("name"),
            new GlobalTextId("desc"),
            ItemCategory.Jewel,
            WorldResourceSet.Empty,
            EquipmentModifierId: tomeModifier.Id,
            EquipmentAttackId: null,
            Effect: null,
            AnimationUnitId: null);

        WorldItem tomeItem = new(new WorldItemId("Tome"), tome.Id, Charges: null);

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Stack1"),
                player.Id,
                new WorldPosition(0, 0),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom, false)
                    .Build(),
                movementPoints: 10) with
            {
                Inventory = new WorldInventory([tomeItem])
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(tomeModifier)
            .WithDefinition(tome)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldEquipItemCommand(stack.Id, tomeItem.Id, LeaderEquipmentSlot.Tome),
            [new WorldItemEquippedEvent(stack.Id, tomeItem, LeaderEquipmentSlot.Tome)]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Inventory = WorldInventory.Empty,
                    Equipment = LeaderEquipment.Empty.With(LeaderEquipmentSlot.Tome, tomeItem),
                    Units = new UnitGrid<WorldUnit>.Builder()
                        .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition) with { Modifiers = [UnitModifier.FromEquipment(tomeModifier.Id)] }, GridPosition.FrontBottom, false)
                        .Build()
                })
            }));
    }

    [Test]
    public void EquipTomeIntoArtifactSlotTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 10, leadership: 6) with
            {
                LeaderAbilities = LeaderAbility.JewelryUse
            };

        ItemDefinition tome = new(
            new ItemDefinitionId("tome1"),
            new GlobalTextId("name"),
            new GlobalTextId("desc"),
            ItemCategory.Jewel,
            WorldResourceSet.Empty,
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: null,
            AnimationUnitId: null);

        WorldItem tomeItem = new(new WorldItemId("Tome"), tome.Id, Charges: null);

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Stack1"),
                player.Id,
                new WorldPosition(0, 0),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom, false)
                    .Build(),
                movementPoints: 10) with
            {
                Inventory = new WorldInventory([tomeItem])
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(tome)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(
            new WorldEquipItemCommand(stack.Id, tomeItem.Id, LeaderEquipmentSlot.Artifact1),
            WorldCommandError.ItemSlotMismatch);
    }

    [Test]
    public void TalismanChargesTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 10, leadership: 6) with
            {
                LeaderAbilities = LeaderAbility.TalismanUse
            };

        ItemDefinition talisman = new(
            new ItemDefinitionId("talisman"),
            new GlobalTextId("name"),
            new GlobalTextId("desc"),
            ItemCategory.Talisman,
            WorldResourceSet.Empty,
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: null,
            AnimationUnitId: null);

        WorldItem talismanItem = new(new WorldItemId("Talisman"), talisman.Id, Charges: 2);

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Stack1"),
                player.Id,
                new WorldPosition(0, 0),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom, false)
                    .Build(),
                movementPoints: 10) with
            {
                Inventory = new WorldInventory([talismanItem])
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(talisman)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldEquipItemCommand(stack.Id, talismanItem.Id, LeaderEquipmentSlot.Battle1),
            [new WorldItemEquippedEvent(stack.Id, talismanItem, LeaderEquipmentSlot.Battle1)]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Inventory = WorldInventory.Empty,
                    Equipment = LeaderEquipment.Empty.With(LeaderEquipmentSlot.Battle1, talismanItem)
                })
            }));

        engine.CheckCommand(
            new WorldUnequipItemCommand(stack.Id, LeaderEquipmentSlot.Battle1),
            [new WorldItemUnequippedEvent(stack.Id, LeaderEquipmentSlot.Battle1)]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Inventory = new WorldInventory([talismanItem]),
                    Equipment = LeaderEquipment.Empty
                })
            }));
    }

    [Test]
    public void MoveEquippedItemTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 10, leadership: 6) with
            {
                LeaderAbilities = LeaderAbility.WeaponArmorUse
            };

        UnitModifierDefinition weaponModifier = TestHelpers.SimpleModifier("weapon-mod") with
        {
            Damage = new UnitStatModifier(20, 0)
        };

        ItemDefinition weapon = new(
            new ItemDefinitionId("weapon1"),
            new GlobalTextId("name"),
            new GlobalTextId("desc"),
            ItemCategory.Weapon,
            WorldResourceSet.Empty,
            EquipmentModifierId: weaponModifier.Id,
            EquipmentAttackId: null,
            Effect: null,
            AnimationUnitId: null);

        WorldItem weaponItem = new(new WorldItemId("Weapon"), weapon.Id, Charges: null);

        ItemDefinition potion = new(
            new ItemDefinitionId("potion1"),
            new GlobalTextId("name"),
            new GlobalTextId("desc"),
            ItemCategory.PotionHeal,
            WorldResourceSet.Empty,
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: null,
            AnimationUnitId: null);

        WorldItem potionItem = new(new WorldItemId("Potion"), potion.Id, Charges: null);

        WorldUnit equippedLeader = new(new WorldUnitId("equippedLeader"), leaderDefinition.Id, leaderDefinition.MaxHitpoints, 0, 1, [UnitModifier.FromEquipment(weaponModifier.Id)], null, false);

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Stack1"),
                player.Id,
                new WorldPosition(0, 0),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(equippedLeader, GridPosition.FrontBottom, false)
                    .Build(),
                movementPoints: 10) with
            {
                Inventory = new WorldInventory([potionItem]),
                Equipment = LeaderEquipment.Empty.With(LeaderEquipmentSlot.Artifact1, weaponItem)
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(weaponModifier)
            .WithDefinition(weapon)
            .WithDefinition(potion)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldEquipItemCommand(stack.Id, weaponItem.Id, LeaderEquipmentSlot.Artifact2),
            [
                new WorldItemUnequippedEvent(stack.Id, LeaderEquipmentSlot.Artifact1),
                new WorldItemEquippedEvent(stack.Id, weaponItem, LeaderEquipmentSlot.Artifact2)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Inventory = new WorldInventory([potionItem]),
                    Equipment = LeaderEquipment.Empty.With(LeaderEquipmentSlot.Artifact2, weaponItem),
                    Units = new UnitGrid<WorldUnit>.Builder()
                        .Place(equippedLeader, GridPosition.FrontBottom, false)
                        .Build()
                })
            }));
    }

    [Test]
    public void MoveEquippedItemIntoWrongSlotTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 10, leadership: 6) with
            {
                LeaderAbilities = LeaderAbility.WeaponArmorUse
            };

        ItemDefinition weapon = new(
            new ItemDefinitionId("weapon1"),
            new GlobalTextId("name"),
            new GlobalTextId("desc"),
            ItemCategory.Weapon,
            WorldResourceSet.Empty,
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: null,
            AnimationUnitId: null);

        WorldItem weaponItem = new(new WorldItemId("Weapon"), weapon.Id, Charges: null);

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Stack1"),
                player.Id,
                new WorldPosition(0, 0),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom, false)
                    .Build(),
                movementPoints: 10) with
            {
                Equipment = LeaderEquipment.Empty.With(LeaderEquipmentSlot.Artifact1, weaponItem)
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(weapon)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(
            new WorldEquipItemCommand(stack.Id, weaponItem.Id, LeaderEquipmentSlot.Battle1),
            WorldCommandError.ItemSlotMismatch);
    }
}
