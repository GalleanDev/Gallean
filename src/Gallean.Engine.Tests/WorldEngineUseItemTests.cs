namespace Gallean.Engine.Tests;

public sealed class WorldEngineUseItemTests
{
    private static ItemDefinition HealingPotion(int hitpoints)
    {
        return TestHelpers.SimpleItemDefinition(new ItemDefinitionId("potion1"), ItemCategory.PotionHeal) with
        {
            Effect = new HealItemEffect(hitpoints)
        };
    }

    private static ItemDefinition RevivePotion()
    {
        return TestHelpers.SimpleItemDefinition(new ItemDefinitionId("revive1"), ItemCategory.PotionRevive) with
        {
            Effect = new ReviveItemEffect()
        };
    }

    private static WorldStack Stack(WorldUnitId leaderId, UnitDefinition leaderDefinition, WorldUnit soldier, WorldPlayerId owner, IEnumerable<WorldItem> inventory)
    {
        return TestHelpers.SimpleStack(
                new WorldStackId("Stack1"),
                owner,
                new WorldPosition(0, 0),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(leaderId, leaderDefinition), GridPosition.BackBottom, false)
                    .Place(soldier, GridPosition.FrontBottom, false)
                    .Build(),
                movementPoints: 6) with
            {
                Inventory = new WorldInventory([.. inventory])
            };
    }

    [Test]
    public void HealTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 3);
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 10);
        ItemDefinition potion = HealingPotion(50);
        WorldItem potionItem = new(new WorldItemId("Potion"), potion.Id, Charges: null);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldStack stack = Stack(
            new WorldUnitId("leader2"),
            leaderDefinition,
            TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition) with { Hitpoints = 30 },
            player.Id,
            [potionItem]);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(potion)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        engine.CheckCommand(
            new WorldUseItemCommand(new WorldUnitGroup.Stack(stack.Id), potionItem.Id, new WorldItemTarget.Unit(new WorldUnitGroup.Stack(stack.Id), new WorldUnitId("soldier"))),
            [
                new WorldItemUsedEvent(new WorldUnitGroup.Stack(stack.Id), potionItem, RemainingCharges: null, Consumed: true),
                new WorldUnitHealedEvent(new WorldUnitGroup.Stack(stack.Id), new WorldUnitId("soldier"), 80, WorldResourceSet.Empty)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Inventory = WorldInventory.Empty,
                    Units = new UnitGrid<WorldUnit>.Builder()
                        .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader2"), leaderDefinition), GridPosition.BackBottom, false)
                        .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition) with { Hitpoints = 80 }, GridPosition.FrontBottom, false)
                        .Build()
                })
            }));
    }

    [Test]
    public void HealMaxHitpointsTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 3);
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 10);
        ItemDefinition potion = HealingPotion(50);
        WorldItem potionItem = new(new WorldItemId("Potion"), potion.Id, Charges: null);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldStack stack = Stack(
            new WorldUnitId("leader2"),
            leaderDefinition,
            TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition) with { Hitpoints = 80 },
            player.Id,
            [potionItem]);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(potion)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        engine.CheckCommand(
            new WorldUseItemCommand(new WorldUnitGroup.Stack(stack.Id), potionItem.Id, new WorldItemTarget.Unit(new WorldUnitGroup.Stack(stack.Id), new WorldUnitId("soldier"))),
            [
                new WorldItemUsedEvent(new WorldUnitGroup.Stack(stack.Id), potionItem, RemainingCharges: null, Consumed: true),
                new WorldUnitHealedEvent(new WorldUnitGroup.Stack(stack.Id), new WorldUnitId("soldier"), 100, WorldResourceSet.Empty)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Inventory = WorldInventory.Empty,
                    Units = new UnitGrid<WorldUnit>.Builder()
                        .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader2"), leaderDefinition), GridPosition.BackBottom, false)
                        .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition), GridPosition.FrontBottom, false)
                        .Build()
                })
            }));
    }

    [Test]
    public void NotWoundedTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 3);
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 10);
        ItemDefinition potion = HealingPotion(50);
        WorldItem potionItem = new(new WorldItemId("Potion"), potion.Id, Charges: null);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldStack stack = Stack(
            new WorldUnitId("leader"),
            leaderDefinition,
            TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition),
            player.Id,
            [potionItem]);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(potion)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        engine.CheckCommandError(
            new WorldUseItemCommand(new WorldUnitGroup.Stack(stack.Id), potionItem.Id, new WorldItemTarget.Unit(new WorldUnitGroup.Stack(stack.Id), new WorldUnitId("soldier"))),
            WorldCommandError.UnitNotWounded);
    }

    [Test]
    public void DeadUnitTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 3);
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 10);
        ItemDefinition potion = HealingPotion(50);
        WorldItem potionItem = new(new WorldItemId("Potion"), potion.Id, Charges: null);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldStack stack = Stack(
            new WorldUnitId("leader"),
            leaderDefinition,
            TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition) with { Hitpoints = 0 },
            player.Id,
            [potionItem]);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(potion)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        engine.CheckCommandError(
            new WorldUseItemCommand(new WorldUnitGroup.Stack(stack.Id), potionItem.Id, new WorldItemTarget.Unit(new WorldUnitGroup.Stack(stack.Id), new WorldUnitId("soldier"))),
            WorldCommandError.UnitDead);
    }

    [Test]
    public void UnknownUnitTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 3);
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 10);
        ItemDefinition potion = HealingPotion(50);
        WorldItem potionItem = new(new WorldItemId("Potion"), potion.Id, Charges: null);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldStack stack = Stack(
            new WorldUnitId("leader"),
            leaderDefinition,
            TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition) with { Hitpoints = 30 },
            player.Id,
            [potionItem]);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(potion)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        engine.CheckCommandError(
            new WorldUseItemCommand(new WorldUnitGroup.Stack(stack.Id), potionItem.Id, new WorldItemTarget.Unit(new WorldUnitGroup.Stack(stack.Id), new WorldUnitId("missing"))),
            WorldCommandError.UnitNotFound);
    }

    [Test]
    public void ItemNotInInventoryTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 3);
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 10);
        ItemDefinition potion = HealingPotion(50);
        WorldItem potionItem = new(new WorldItemId("Potion"), potion.Id, Charges: null);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldStack stack = Stack(
            new WorldUnitId("leader"),
            leaderDefinition,
            TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition) with { Hitpoints = 30 },
            player.Id,
            []);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(potion)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        engine.CheckCommandError(
            new WorldUseItemCommand(new WorldUnitGroup.Stack(stack.Id), potionItem.Id, new WorldItemTarget.Unit(new WorldUnitGroup.Stack(stack.Id), new WorldUnitId("soldier"))),
            WorldCommandError.ItemNotFound);
    }

    [Test]
    public void ItemNotUsableTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 3);
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 10);

        ItemDefinition weapon = TestHelpers.SimpleItemDefinition(new ItemDefinitionId("weapon1"), ItemCategory.Weapon);
        WorldItem weaponItem = new(new WorldItemId("Weapon"), weapon.Id, Charges: null);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldStack stack = Stack(
            new WorldUnitId("leader"),
            leaderDefinition,
            TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition) with { Hitpoints = 30 },
            player.Id,
            [weaponItem]);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(weapon)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        engine.CheckCommandError(
            new WorldUseItemCommand(new WorldUnitGroup.Stack(stack.Id), weaponItem.Id, new WorldItemTarget.Unit(new WorldUnitGroup.Stack(stack.Id), new WorldUnitId("soldier"))),
            WorldCommandError.ItemNotUsable);
    }

    [Test]
    public void ReviveTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 3);
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 10) with
        {
            ReviveCost = new WorldResourceSet(50, 0, 0, 0, 0, 0)
        };
        ItemDefinition potion = RevivePotion();
        WorldItem potionItem = new(new WorldItemId("Potion"), potion.Id, Charges: null);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldStack stack = Stack(
            new WorldUnitId("leader2"),
            leaderDefinition,
            TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition) with { Hitpoints = 0 },
            player.Id,
            [potionItem]);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(potion)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        engine.CheckCommand(
            new WorldUseItemCommand(new WorldUnitGroup.Stack(stack.Id), potionItem.Id, new WorldItemTarget.Unit(new WorldUnitGroup.Stack(stack.Id), new WorldUnitId("soldier"))),
            [
                new WorldItemUsedEvent(new WorldUnitGroup.Stack(stack.Id), potionItem, RemainingCharges: null, Consumed: true),
                new WorldUnitResurrectedEvent(new WorldUnitGroup.Stack(stack.Id), new WorldUnitId("soldier"), 1, WorldResourceSet.Empty)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Inventory = WorldInventory.Empty,
                    Units = new UnitGrid<WorldUnit>.Builder()
                        .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader2"), leaderDefinition), GridPosition.BackBottom, false)
                        .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition) with { Hitpoints = 1 }, GridPosition.FrontBottom, false)
                        .Build()
                })
            }));
    }

    [Test]
    public void ReviveAliveUnitTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 3);
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 10) with
        {
            ReviveCost = new WorldResourceSet(50, 0, 0, 0, 0, 0)
        };
        ItemDefinition potion = RevivePotion();
        WorldItem potionItem = new(new WorldItemId("Potion"), potion.Id, Charges: null);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldStack stack = Stack(
            new WorldUnitId("leader"),
            leaderDefinition,
            TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition) with { Hitpoints = 30 },
            player.Id,
            [potionItem]);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(potion)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        engine.CheckCommandError(
            new WorldUseItemCommand(new WorldUnitGroup.Stack(stack.Id), potionItem.Id, new WorldItemTarget.Unit(new WorldUnitGroup.Stack(stack.Id), new WorldUnitId("soldier"))),
            WorldCommandError.UnitNotDead);
    }

    [Test]
    public void ReviveForbiddenTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 3);
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 10) with
        {
            ReviveCost = WorldResourceSet.Empty
        };
        ItemDefinition potion = RevivePotion();
        WorldItem potionItem = new(new WorldItemId("Potion"), potion.Id, Charges: null);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldStack stack = Stack(
            new WorldUnitId("leader"),
            leaderDefinition,
            TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition) with { Hitpoints = 0 },
            player.Id,
            [potionItem]);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(potion)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        engine.CheckCommandError(
            new WorldUseItemCommand(new WorldUnitGroup.Stack(stack.Id), potionItem.Id, new WorldItemTarget.Unit(new WorldUnitGroup.Stack(stack.Id), new WorldUnitId("soldier"))),
            WorldCommandError.UnitResurrectForbidden);
    }

    [Test]
    public void AttackItemNotUsableTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 6, leadership: 3) with
            {
                LeaderAbilities = LeaderAbility.OrbUse
            };

        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 10);

        ItemDefinition orb = TestHelpers.SimpleItemDefinition(new ItemDefinitionId("orb1"), ItemCategory.Orb) with
        {
            Effect = new AttackItemEffect(new UnitAttackDefinitionId("orb-attack"))
        };
        WorldItem orbItem = new(new WorldItemId("Orb"), orb.Id, Charges: null);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldStack stack = Stack(
            new WorldUnitId("leader"),
            leaderDefinition,
            TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition) with { Hitpoints = 30 },
            player.Id,
            [orbItem]);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(orb)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        engine.CheckCommandError(
            new WorldUseItemCommand(new WorldUnitGroup.Stack(stack.Id), orbItem.Id, new WorldItemTarget.Unit(new WorldUnitGroup.Stack(stack.Id), new WorldUnitId("soldier"))),
            WorldCommandError.ItemNotUsable);
    }

    [Test]
    public void EquippedPotionTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 3);
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 10);
        ItemDefinition potion = HealingPotion(50);
        WorldItem potionItem = new(new WorldItemId("Potion"), potion.Id, Charges: null);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldStack stack = Stack(
                new WorldUnitId("leader2"),
                leaderDefinition,
                TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition) with { Hitpoints = 30 },
                player.Id,
                []) with
            {
                Equipment = LeaderEquipment.Empty.With(LeaderEquipmentSlot.Battle1, potionItem)
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(potion)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        engine.CheckCommand(
            new WorldUseItemCommand(new WorldUnitGroup.Stack(stack.Id), potionItem.Id, new WorldItemTarget.Unit(new WorldUnitGroup.Stack(stack.Id), new WorldUnitId("soldier"))),
            [
                new WorldItemUsedEvent(new WorldUnitGroup.Stack(stack.Id), potionItem, RemainingCharges: null, Consumed: true),
                new WorldUnitHealedEvent(new WorldUnitGroup.Stack(stack.Id), new WorldUnitId("soldier"), 80, WorldResourceSet.Empty)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Equipment = LeaderEquipment.Empty,
                    Units = new UnitGrid<WorldUnit>.Builder()
                        .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader2"), leaderDefinition), GridPosition.BackBottom, false)
                        .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition) with { Hitpoints = 80 }, GridPosition.FrontBottom, false)
                        .Build()
                })
            }));
    }

    [Test]
    public void GarrisonHealTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 10);
        ItemDefinition potion = HealingPotion(50);
        WorldItem potionItem = new(new WorldItemId("Potion"), potion.Id, Charges: null);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Undead, 1),
            WorldResourceSet.Empty);

        WorldCity city = TestHelpers.SimpleCity(
                new WorldFortificationId("city1"),
                cityDefinition,
                player.Id,
                new WorldPosition(3, 3),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition) with { Hitpoints = 30 }, GridPosition.FrontBottom, false)
                    .Build()) with
            {
                Inventory = new WorldInventory([potionItem])
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(soldierDefinition)
            .WithDefinition(potion)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCity(city)
            .Build();

        engine.CheckCommand(
            new WorldUseItemCommand(new WorldUnitGroup.Fortification(city.Id), potionItem.Id, new WorldItemTarget.Unit(new WorldUnitGroup.Fortification(city.Id), new WorldUnitId("soldier"))),
            [
                new WorldItemUsedEvent(new WorldUnitGroup.Fortification(city.Id), potionItem, RemainingCharges: null, Consumed: true),
                new WorldUnitHealedEvent(new WorldUnitGroup.Fortification(city.Id), new WorldUnitId("soldier"), 80, WorldResourceSet.Empty)
            ]);

        Assert.That(engine.State.Fortifications[city.Id], Is.EqualTo(city with
        {
            Inventory = WorldInventory.Empty,
            Garrison = new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition) with { Hitpoints = 80 }, GridPosition.FrontBottom, false)
                .Build()
        }));
    }

    [Test]
    public void GarrisonScrollTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 10);
        ItemDefinition scroll = TestHelpers.SimpleItemDefinition(new ItemDefinitionId("scroll1"), ItemCategory.Scroll) with
        {
            Effect = new SpellItemEffect(new WorldSpellDefinitionId("spell1"), WorldResourceSet.Empty)
        };
        WorldItem scrollItem = new(new WorldItemId("Scroll"), scroll.Id, Charges: null);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Undead, 1),
            WorldResourceSet.Empty);

        WorldCity city = TestHelpers.SimpleCity(
                new WorldFortificationId("city1"),
                cityDefinition,
                player.Id,
                new WorldPosition(3, 3),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition), GridPosition.FrontBottom, false)
                    .Build()) with
            {
                Inventory = new WorldInventory([scrollItem])
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(soldierDefinition)
            .WithDefinition(scroll)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCity(city)
            .Build();

        engine.CheckCommandError(
            new WorldUseItemCommand(new WorldUnitGroup.Fortification(city.Id), scrollItem.Id, Target: null),
            WorldCommandError.MissingLeaderAbility);
    }

    [Test]
    public void VisitingStackHealTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 3);
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 10);
        ItemDefinition potion = HealingPotion(50);
        WorldItem potionItem = new(new WorldItemId("Potion"), potion.Id, Charges: null);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Undead, 1),
            WorldResourceSet.Empty);

        WorldCity city = TestHelpers.SimpleCity(
                new WorldFortificationId("city1"),
                cityDefinition,
                player.Id,
                new WorldPosition(3, 3),
                UnitGrid<WorldUnit>.Empty) with
            {
                Inventory = new WorldInventory([potionItem])
            };

        WorldStack stack = Stack(
                new WorldUnitId("leader2"),
                leaderDefinition,
                TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition) with { Hitpoints = 30 },
                player.Id,
                []) with
            {
                Position = city.Entrance
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(potion)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCity(city)
            .WithStack(stack)
            .Build();

        engine.CheckCommand(
            new WorldUseItemCommand(new WorldUnitGroup.Fortification(city.Id), potionItem.Id, new WorldItemTarget.Unit(new WorldUnitGroup.Stack(stack.Id), new WorldUnitId("soldier"))),
            [
                new WorldItemUsedEvent(new WorldUnitGroup.Fortification(city.Id), potionItem, RemainingCharges: null, Consumed: true),
                new WorldUnitHealedEvent(new WorldUnitGroup.Stack(stack.Id), new WorldUnitId("soldier"), 80, WorldResourceSet.Empty)
            ]);

        Assert.That(engine.State.Fortifications[city.Id], Is.EqualTo(city with { Inventory = WorldInventory.Empty }));
        Assert.That(engine.State.Stacks[stack.Id], Is.EqualTo(stack with
        {
            Units = new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader2"), leaderDefinition), GridPosition.BackBottom, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition) with { Hitpoints = 80 }, GridPosition.FrontBottom, false)
                .Build()
        }));
    }

    [Test]
    public void AdjacentStackHealTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 3);
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 10);
        ItemDefinition potion = HealingPotion(50);
        WorldItem potionItem = new(new WorldItemId("Potion"), potion.Id, Charges: null);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldStack stack = Stack(
            new WorldUnitId("leader2"),
            leaderDefinition,
            TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition),
            player.Id,
            [potionItem]);

        WorldStack otherStack = Stack(
                new WorldUnitId("otherLeader"),
                leaderDefinition,
                TestHelpers.SimpleUnit(new WorldUnitId("otherSoldier"), soldierDefinition) with { Hitpoints = 30 },
                player.Id,
                []) with
            {
                Id = new WorldStackId("Stack2"),
                Position = new WorldPosition(1, 0)
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(potion)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .WithStack(otherStack)
            .Build();

        engine.CheckCommand(
            new WorldUseItemCommand(new WorldUnitGroup.Stack(stack.Id), potionItem.Id, new WorldItemTarget.Unit(new WorldUnitGroup.Stack(otherStack.Id), new WorldUnitId("otherSoldier"))),
            [
                new WorldItemUsedEvent(new WorldUnitGroup.Stack(stack.Id), potionItem, RemainingCharges: null, Consumed: true),
                new WorldUnitHealedEvent(new WorldUnitGroup.Stack(otherStack.Id), new WorldUnitId("otherSoldier"), 80, WorldResourceSet.Empty)
            ]);

        Assert.That(engine.State.Stacks[stack.Id], Is.EqualTo(stack with { Inventory = WorldInventory.Empty }));
        Assert.That(engine.State.Stacks[otherStack.Id], Is.EqualTo(otherStack with
        {
            Units = new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("otherLeader"), leaderDefinition), GridPosition.BackBottom, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("otherSoldier"), soldierDefinition) with { Hitpoints = 80 }, GridPosition.FrontBottom, false)
                .Build()
        }));
    }

    [Test]
    public void DistantStackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 3);
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 10);
        ItemDefinition potion = HealingPotion(50);
        WorldItem potionItem = new(new WorldItemId("Potion"), potion.Id, Charges: null);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldStack stack = Stack(
            new WorldUnitId("leader2"),
            leaderDefinition,
            TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition),
            player.Id,
            [potionItem]);

        WorldStack otherStack = Stack(
                new WorldUnitId("otherLeader"),
                leaderDefinition,
                TestHelpers.SimpleUnit(new WorldUnitId("otherSoldier"), soldierDefinition) with { Hitpoints = 30 },
                player.Id,
                []) with
            {
                Id = new WorldStackId("Stack2"),
                Position = new WorldPosition(4, 0)
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(potion)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .WithStack(otherStack)
            .Build();

        engine.CheckCommandError(
            new WorldUseItemCommand(new WorldUnitGroup.Stack(stack.Id), potionItem.Id, new WorldItemTarget.Unit(new WorldUnitGroup.Stack(otherStack.Id), new WorldUnitId("otherSoldier"))),
            WorldCommandError.StackNotAdjacent);
    }
}
