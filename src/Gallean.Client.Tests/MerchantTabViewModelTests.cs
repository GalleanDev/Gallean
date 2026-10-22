using Gallean.Common;
using Gallean.Engine;
using Gallean.Engine.Tests;
using Gallean.Tests.Common;

namespace Gallean.Client.Tests;

public sealed class MerchantTabViewModelTests
{
    private static WorldStack Stack(
        WorldUnitId leaderId,
        WorldPlayerId ownerId,
        WorldPosition position,
        UnitDefinition leader,
        IReadOnlyList<WorldItem> inventory)
    {
        return TestHelpers.SimpleStack(
                new WorldStackId("Virgile"),
                ownerId,
                position,
                new UnitGrid<WorldUnit>.Builder()
                    .Place(new WorldUnit(leaderId, leader.Id, leader.MaxHitpoints, 0, leader.Level, [], null, false), GridPosition.FrontBottom, false)
                    .Build(),
                movementPoints: 20) with
            {
                Inventory = new WorldInventory([.. inventory])
            };
    }

    private static UnitModifierDefinition DiscountModifier(string id, int percent)
    {
        return TestHelpers.SimpleModifier(id) with { PurchaseDiscountPercent = percent };
    }

    private static WorldStack ModifiedStack(
        WorldUnitId leaderUnitId,
        WorldPlayerId ownerId,
        WorldPosition position,
        UnitDefinition leader,
        IReadOnlyList<UnitModifierDefinitionId> modifierIds)
    {
        WorldUnit leaderUnit = new(
            leaderUnitId,
            leader.Id,
            leader.MaxHitpoints,
            0,
            leader.Level,
            [.. modifierIds.Select(UnitModifier.Permanent)],
            Name: null,
            IsTypeLocked: false);

        return TestHelpers.SimpleStack(
            new WorldStackId("Virgile"),
            ownerId,
            position,
            new UnitGrid<WorldUnit>.Builder()
                .Place(leaderUnit, GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);
    }

    private static ItemDefinition Item(string id, WorldResourceSet value)
    {
        return new ItemDefinition(
            new ItemDefinitionId(id),
            new GlobalTextId("name"),
            new GlobalTextId("desc"),
            ItemCategory.PotionHeal,
            value,
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: new HealItemEffect(50),
            AnimationUnitId: null);
    }

    private static WorldMerchantSite Site(IReadOnlyList<WorldMerchantItem> items)
    {
        return new WorldMerchantSite(
            new WorldSiteId("shop"),
            new WorldFootprint(new WorldPosition(2, 2), 3, 3),
            Variant: new WorldObjectVariant(0),
            TitleTextId: new ScenarioTextId("Under's Shop"),
            GreetingTextId: new ScenarioTextId("My items are the best in the realm."),
            [.. items],
            VisitorIds: [],
            AiPriority: 3);
    }

    private static IReadOnlyList<string> FormatPurchaseItems(IReadOnlyList<MerchantPurchaseItemViewModel> items)
    {
        List<string> rows = [];

        foreach (MerchantPurchaseItemViewModel item in items)
        {
            rows.Add($"{item.ItemId.Value} | {item.Name} | {item.Description} | {item.CountText.Text} | Cost: {FormatPrice(Assert.NotNull(item.Status.Price))} | {FormatAvailability(item.Status)} | {FormatSelection(item.IsSelected)}");
        }

        return rows;
    }

    private static IReadOnlyList<string> FormatSellItems(IReadOnlyList<MerchantSellItemViewModel> items)
    {
        List<string> rows = [];

        foreach (MerchantSellItemViewModel item in items)
        {
            rows.Add($"{item.Name} | {item.Description} | Sell for: {WorldResourceFormatter.Format(item.Value.Resources)} | {FormatSellAvailability(item.UnavailableReason)} | {FormatSelection(item.IsSelected)}");
        }

        return rows;
    }

    private static string FormatOffer(SiteOfferViewModel offer)
    {
        return $"{offer.Title} | {offer.Subtitle.Text} | {offer.Description} | {FormatPrice(Assert.NotNull(offer.Status.Price))} | {offer.ActionText} | {FormatAvailability(offer.Status)} | {FormatEnabled(offer.ActCommand.CanExecute(null))}";
    }

    private static string FormatEnabled(bool isEnabled)
    {
        if (isEnabled)
        {
            return "enabled";
        }
        else
        {
            return "disabled";
        }
    }

    private static string FormatPrice(OfferPriceViewModel price)
    {
        List<string> items = [];

        foreach (OfferPriceItemViewModel item in price.Items)
        {
            if (item.IsShort)
            {
                items.Add($"{item.Resource.Value} {item.Resource.Name.ToLowerInvariant()} (short)");
            }
            else
            {
                items.Add($"{item.Resource.Value} {item.Resource.Name.ToLowerInvariant()}");
            }
        }

        return string.Join(", ", items);
    }

    private static string FormatSellAvailability(string? unavailableReason)
    {
        if (unavailableReason is null)
        {
            return "available";
        }
        else
        {
            return $"unavailable: {unavailableReason}";
        }
    }

    private static string FormatAvailability(OfferStatusViewModel status)
    {
        if (status.IsAvailable)
        {
            return "available";
        }
        else if (status.UnavailableReason is null)
        {
            return "unavailable";
        }
        else
        {
            return $"unavailable: {status.UnavailableReason}";
        }
    }

    private static string FormatSelection(bool isSelected)
    {
        if (isSelected)
        {
            return "selected";
        }
        else
        {
            return "not selected";
        }
    }

    private static MerchantTabViewModel BuildScreen(GameSession session, IEngineContext engine, WorldSiteId siteId, WorldStackId stackId)
    {
        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel management = new(gameModel, gameViewModel, ScreenMusic.Keep, null, ResourceSetProjection.Gold, owner => [new MerchantTabViewModel(gameModel, gameViewModel, owner, siteId, stackId)]);

        return Assert.IsOfType<MerchantTabViewModel>(management.Tabs[0]);
    }

    [Test]
    public async Task BuyTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0)
        };

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 4);
        ItemDefinition itemDefinition = Item("potion", new WorldResourceSet(50, 0, 0, 0, 0, 0));

        WorldStack stack = Stack(new WorldUnitId("leader"), player.Id, new WorldPosition(4, 4), leaderDefinition, []);
        WorldMerchantSite site = Site([new WorldMerchantItem(itemDefinition.Id, 2)]);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithRace(TestHelpers.SimpleRace(WorldRace.Human, new GlobalTextId("Empire"), WorldMapTerrain.Human))
            .WithDefinition(leaderDefinition)
            .WithDefinition(itemDefinition)
            .WithStack(stack)
            .WithSite(site)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        MerchantTabViewModel screen = BuildScreen(session, engine, site.Id, stack.Id);

        Assert.That(FormatPurchaseItems(screen.PurchaseItems), Is.EqualTo([
            "potion | name | desc | In stock: 2 | Cost: 50 gold | available | not selected"
        ]));

        Assert.That(screen.Offer, Is.Null);

        screen.PurchaseItems[0].SelectCommand.Execute(null);

        Assert.That(FormatOffer(Assert.NotNull(screen.Offer)), Is.EqualTo("name | In stock: 2 | desc | 50 gold | Buy | available | enabled"));
        Assert.That(FormatPurchaseItems(screen.PurchaseItems), Is.EqualTo([
            "potion | name | desc | In stock: 2 | Cost: 50 gold | available | selected"
        ]));

        await Assert.NotNull(screen.Offer).ActCommand.ExecuteAsync(null);

        Assert.That(session.World.State.Stacks[stack.Id].Inventory, Is.EqualTo(new WorldInventory([new WorldItem(new WorldItemId("Item0001"), itemDefinition.Id, Charges: null)])));
        Assert.That(session.World.State.Players[player.Id].Resources, Is.EqualTo(new WorldResourceSet(50, 0, 0, 0, 0, 0)));
        Assert.That(FormatOffer(Assert.NotNull(screen.Offer)), Is.EqualTo("name | In stock: 1 | desc | 50 gold | Buy | available | enabled"));
        Assert.That(FormatPurchaseItems(screen.PurchaseItems), Is.EqualTo([
            "potion | name | desc | In stock: 1 | Cost: 50 gold | available | selected"
        ]));
        Assert.That(FormatSellItems(screen.SellItems), Is.EqualTo([
            "name | desc | Sell for: 10 gold | available | not selected"
        ]));
        Assert.That(screen.Game.Alert.Current, Is.Null);
    }

    [Test]
    public async Task StockDepletedTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0)
        };

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 4);
        ItemDefinition itemDefinition = Item("potion", new WorldResourceSet(50, 0, 0, 0, 0, 0));

        WorldStack stack = Stack(new WorldUnitId("leader"), player.Id, new WorldPosition(4, 4), leaderDefinition, []);
        WorldMerchantSite site = Site([new WorldMerchantItem(itemDefinition.Id, 1)]);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithRace(TestHelpers.SimpleRace(WorldRace.Human, new GlobalTextId("Empire"), WorldMapTerrain.Human))
            .WithDefinition(leaderDefinition)
            .WithDefinition(itemDefinition)
            .WithStack(stack)
            .WithSite(site)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        MerchantTabViewModel screen = BuildScreen(session, engine, site.Id, stack.Id);

        screen.PurchaseItems[0].SelectCommand.Execute(null);
        await Assert.NotNull(screen.Offer).ActCommand.ExecuteAsync(null);

        Assert.That(session.World.State.Stacks[stack.Id].Inventory, Is.EqualTo(new WorldInventory([new WorldItem(new WorldItemId("Item0001"), itemDefinition.Id, Charges: null)])));
        Assert.That(FormatPurchaseItems(screen.PurchaseItems), Is.Empty);
        Assert.That(screen.Offer, Is.Null);
    }

    [Test]
    public void InsufficientFundsTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map) with
        {
            Resources = new WorldResourceSet(10, 0, 0, 0, 0, 0)
        };

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 4);
        ItemDefinition itemDefinition = Item("potion", new WorldResourceSet(50, 0, 0, 0, 0, 0));

        WorldStack stack = Stack(new WorldUnitId("leader"), player.Id, new WorldPosition(4, 4), leaderDefinition, []);
        WorldMerchantSite site = Site([new WorldMerchantItem(itemDefinition.Id, 2)]);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithRace(TestHelpers.SimpleRace(WorldRace.Human, new GlobalTextId("Empire"), WorldMapTerrain.Human))
            .WithDefinition(leaderDefinition)
            .WithDefinition(itemDefinition)
            .WithStack(stack)
            .WithSite(site)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        MerchantTabViewModel screen = BuildScreen(session, engine, site.Id, stack.Id);

        Assert.That(FormatPurchaseItems(screen.PurchaseItems), Is.EqualTo([
            "potion | name | desc | In stock: 2 | Cost: 50 gold (short) | unavailable | not selected"
        ]));

        screen.PurchaseItems[0].SelectCommand.Execute(null);

        Assert.That(FormatOffer(Assert.NotNull(screen.Offer)), Is.EqualTo("name | In stock: 2 | desc | 50 gold (short) | Buy | unavailable: Not enough resources | disabled"));
    }

    [Test]
    public async Task SellTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0)
        };

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 4);
        ItemDefinition itemDefinition = Item("potion", new WorldResourceSet(50, 0, 0, 0, 0, 0));
        WorldItem potionItem = new(new WorldItemId("Potion"), itemDefinition.Id, Charges: null);
        WorldItem secondPotionItem = new(new WorldItemId("SecondPotion"), itemDefinition.Id, Charges: null);

        WorldStack stack = Stack(new WorldUnitId("leader"), player.Id, new WorldPosition(4, 4), leaderDefinition, [potionItem, secondPotionItem]);
        WorldMerchantSite site = Site([]);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithRace(TestHelpers.SimpleRace(WorldRace.Human, new GlobalTextId("Empire"), WorldMapTerrain.Human))
            .WithDefinition(leaderDefinition)
            .WithDefinition(itemDefinition)
            .WithStack(stack)
            .WithSite(site)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        MerchantTabViewModel screen = BuildScreen(session, engine, site.Id, stack.Id);

        Assert.That(screen.PurchaseItems, Is.Empty);
        Assert.That(FormatSellItems(screen.SellItems), Is.EqualTo([
            "name | desc | Sell for: 10 gold | available | not selected",
            "name | desc | Sell for: 10 gold | available | not selected"
        ]));

        screen.SellItems[0].SelectCommand.Execute(null);

        Assert.That(FormatOffer(Assert.NotNull(screen.Offer)), Is.EqualTo("name | From your inventory | desc | 10 gold | Sell | available | enabled"));

        await Assert.NotNull(screen.Offer).ActCommand.ExecuteAsync(null);

        Assert.That(session.World.State.Stacks[stack.Id].Inventory, Is.EqualTo(new WorldInventory([secondPotionItem])));
        Assert.That(session.World.State.Players[player.Id].Resources, Is.EqualTo(new WorldResourceSet(110, 0, 0, 0, 0, 0)));
        Assert.That(screen.Offer, Is.Null);
        Assert.That(FormatSellItems(screen.SellItems), Is.EqualTo([
            "name | desc | Sell for: 10 gold | available | not selected"
        ]));
        Assert.That(screen.Game.Alert.Current, Is.Null);
    }

    [Test]
    public void SellSelectionTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0)
        };

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 4);
        ItemDefinition wares = Item("scroll", new WorldResourceSet(30, 0, 0, 0, 0, 0));
        ItemDefinition owned = Item("potion", new WorldResourceSet(50, 0, 0, 0, 0, 0));
        WorldItem ownedItem = new(new WorldItemId("Potion"), owned.Id, Charges: null);

        WorldStack stack = Stack(new WorldUnitId("leader"), player.Id, new WorldPosition(4, 4), leaderDefinition, [ownedItem]);
        WorldMerchantSite site = Site([new WorldMerchantItem(wares.Id, 2)]);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithRace(TestHelpers.SimpleRace(WorldRace.Human, new GlobalTextId("Empire"), WorldMapTerrain.Human))
            .WithDefinition(leaderDefinition)
            .WithDefinition(wares)
            .WithDefinition(owned)
            .WithStack(stack)
            .WithSite(site)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        MerchantTabViewModel screen = BuildScreen(session, engine, site.Id, stack.Id);

        screen.PurchaseItems[0].SelectCommand.Execute(null);

        Assert.That(FormatOffer(Assert.NotNull(screen.Offer)), Is.EqualTo("name | In stock: 2 | desc | 30 gold | Buy | available | enabled"));
        Assert.That(FormatPurchaseItems(screen.PurchaseItems), Is.EqualTo([
            "scroll | name | desc | In stock: 2 | Cost: 30 gold | available | selected"
        ]));
        Assert.That(FormatSellItems(screen.SellItems), Is.EqualTo([
            "name | desc | Sell for: 10 gold | available | not selected"
        ]));

        screen.SellItems[0].SelectCommand.Execute(null);

        Assert.That(FormatOffer(Assert.NotNull(screen.Offer)), Is.EqualTo("name | From your inventory | desc | 10 gold | Sell | available | enabled"));
        Assert.That(FormatPurchaseItems(screen.PurchaseItems), Is.EqualTo([
            "scroll | name | desc | In stock: 2 | Cost: 30 gold | available | not selected"
        ]));
        Assert.That(FormatSellItems(screen.SellItems), Is.EqualTo([
            "name | desc | Sell for: 10 gold | available | selected"
        ]));
    }

    [Test]
    public async Task DiscountedCostTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0)
        };

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 4);
        UnitModifierDefinition discount = DiscountModifier("discount", 10);
        ItemDefinition itemDefinition = Item("potion", new WorldResourceSet(50, 0, 0, 0, 0, 0));

        WorldStack stack = ModifiedStack(new WorldUnitId("leaderUnit"), player.Id, new WorldPosition(4, 4), leaderDefinition, [discount.Id]);
        WorldMerchantSite site = Site([new WorldMerchantItem(itemDefinition.Id, 2)]);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithRace(TestHelpers.SimpleRace(WorldRace.Human, new GlobalTextId("Empire"), WorldMapTerrain.Human))
            .WithDefinition(leaderDefinition)
            .WithDefinition(discount)
            .WithDefinition(itemDefinition)
            .WithStack(stack)
            .WithSite(site)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        MerchantTabViewModel screen = BuildScreen(session, engine, site.Id, stack.Id);

        Assert.That(FormatPurchaseItems(screen.PurchaseItems), Is.EqualTo([
            "potion | name | desc | In stock: 2 | Cost: 45 gold | available | not selected"
        ]));

        screen.PurchaseItems[0].SelectCommand.Execute(null);

        Assert.That(FormatOffer(Assert.NotNull(screen.Offer)), Is.EqualTo("name | In stock: 2 | desc | 45 gold | Buy | available | enabled"));

        await Assert.NotNull(screen.Offer).ActCommand.ExecuteAsync(null);

        Assert.That(session.World.State.Players[player.Id].Resources, Is.EqualTo(new WorldResourceSet(55, 0, 0, 0, 0, 0)));
    }

    [Test]
    public async Task DragTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0)
        };

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 4);
        ItemDefinition wares = Item("scroll", new WorldResourceSet(30, 0, 0, 0, 0, 0));
        ItemDefinition owned = Item("potion", new WorldResourceSet(50, 0, 0, 0, 0, 0));
        WorldItem ownedItem = new(new WorldItemId("Potion"), owned.Id, Charges: null);

        WorldStack stack = Stack(new WorldUnitId("leader"), player.Id, new WorldPosition(4, 4), leaderDefinition, [ownedItem]);
        WorldMerchantSite site = Site([new WorldMerchantItem(wares.Id, 2)]);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithRace(TestHelpers.SimpleRace(WorldRace.Human, new GlobalTextId("Empire"), WorldMapTerrain.Human))
            .WithDefinition(leaderDefinition)
            .WithDefinition(wares)
            .WithDefinition(owned)
            .WithStack(stack)
            .WithSite(site)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        MerchantTabViewModel screen = BuildScreen(session, engine, site.Id, stack.Id);

        Assert.That(screen.BuyZone.IsVisible, Is.False);
        Assert.That(screen.SellZone.IsVisible, Is.False);

        screen.DragDrop.TryBeginDrag(screen.PurchaseItems[0].Reference);

        Assert.That(screen.BuyZone.IsVisible, Is.True);
        Assert.That(screen.BuyZone.Content, Is.EqualTo(new MerchantTradeDropViewModel("Buy name", new ResourceSetViewModel(new WorldResourceSet(30, 0, 0, 0, 0, 0), ResourceSetProjection.All))));
        Assert.That(screen.SellZone.IsVisible, Is.False);

        await screen.DragDrop.DropAsync(screen.BuyZone.DropReference);

        Assert.That(session.World.State.Stacks[stack.Id].Inventory, Is.EqualTo(new WorldInventory([ownedItem, new WorldItem(new WorldItemId("Item0001"), wares.Id, Charges: null)])));
        Assert.That(session.World.State.Players[player.Id].Resources, Is.EqualTo(new WorldResourceSet(70, 0, 0, 0, 0, 0)));
        Assert.That(screen.BuyZone.IsVisible, Is.False);
        Assert.That(screen.Offer, Is.Null);

        screen.DragDrop.TryBeginDrag(screen.SellItems[0].Reference);

        Assert.That(screen.BuyZone.IsVisible, Is.False);
        Assert.That(screen.SellZone.IsVisible, Is.True);
        Assert.That(screen.SellZone.Content, Is.EqualTo(new MerchantTradeDropViewModel("Sell name", new ResourceSetViewModel(new WorldResourceSet(10, 0, 0, 0, 0, 0), ResourceSetProjection.All))));

        await screen.DragDrop.DropAsync(screen.SellZone.DropReference);

        Assert.That(session.World.State.Stacks[stack.Id].Inventory, Is.EqualTo(new WorldInventory([new WorldItem(new WorldItemId("Item0001"), wares.Id, Charges: null)])));
        Assert.That(session.World.State.Players[player.Id].Resources, Is.EqualTo(new WorldResourceSet(80, 0, 0, 0, 0, 0)));
        Assert.That(screen.SellZone.IsVisible, Is.False);
        Assert.That(screen.Game.Alert.Current, Is.Null);
    }

    [Test]
    public async Task InsufficientFundsDragTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map) with
        {
            Resources = new WorldResourceSet(10, 0, 0, 0, 0, 0)
        };

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 4);
        ItemDefinition itemDefinition = Item("potion", new WorldResourceSet(50, 0, 0, 0, 0, 0));

        WorldStack stack = Stack(new WorldUnitId("leader"), player.Id, new WorldPosition(4, 4), leaderDefinition, []);
        WorldMerchantSite site = Site([new WorldMerchantItem(itemDefinition.Id, 2)]);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithRace(TestHelpers.SimpleRace(WorldRace.Human, new GlobalTextId("Empire"), WorldMapTerrain.Human))
            .WithDefinition(leaderDefinition)
            .WithDefinition(itemDefinition)
            .WithStack(stack)
            .WithSite(site)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        MerchantTabViewModel screen = BuildScreen(session, engine, site.Id, stack.Id);

        screen.DragDrop.TryBeginDrag(screen.PurchaseItems[0].Reference);

        Assert.That(screen.BuyZone.IsVisible, Is.True);
        Assert.That(screen.BuyZone.Content, Is.EqualTo(new MerchantTradeDropViewModel("Buy name", new ResourceSetViewModel(new WorldResourceSet(50, 0, 0, 0, 0, 0), ResourceSetProjection.All))));

        await screen.DragDrop.DropAsync(screen.BuyZone.DropReference);

        Assert.That(session.World.State.Stacks[stack.Id].Inventory, Is.EqualTo(new WorldInventory([])));
        Assert.That(session.World.State.Players[player.Id].Resources, Is.EqualTo(new WorldResourceSet(10, 0, 0, 0, 0, 0)));
        Assert.That(screen.BuyZone.IsVisible, Is.False);
        Assert.That(screen.Game.Alert.Current, Is.EqualTo(new Alert(AlertType.Error, IconMoniker.Warning, "Not enough resources", Details: null)));
    }

    [Test]
    public void DetailsTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 4);
        ItemDefinition stockDefinition = Item("potion", new WorldResourceSet(50, 0, 0, 0, 0, 0));
        ItemDefinition ownedDefinition = Item("elixir", new WorldResourceSet(80, 0, 0, 0, 0, 0));
        WorldItem ownedItem = new(new WorldItemId("Elixir"), ownedDefinition.Id, Charges: null);

        WorldStack stack = Stack(new WorldUnitId("leader"), player.Id, new WorldPosition(4, 4), leaderDefinition, [ownedItem]);
        WorldMerchantSite site = Site([new WorldMerchantItem(stockDefinition.Id, 2)]);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithRace(TestHelpers.SimpleRace(WorldRace.Human, new GlobalTextId("Empire"), WorldMapTerrain.Human))
            .WithDefinition(leaderDefinition)
            .WithDefinition(stockDefinition)
            .WithDefinition(ownedDefinition)
            .WithStack(stack)
            .WithSite(site)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        MerchantTabViewModel screen = BuildScreen(session, engine, site.Id, stack.Id);

        Assert.That(screen.PurchaseItems[0].Details, Is.EqualTo(new ItemDetailsViewModel(
            new AssetSpriteKey.ItemIcon(new AssetSpriteKey.ItemIconSource.Item(stockDefinition.Id)),
            "name",
            "Healing potion",
            "desc",
            [],
            new ResourceSetViewModel(new WorldResourceSet(50, 0, 0, 0, 0, 0), ResourceSetProjection.All))));
        Assert.That(screen.SellItems[0].Details, Is.EqualTo(new ItemDetailsViewModel(
            new AssetSpriteKey.ItemIcon(new AssetSpriteKey.ItemIconSource.Item(ownedDefinition.Id)),
            "name",
            "Healing potion",
            "desc",
            [],
            new ResourceSetViewModel(new WorldResourceSet(80, 0, 0, 0, 0, 0), ResourceSetProjection.All))));
    }
}
