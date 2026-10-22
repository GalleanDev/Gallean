namespace Gallean.Engine.Tests;

public sealed class WorldEngineThiefStealItemTests
{
    private static ItemDefinition Item(string id)
    {
        return TestHelpers.SimpleItemDefinition(new ItemDefinitionId(id), ItemCategory.PotionHeal) with
        {
            Value = new WorldResourceSet(50, 0, 0, 0, 0, 0),
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
    public void StackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 7);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition warlord = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("warlord", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 10, leadership: 2);
        ItemDefinition itemDefinition = Item("potion");
        WorldItem potionItem = new(new WorldItemId("Potion"), itemDefinition.Id, Charges: null);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack thiefStack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player1.Id,
            new WorldPosition(2, 2),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("thief"), thief), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        WorldStack targetStack = TestHelpers.SimpleStack(
                new WorldStackId("enemy"),
                player2.Id,
                new WorldPosition(3, 2),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("warlord"), warlord), GridPosition.FrontBottom, false)
                    .Build(),
                movementPoints: 10) with
            {
                Inventory = new WorldInventory([potionItem])
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithRandom(new TestSequenceRandomProvider(0, 0))
            .WithDefinition(thief)
            .WithDefinition(warlord)
            .WithDefinition(itemDefinition)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.StealItem, 80))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Guildmaster))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(thiefStack)
            .WithStack(targetStack)
            .Build();

        engine.CheckCommand(
            new WorldThiefActionCommand(
                thiefStack.Id,
                new WorldThiefStealItemAction(new WorldPosition(3, 2), itemDefinition.Id)),
            [
                new WorldThiefActionSucceededEvent(thiefStack.Id, WorldThiefActionType.StealItem, player2.Id),
                new WorldInventoryItemTransferredEvent(
                    new WorldUnitGroup.Stack(targetStack.Id),
                    new WorldUnitGroup.Stack(thiefStack.Id),
                    potionItem),
                new WorldStackMovementConsumedEvent(thiefStack.Id, 10)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks
                    .SetItem(targetStack.Id, targetStack with { Inventory = WorldInventory.Empty })
                    .SetItem(thiefStack.Id, thiefStack with
                    {
                        MovementPoints = 10,
                        Inventory = new WorldInventory([potionItem])
                    })
            }));
    }

    [Test]
    public void MerchantTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 7);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        ItemDefinition itemDefinition = Item("potion");

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldStack thiefStack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player1.Id,
            new WorldPosition(5, 4),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("thief"), thief), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        WorldMerchantSite site = Site([new WorldMerchantItem(itemDefinition.Id, 2)]);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithRandom(new TestSequenceRandomProvider(0, 0))
            .WithDefinition(thief)
            .WithDefinition(itemDefinition)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.StealMerchant, 60))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithStack(thiefStack)
            .WithSite(site)
            .Build();

        engine.CheckCommand(
            new WorldThiefActionCommand(
                thiefStack.Id,
                new WorldThiefStealItemAction(new WorldPosition(4, 4), itemDefinition.Id)),
            [
                new WorldThiefActionSucceededEvent(thiefStack.Id, WorldThiefActionType.StealMerchant, null),
                new WorldMerchantItemStolenEvent(thiefStack.Id, site.Id, new WorldItem(new WorldItemId("Item0001"), itemDefinition.Id, Charges: null)),
                new WorldStackMovementConsumedEvent(thiefStack.Id, 10)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(thiefStack.Id, thiefStack with
                {
                    MovementPoints = 10,
                    Inventory = new WorldInventory([new WorldItem(new WorldItemId("Item0001"), itemDefinition.Id, Charges: null)])
                }),
                Sites = engine.State.Sites.SetItem(site.Id, site with
                {
                    Items = [new WorldMerchantItem(itemDefinition.Id, 1)]
                })
            }));
    }

    [Test]
    public void MerchantLastWareTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 7);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        ItemDefinition itemDefinition = Item("potion");

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldStack thiefStack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player1.Id,
            new WorldPosition(5, 4),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("thief"), thief), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        WorldMerchantSite site = Site([new WorldMerchantItem(itemDefinition.Id, 1)]);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithRandom(new TestSequenceRandomProvider(0, 0))
            .WithDefinition(thief)
            .WithDefinition(itemDefinition)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.StealMerchant, 60))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithStack(thiefStack)
            .WithSite(site)
            .Build();

        engine.CheckCommand(
            new WorldThiefActionCommand(
                thiefStack.Id,
                new WorldThiefStealItemAction(new WorldPosition(4, 4), itemDefinition.Id)),
            [
                new WorldThiefActionSucceededEvent(thiefStack.Id, WorldThiefActionType.StealMerchant, null),
                new WorldMerchantItemStolenEvent(thiefStack.Id, site.Id, new WorldItem(new WorldItemId("Item0001"), itemDefinition.Id, Charges: null)),
                new WorldStackMovementConsumedEvent(thiefStack.Id, 10)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(thiefStack.Id, thiefStack with
                {
                    MovementPoints = 10,
                    Inventory = new WorldInventory([new WorldItem(new WorldItemId("Item0001"), itemDefinition.Id, Charges: null)])
                }),
                Sites = engine.State.Sites.SetItem(site.Id, site with { Items = [] })
            }));
    }

    [Test]
    public void MerchantFailedRollTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 7);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        ItemDefinition itemDefinition = Item("potion");

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldStack thiefStack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player1.Id,
            new WorldPosition(5, 4),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("thief"), thief), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        WorldMerchantSite site = Site([new WorldMerchantItem(itemDefinition.Id, 2)]);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithRandom(new TestSequenceRandomProvider(43, 79))
            .WithDefinition(thief)
            .WithDefinition(itemDefinition)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.StealMerchant, 60))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithStack(thiefStack)
            .WithSite(site)
            .Build();

        WorldState before = engine.State;

        engine.CheckCommand(
            new WorldThiefActionCommand(
                thiefStack.Id,
                new WorldThiefStealItemAction(new WorldPosition(4, 4), itemDefinition.Id)),
            [
                new WorldThiefActionFailedEvent(thiefStack.Id, WorldThiefActionType.StealMerchant, null),
                new WorldStackDestroyedEvent(thiefStack.Id, null, null)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            before with
            {
                Stacks = before.Stacks.Remove(thiefStack.Id),
                ScenarioState = before.ScenarioState with
                {
                    DestroyedStacks = before.ScenarioState.DestroyedStacks.Add(new ScenarioDestroyedStack(thiefStack.Id, DestroyerPlayerId: null, SourceTemplateId: null))
                }
            }));
    }

    [Test]
    public void ItemNotCarriedTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 7);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition warlord = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("warlord", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 10, leadership: 2);
        ItemDefinition itemDefinition = Item("potion");

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack thiefStack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player1.Id,
            new WorldPosition(2, 2),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("thief"), thief), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        WorldStack targetStack = TestHelpers.SimpleStack(
            new WorldStackId("enemy"),
            player2.Id,
            new WorldPosition(3, 2),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("warlord"), warlord), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 10);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(warlord)
            .WithDefinition(itemDefinition)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.StealItem, 80))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Guildmaster))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(thiefStack)
            .WithStack(targetStack)
            .Build();

        engine.CheckCommandError(
            new WorldThiefActionCommand(
                thiefStack.Id,
                new WorldThiefStealItemAction(new WorldPosition(3, 2), itemDefinition.Id)),
            WorldCommandError.ItemNotFound);
    }

    [Test]
    public void MerchantWareNotStockedTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 7);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        ItemDefinition itemDefinition = Item("potion");
        ItemDefinition otherDefinition = Item("other");

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldStack thiefStack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player1.Id,
            new WorldPosition(5, 4),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("thief"), thief), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        WorldMerchantSite site = Site([new WorldMerchantItem(otherDefinition.Id, 1)]);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(itemDefinition)
            .WithDefinition(otherDefinition)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.StealMerchant, 60))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithStack(thiefStack)
            .WithSite(site)
            .Build();

        engine.CheckCommandError(
            new WorldThiefActionCommand(
                thiefStack.Id,
                new WorldThiefStealItemAction(new WorldPosition(4, 4), itemDefinition.Id)),
            WorldCommandError.ItemNotAvailable);
    }

    [Test]
    public void NotAdjacentTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 7);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 40, damage: 5),
            movementPoints: 6, leadership: 6);
        ItemDefinition itemDefinition = Item("potion");
        WorldItem potionItem = new(new WorldItemId("Potion"), itemDefinition.Id, Charges: null);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack thiefStack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player1.Id,
            new WorldPosition(0, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("thief"), thief), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        WorldStack targetStack = TestHelpers.SimpleStack(
                new WorldStackId("enemy"),
                player2.Id,
                new WorldPosition(6, 6),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader), GridPosition.FrontBottom, false)
                    .Build(),
                movementPoints: 10) with
            {
                Inventory = new WorldInventory([potionItem])
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(leader)
            .WithDefinition(itemDefinition)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.StealItem, 80))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Guildmaster))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(thiefStack)
            .WithStack(targetStack)
            .Build();

        engine.CheckCommandError(
            new WorldThiefActionCommand(
                thiefStack.Id,
                new WorldThiefStealItemAction(new WorldPosition(6, 6), itemDefinition.Id)),
            WorldCommandError.ThiefTargetInvalid);
    }

    [Test]
    public void OwnStackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 7);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 40, damage: 5),
            movementPoints: 6, leadership: 6);
        ItemDefinition itemDefinition = Item("potion");
        WorldItem potionItem = new(new WorldItemId("Potion"), itemDefinition.Id, Charges: null);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldStack thiefStack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player1.Id,
            new WorldPosition(2, 2),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("thief"), thief), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        WorldStack allyStack = TestHelpers.SimpleStack(
                new WorldStackId("ally"),
                player1.Id,
                new WorldPosition(3, 2),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader), GridPosition.FrontBottom, false)
                    .Build(),
                movementPoints: 10) with
            {
                Inventory = new WorldInventory([potionItem])
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(leader)
            .WithDefinition(itemDefinition)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.StealItem, 80))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Guildmaster))
            .WithPlayer(player1)
            .WithStack(thiefStack)
            .WithStack(allyStack)
            .Build();

        engine.CheckCommandError(
            new WorldThiefActionCommand(
                thiefStack.Id,
                new WorldThiefStealItemAction(new WorldPosition(3, 2), itemDefinition.Id)),
            WorldCommandError.ThiefTargetInvalid);
    }
}
