namespace Gallean.Engine.Tests;

public sealed class WorldEngineGetMerchantInfoTests
{
    private static UnitModifierDefinition DiscountModifier(string id, int percent)
    {
        return TestHelpers.SimpleModifier(id) with { PurchaseDiscountPercent = percent };
    }

    private static ItemDefinition Item(string id, WorldResourceSet value)
    {
        return TestHelpers.SimpleItemDefinition(new ItemDefinitionId(id), ItemCategory.PotionHeal) with
        {
            Value = value,
            Effect = new HealItemEffect(50)
        };
    }

    private static WorldStack StackWithModifiers(
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
            1,
            [.. modifierIds.Select(UnitModifier.Permanent)],
            Name: null,
            IsTypeLocked: false);

        return TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            ownerId,
            position,
            new UnitGrid<WorldUnit>.Builder()
                .Place(leaderUnit, GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 6);
    }

    [Test]
    public void ItemsTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 7);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 4);
        UnitModifierDefinition discount = DiscountModifier("discount", 10);
        ItemDefinition potionDefinition = Item("potion", new WorldResourceSet(50, 0, 0, 0, 0, 0));
        ItemDefinition crownDefinition = Item("crown", new WorldResourceSet(500, 0, 0, 0, 0, 0));
        ItemDefinition talismanDefinition = Item("talisman", new WorldResourceSet(100, 0, 0, 0, 0, 0)) with
        {
            Category = ItemCategory.Talisman
        };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0)
        };

        WorldItem potionItem = new(new WorldItemId("Potion"), potionDefinition.Id, Charges: null);
        WorldItem talismanItem = new(new WorldItemId("Talisman"), talismanDefinition.Id, Charges: 3);
        WorldItem secondPotionItem = new(new WorldItemId("SecondPotion"), potionDefinition.Id, Charges: null);
        WorldStack stack = StackWithModifiers(new WorldUnitId("leaderUnit"), player.Id, new WorldPosition(4, 4), leaderDefinition, [discount.Id]) with
        {
            Inventory = new WorldInventory([potionItem, talismanItem, secondPotionItem])
        };

        WorldMerchantSite site = new(
            new WorldSiteId("site"),
            new WorldFootprint(new WorldPosition(2, 2), 3, 3),
            Variant: new WorldObjectVariant(0),
            TitleTextId: new ScenarioTextId("Title"),
            GreetingTextId: new ScenarioTextId("Greeting"),
            [new WorldMerchantItem(potionDefinition.Id, 2), new WorldMerchantItem(crownDefinition.Id, 1)],
            VisitorIds: [],
            AiPriority: 3);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(discount)
            .WithDefinition(potionDefinition)
            .WithDefinition(crownDefinition)
            .WithDefinition(talismanDefinition)
            .WithRace(TestHelpers.SimpleRace(WorldRace.Undead, new GlobalTextId("Undead Hordes"), WorldMapTerrain.Undead))
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(stack)
            .WithSite(site)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        WorldGetMerchantInfoCommand.Result result = engine.CheckCommand(
            new WorldGetMerchantInfoCommand(stack.Id, site.Id),
            []);

        Assert.That(result, Is.EqualTo(new WorldGetMerchantInfoCommand.Result(
            [
                new WorldGetMerchantInfoCommand.PurchaseItem(
                    potionDefinition.Id,
                    2,
                    new WorldResourceSet(45, 0, 0, 0, 0, 0),
                    Error: null),
                new WorldGetMerchantInfoCommand.PurchaseItem(
                    crownDefinition.Id,
                    1,
                    new WorldResourceSet(450, 0, 0, 0, 0, 0),
                    WorldCommandError.InsufficientResources)
            ],
            [
                new WorldGetMerchantInfoCommand.SellItem(potionItem, new WorldResourceSet(10, 0, 0, 0, 0, 0)),
                new WorldGetMerchantInfoCommand.SellItem(talismanItem, new WorldResourceSet(60, 0, 0, 0, 0, 0)),
                new WorldGetMerchantInfoCommand.SellItem(secondPotionItem, new WorldResourceSet(10, 0, 0, 0, 0, 0))
            ])));
    }

    [Test]
    public void NotAtSiteTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 7);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 4);
        ItemDefinition potionDefinition = Item("potion", new WorldResourceSet(50, 0, 0, 0, 0, 0));

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0)
        };

        WorldStack stack = StackWithModifiers(new WorldUnitId("leaderUnit"), player.Id, new WorldPosition(6, 6), leaderDefinition, []);

        WorldMerchantSite site = new(
            new WorldSiteId("site"),
            new WorldFootprint(new WorldPosition(2, 2), 3, 3),
            Variant: new WorldObjectVariant(0),
            TitleTextId: new ScenarioTextId("Title"),
            GreetingTextId: new ScenarioTextId("Greeting"),
            [new WorldMerchantItem(potionDefinition.Id, 2)],
            VisitorIds: [],
            AiPriority: 3);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(potionDefinition)
            .WithRace(TestHelpers.SimpleRace(WorldRace.Undead, new GlobalTextId("Undead Hordes"), WorldMapTerrain.Undead))
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(stack)
            .WithSite(site)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(new WorldGetMerchantInfoCommand(stack.Id, site.Id), WorldCommandError.StackNotAtSite);
    }
}
