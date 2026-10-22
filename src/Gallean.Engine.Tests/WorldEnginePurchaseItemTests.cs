namespace Gallean.Engine.Tests;

public sealed class WorldEnginePurchaseItemTests
{
    private static WorldStack Stack(WorldUnitId leaderId, WorldPlayerId ownerId, WorldPosition position, UnitDefinition leader)
    {
        return TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            ownerId,
            position,
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(leaderId, leader), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 6);
    }

    private static ItemDefinition Item(string id, WorldResourceSet value)
    {
        return TestHelpers.SimpleItemDefinition(new ItemDefinitionId(id), ItemCategory.PotionHeal) with
        {
            Value = value,
            Effect = new HealItemEffect(50)
        };
    }

    private static WorldMerchantSite Site(IReadOnlyList<WorldMerchantItem> items)
    {
        return new WorldMerchantSite(
            new WorldSiteId("site"),
            new WorldFootprint(new WorldPosition(2, 2), 3, 3),
            Variant: new WorldObjectVariant(0),
            TitleTextId: new ScenarioTextId("Title"),
            GreetingTextId: new ScenarioTextId("Greeting"),
            [.. items],
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

        WorldStack stack = Stack(new WorldUnitId("leader"), player.Id, new WorldPosition(4, 4), leaderDefinition);
        WorldMerchantSite site = Site([new WorldMerchantItem(itemDefinition.Id, 2)]);

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
            new WorldPurchaseItemCommand(stack.Id, site.Id, itemDefinition.Id),
            [
                new WorldItemPurchasedEvent(
                    site.Id,
                    stack.Id,
                    new WorldItem(new WorldItemId("Item0001"), itemDefinition.Id, Charges: null),
                    new WorldResourceSet(50, 0, 0, 0, 0, 0))
            ]
        );

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Players = engine.State.Players.SetItem(player.Id, player with
                {
                    Resources = new WorldResourceSet(50, 0, 0, 0, 0, 0)
                }),
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Inventory = new WorldInventory([new WorldItem(new WorldItemId("Item0001"), itemDefinition.Id, Charges: null)])
                }),
                Sites = engine.State.Sites.SetItem(site.Id, site with
                {
                    Items = [new WorldMerchantItem(itemDefinition.Id, 1)]
                })
            }));
    }

    [Test]
    public void StockDepletedTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 7);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 4);
        ItemDefinition itemDefinition = Item("potion", new WorldResourceSet(50, 0, 0, 0, 0, 0));
        ItemDefinition otherDefinition = Item("scroll", new WorldResourceSet(20, 0, 0, 0, 0, 0));

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0)
        };

        WorldStack stack = Stack(new WorldUnitId("leader"), player.Id, new WorldPosition(4, 4), leaderDefinition);
        WorldMerchantSite site = Site([
            new WorldMerchantItem(itemDefinition.Id, 1),
            new WorldMerchantItem(otherDefinition.Id, 4)
        ]);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(itemDefinition)
            .WithDefinition(otherDefinition)
            .WithRace(TestHelpers.SimpleRace(WorldRace.Undead, new GlobalTextId("Undead Hordes"), WorldMapTerrain.Undead))
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(stack)
            .WithSite(site)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldPurchaseItemCommand(stack.Id, site.Id, itemDefinition.Id),
            [
                new WorldItemPurchasedEvent(
                    site.Id,
                    stack.Id,
                    new WorldItem(new WorldItemId("Item0001"), itemDefinition.Id, Charges: null),
                    new WorldResourceSet(50, 0, 0, 0, 0, 0))
            ]
        );

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Players = engine.State.Players.SetItem(player.Id, player with
                {
                    Resources = new WorldResourceSet(50, 0, 0, 0, 0, 0)
                }),
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Inventory = new WorldInventory([new WorldItem(new WorldItemId("Item0001"), itemDefinition.Id, Charges: null)])
                }),
                Sites = engine.State.Sites.SetItem(site.Id, site with
                {
                    Items = [new WorldMerchantItem(otherDefinition.Id, 4)]
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

        WorldStack stack = Stack(new WorldUnitId("leader"), player.Id, new WorldPosition(4, 4), leaderDefinition);
        WorldMerchantSite site = Site([new WorldMerchantItem(itemDefinition.Id, 2)]);

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
            new WorldPurchaseItemCommand(stack.Id, new WorldSiteId("missing"), itemDefinition.Id),
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

        WorldStack stack = Stack(new WorldUnitId("leader"), player.Id, new WorldPosition(6, 6), leaderDefinition);
        WorldMerchantSite site = Site([new WorldMerchantItem(itemDefinition.Id, 2)]);

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
            new WorldPurchaseItemCommand(stack.Id, site.Id, itemDefinition.Id),
            WorldCommandError.StackNotAtSite
        );
    }

    [Test]
    public void ItemNotAvailableTest()
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

        WorldStack stack = Stack(new WorldUnitId("leader"), player.Id, new WorldPosition(4, 4), leaderDefinition);
        WorldMerchantSite site = Site([new WorldMerchantItem(itemDefinition.Id, 2)]);

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
            new WorldPurchaseItemCommand(stack.Id, site.Id, new ItemDefinitionId("missing")),
            WorldCommandError.ItemNotAvailable
        );
    }

    [Test]
    public void InsufficientResourcesTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 7);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 4);
        ItemDefinition itemDefinition = Item("potion", new WorldResourceSet(50, 0, 0, 0, 0, 0));

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(10, 0, 0, 0, 0, 0)
        };

        WorldStack stack = Stack(new WorldUnitId("leader"), player.Id, new WorldPosition(4, 4), leaderDefinition);
        WorldMerchantSite site = Site([new WorldMerchantItem(itemDefinition.Id, 2)]);

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
            new WorldPurchaseItemCommand(stack.Id, site.Id, itemDefinition.Id),
            WorldCommandError.InsufficientResources
        );
    }

    [Test]
    public void TalismanTest()
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
            Resources = new WorldResourceSet(300, 0, 0, 0, 0, 0)
        };

        WorldStack stack = Stack(new WorldUnitId("leader"), player.Id, new WorldPosition(4, 4), leaderDefinition);
        WorldMerchantSite site = Site([new WorldMerchantItem(itemDefinition.Id, 1)]);

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
            new WorldPurchaseItemCommand(stack.Id, site.Id, itemDefinition.Id),
            [
                new WorldItemPurchasedEvent(
                    site.Id,
                    stack.Id,
                    new WorldItem(new WorldItemId("Item0001"), itemDefinition.Id, Charges: 5),
                    new WorldResourceSet(250, 0, 0, 0, 0, 0))
            ]
        );

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Players = engine.State.Players.SetItem(player.Id, player with
                {
                    Resources = new WorldResourceSet(50, 0, 0, 0, 0, 0)
                }),
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Inventory = new WorldInventory([new WorldItem(new WorldItemId("Item0001"), itemDefinition.Id, Charges: 5)])
                }),
                Sites = engine.State.Sites.SetItem(site.Id, site with
                {
                    Items = []
                })
            }));
    }
}
