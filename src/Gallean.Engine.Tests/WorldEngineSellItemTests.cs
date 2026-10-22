namespace Gallean.Engine.Tests;

public sealed class WorldEngineSellItemTests
{
    private static WorldStack Stack(
        WorldUnitId leaderId,
        WorldPlayerId ownerId,
        WorldPosition position,
        UnitDefinition leader,
        IReadOnlyList<WorldItem> inventory)
    {
        return TestHelpers.SimpleStack(
                new WorldStackId("stack"),
                ownerId,
                position,
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(leaderId, leader), GridPosition.FrontBottom, false)
                    .Build(),
                movementPoints: 6) with
            {
                Inventory = new WorldInventory([.. inventory])
            };
    }

    private static ItemDefinition Item(string id, WorldResourceSet value)
    {
        return TestHelpers.SimpleItemDefinition(new ItemDefinitionId(id), ItemCategory.PotionHeal) with
        {
            Value = value,
            Effect = new HealItemEffect(50)
        };
    }

    private static WorldMerchantSite Site()
    {
        return new WorldMerchantSite(
            new WorldSiteId("site"),
            new WorldFootprint(new WorldPosition(2, 2), 3, 3),
            Variant: new WorldObjectVariant(0),
            TitleTextId: new ScenarioTextId("Title"),
            GreetingTextId: new ScenarioTextId("Greeting"),
            [],
            VisitorIds: [],
            AiPriority: 3);
    }

    [Test]
    public void SuccessTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 7);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 4);
        ItemDefinition itemDefinition = Item("potion", new WorldResourceSet(50, 0, 0, 0, 0, 0));

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0)
        };

        WorldItem potionItem = new(new WorldItemId("Potion"), itemDefinition.Id, Charges: null);
        WorldStack stack = Stack(new WorldUnitId("leader"), player.Id, new WorldPosition(4, 4), leaderDefinition, [potionItem]);
        WorldMerchantSite site = Site();

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(itemDefinition)
            .WithRace(TestHelpers.SimpleRace(WorldRace.Undead, new GlobalTextId("Undead Hordes"), WorldMapTerrain.Undead))
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(stack)
            .WithSite(site)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldSellItemCommand(stack.Id, site.Id, potionItem.Id),
            [
                new WorldItemSoldEvent(
                    site.Id,
                    stack.Id,
                    potionItem,
                    new WorldResourceSet(10, 0, 0, 0, 0, 0))
            ]
        );

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Players = engine.State.Players.SetItem(player.Id, player with
                {
                    Resources = new WorldResourceSet(110, 0, 0, 0, 0, 0)
                }),
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Inventory = WorldInventory.Empty
                })
            }));
    }

    [Test]
    public void MultiCurrencyValueTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 7);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 4);
        ItemDefinition itemDefinition = Item("potion", new WorldResourceSet(37, 4, 15, 9, 100, 0));

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0)
        };

        WorldItem potionItem = new(new WorldItemId("Potion"), itemDefinition.Id, Charges: null);
        WorldStack stack = Stack(new WorldUnitId("leader"), player.Id, new WorldPosition(4, 4), leaderDefinition, [potionItem]);
        WorldMerchantSite site = Site();

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(itemDefinition)
            .WithRace(TestHelpers.SimpleRace(WorldRace.Undead, new GlobalTextId("Undead Hordes"), WorldMapTerrain.Undead))
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(stack)
            .WithSite(site)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldSellItemCommand(stack.Id, site.Id, potionItem.Id),
            [
                new WorldItemSoldEvent(
                    site.Id,
                    stack.Id,
                    potionItem,
                    new WorldResourceSet(7, 0, 3, 1, 20, 0))
            ]
        );

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Players = engine.State.Players.SetItem(player.Id, player with
                {
                    Resources = new WorldResourceSet(107, 0, 3, 1, 20, 0)
                }),
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Inventory = WorldInventory.Empty
                })
            }));
    }

    [Test]
    public void DuplicateItemTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 7);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 4);
        ItemDefinition itemDefinition = Item("potion", new WorldResourceSet(50, 0, 0, 0, 0, 0));

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0)
        };

        WorldItem potionItem = new(new WorldItemId("Potion"), itemDefinition.Id, Charges: null);
        WorldItem secondPotionItem = new(new WorldItemId("SecondPotion"), itemDefinition.Id, Charges: null);
        WorldStack stack = Stack(
            new WorldUnitId("leader"),
            player.Id,
            new WorldPosition(4, 4),
            leaderDefinition,
            [potionItem, secondPotionItem]);
        WorldMerchantSite site = Site();

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(itemDefinition)
            .WithRace(TestHelpers.SimpleRace(WorldRace.Undead, new GlobalTextId("Undead Hordes"), WorldMapTerrain.Undead))
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(stack)
            .WithSite(site)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldSellItemCommand(stack.Id, site.Id, potionItem.Id),
            [
                new WorldItemSoldEvent(
                    site.Id,
                    stack.Id,
                    potionItem,
                    new WorldResourceSet(10, 0, 0, 0, 0, 0))
            ]
        );

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Players = engine.State.Players.SetItem(player.Id, player with
                {
                    Resources = new WorldResourceSet(110, 0, 0, 0, 0, 0)
                }),
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Inventory = new WorldInventory([secondPotionItem])
                })
            }));
    }

    [Test]
    public void SiteNotFoundTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 7);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 4);
        ItemDefinition itemDefinition = Item("potion", new WorldResourceSet(50, 0, 0, 0, 0, 0));

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0)
        };

        WorldItem potionItem = new(new WorldItemId("Potion"), itemDefinition.Id, Charges: null);
        WorldStack stack = Stack(new WorldUnitId("leader"), player.Id, new WorldPosition(4, 4), leaderDefinition, [potionItem]);
        WorldMerchantSite site = Site();

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(itemDefinition)
            .WithRace(TestHelpers.SimpleRace(WorldRace.Undead, new GlobalTextId("Undead Hordes"), WorldMapTerrain.Undead))
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(stack)
            .WithSite(site)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(
            new WorldSellItemCommand(stack.Id, new WorldSiteId("missing"), potionItem.Id),
            WorldCommandError.SiteNotFound
        );
    }

    [Test]
    public void NotAtSiteTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 7);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 4);
        ItemDefinition itemDefinition = Item("potion", new WorldResourceSet(50, 0, 0, 0, 0, 0));

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0)
        };

        WorldItem potionItem = new(new WorldItemId("Potion"), itemDefinition.Id, Charges: null);
        WorldStack stack = Stack(new WorldUnitId("leader"), player.Id, new WorldPosition(6, 6), leaderDefinition, [potionItem]);
        WorldMerchantSite site = Site();

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(itemDefinition)
            .WithRace(TestHelpers.SimpleRace(WorldRace.Undead, new GlobalTextId("Undead Hordes"), WorldMapTerrain.Undead))
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(stack)
            .WithSite(site)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(
            new WorldSellItemCommand(stack.Id, site.Id, potionItem.Id),
            WorldCommandError.StackNotAtSite
        );
    }

    [Test]
    public void ItemNotFoundTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 7);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 4);
        ItemDefinition itemDefinition = Item("potion", new WorldResourceSet(50, 0, 0, 0, 0, 0));

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0)
        };

        WorldStack stack = Stack(new WorldUnitId("leader"), player.Id, new WorldPosition(4, 4), leaderDefinition, []);
        WorldMerchantSite site = Site();

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(itemDefinition)
            .WithRace(TestHelpers.SimpleRace(WorldRace.Undead, new GlobalTextId("Undead Hordes"), WorldMapTerrain.Undead))
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(stack)
            .WithSite(site)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(
            new WorldSellItemCommand(stack.Id, site.Id, new WorldItemId("Potion")),
            WorldCommandError.ItemNotFound
        );
    }

    [Test]
    public void TalismanChargesTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 7);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 4);
        ItemDefinition itemDefinition = Item("talisman", new WorldResourceSet(50, 0, 0, 0, 0, 0)) with
        {
            Category = ItemCategory.Talisman
        };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0)
        };

        WorldItem talismanItem = new(new WorldItemId("Talisman"), itemDefinition.Id, Charges: 3);
        WorldStack stack = Stack(new WorldUnitId("leader"), player.Id, new WorldPosition(4, 4), leaderDefinition, [talismanItem]);
        WorldMerchantSite site = Site();

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(itemDefinition)
            .WithRace(TestHelpers.SimpleRace(WorldRace.Undead, new GlobalTextId("Undead Hordes"), WorldMapTerrain.Undead))
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(stack)
            .WithSite(site)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldSellItemCommand(stack.Id, site.Id, talismanItem.Id),
            [
                new WorldItemSoldEvent(
                    site.Id,
                    stack.Id,
                    talismanItem,
                    new WorldResourceSet(30, 0, 0, 0, 0, 0))
            ]
        );

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Players = engine.State.Players.SetItem(player.Id, player with
                {
                    Resources = new WorldResourceSet(130, 0, 0, 0, 0, 0)
                }),
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Inventory = WorldInventory.Empty
                })
            }));
    }

    [Test]
    public void TalismanFullChargesTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 7);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 4);
        ItemDefinition itemDefinition = Item("talisman", new WorldResourceSet(50, 0, 0, 0, 0, 0)) with
        {
            Category = ItemCategory.Talisman
        };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0)
        };

        WorldItem talismanItem = new(new WorldItemId("Talisman"), itemDefinition.Id, Charges: 5);
        WorldStack stack = Stack(new WorldUnitId("leader"), player.Id, new WorldPosition(4, 4), leaderDefinition, [talismanItem]);
        WorldMerchantSite site = Site();

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(itemDefinition)
            .WithRace(TestHelpers.SimpleRace(WorldRace.Undead, new GlobalTextId("Undead Hordes"), WorldMapTerrain.Undead))
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(stack)
            .WithSite(site)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldSellItemCommand(stack.Id, site.Id, talismanItem.Id),
            [
                new WorldItemSoldEvent(
                    site.Id,
                    stack.Id,
                    talismanItem,
                    new WorldResourceSet(50, 0, 0, 0, 0, 0))
            ]
        );

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Players = engine.State.Players.SetItem(player.Id, player with
                {
                    Resources = new WorldResourceSet(150, 0, 0, 0, 0, 0)
                }),
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Inventory = WorldInventory.Empty
                })
            }));
    }
}
