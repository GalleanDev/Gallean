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
            new WorldUseItemCommand(stack.Id, potionItem.Id, new WorldItemTarget.Unit(new WorldUnitId("soldier"))),
            [
                new WorldItemUsedEvent(stack.Id, potionItem, RemainingCharges: null, Consumed: true),
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
            new WorldUseItemCommand(stack.Id, potionItem.Id, new WorldItemTarget.Unit(new WorldUnitId("soldier"))),
            [
                new WorldItemUsedEvent(stack.Id, potionItem, RemainingCharges: null, Consumed: true),
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
            new WorldUseItemCommand(stack.Id, potionItem.Id, new WorldItemTarget.Unit(new WorldUnitId("soldier"))),
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
            new WorldUseItemCommand(stack.Id, potionItem.Id, new WorldItemTarget.Unit(new WorldUnitId("soldier"))),
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
            new WorldUseItemCommand(stack.Id, potionItem.Id, new WorldItemTarget.Unit(new WorldUnitId("missing"))),
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
            new WorldUseItemCommand(stack.Id, potionItem.Id, new WorldItemTarget.Unit(new WorldUnitId("soldier"))),
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
            new WorldUseItemCommand(stack.Id, weaponItem.Id, new WorldItemTarget.Unit(new WorldUnitId("soldier"))),
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
            new WorldUseItemCommand(stack.Id, potionItem.Id, new WorldItemTarget.Unit(new WorldUnitId("soldier"))),
            [
                new WorldItemUsedEvent(stack.Id, potionItem, RemainingCharges: null, Consumed: true),
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
            new WorldUseItemCommand(stack.Id, potionItem.Id, new WorldItemTarget.Unit(new WorldUnitId("soldier"))),
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
            new WorldUseItemCommand(stack.Id, potionItem.Id, new WorldItemTarget.Unit(new WorldUnitId("soldier"))),
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
            new WorldUseItemCommand(stack.Id, orbItem.Id, new WorldItemTarget.Unit(new WorldUnitId("soldier"))),
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
            new WorldUseItemCommand(stack.Id, potionItem.Id, new WorldItemTarget.Unit(new WorldUnitId("soldier"))),
            [
                new WorldItemUsedEvent(stack.Id, potionItem, RemainingCharges: null, Consumed: true),
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
}
