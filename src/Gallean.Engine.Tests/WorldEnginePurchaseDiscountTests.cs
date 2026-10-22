using Gallean.Tests.Common;

namespace Gallean.Engine.Tests;

public sealed class WorldEnginePurchaseDiscountTests
{
    private static UnitModifierDefinition DiscountModifier(string id, int percent)
    {
        return TestHelpers.SimpleModifier(id) with { PurchaseDiscountPercent = percent };
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

    private static ItemDefinition Item(string id, WorldResourceSet value)
    {
        return TestHelpers.SimpleItemDefinition(new ItemDefinitionId(id), ItemCategory.PotionHeal) with
        {
            Value = value,
            Effect = new HealItemEffect(50)
        };
    }

    private static WorldSpellDefinition Spell(string id, WorldResourceSet purchaseCost)
    {
        return TestHelpers.SimpleSpellDefinition(new WorldSpellDefinitionId(id), new WorldFogSpellEffect()) with
        {
            CastingCost = new WorldResourceSet(10, 0, 0, 0, 0, 0),
            PurchaseCost = purchaseCost
        };
    }

    [Test]
    public void MerchantItemTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 7);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 4);
        UnitModifierDefinition discount = DiscountModifier("discount", 10);
        ItemDefinition itemDefinition = Item("potion", new WorldResourceSet(50, 0, 0, 0, 0, 0));

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0)
        };

        WorldStack stack = StackWithModifiers(new WorldUnitId("leaderUnit"), player.Id, new WorldPosition(4, 4), leaderDefinition, [discount.Id]);

        WorldMerchantSite site = new(
            new WorldSiteId("site"),
            new WorldFootprint(new WorldPosition(2, 2), 3, 3),
            Variant: new WorldObjectVariant(0),
            TitleTextId: new ScenarioTextId("Title"),
            GreetingTextId: new ScenarioTextId("Greeting"),
            [new WorldMerchantItem(itemDefinition.Id, 2)],
            VisitorIds: [],
            AiPriority: 3);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(discount)
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
                    new WorldResourceSet(45, 0, 0, 0, 0, 0))
            ]
        );

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Players = engine.State.Players.SetItem(player.Id, player with
                {
                    Resources = new WorldResourceSet(55, 0, 0, 0, 0, 0)
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
    public void MageSpellTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 7);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 4);
        UnitModifierDefinition discount = DiscountModifier("discount", 10);
        WorldSpellDefinition spell = Spell("spell", new WorldResourceSet(200, 0, 0, 0, 0, 0));

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(300, 0, 0, 0, 0, 0)
        };

        WorldStack stack = StackWithModifiers(new WorldUnitId("leaderUnit"), player.Id, new WorldPosition(4, 4), leaderDefinition, [discount.Id]);

        WorldMageSite site = new(
            new WorldSiteId("site"),
            new WorldFootprint(new WorldPosition(2, 2), 3, 3),
            Variant: new WorldObjectVariant(0),
            TitleTextId: new ScenarioTextId("Title"),
            GreetingTextId: new ScenarioTextId("Greeting"),
            [spell.Id],
            VisitorIds: [],
            AiPriority: 3);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(discount)
            .WithDefinition(spell)
            .WithRace(TestHelpers.SimpleRace(WorldRace.Undead, new GlobalTextId("Undead Hordes"), WorldMapTerrain.Undead))
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(stack)
            .WithSite(site)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldPurchaseSpellCommand(stack.Id, site.Id, spell.Id),
            [
                new WorldSpellPurchasedEvent(
                    site.Id,
                    stack.Id,
                    player.Id,
                    spell.Id,
                    new WorldResourceSet(180, 0, 0, 0, 0, 0))
            ]
        );

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Players = engine.State.Players.SetItem(player.Id, player with
                {
                    Resources = new WorldResourceSet(120, 0, 0, 0, 0, 0),
                    Spellbook = player.Spellbook with { Acquired = [spell.Id] }
                })
            }));
    }

    [Test]
    public void MercenaryTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 7);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 4);
        UnitModifierDefinition discount = DiscountModifier("discount", 10);
        UnitDefinition mercenaryDefinition = TestHelpers.SimpleUnitDefinition("merc", maxHitpoints: 80, initiative: 40, damage: 15);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0)
        };

        WorldStack stack = StackWithModifiers(new WorldUnitId("leaderUnit"), player.Id, new WorldPosition(4, 4), leaderDefinition, [discount.Id]);

        WorldMercenarySite site = new(
            new WorldSiteId("site"),
            new WorldFootprint(new WorldPosition(2, 2), 3, 3),
            Variant: new WorldObjectVariant(0),
            TitleTextId: new ScenarioTextId("Title"),
            GreetingTextId: new ScenarioTextId("Greeting"),
            [new WorldMercenaryUnit(mercenaryDefinition.Id, 1, false)],
            VisitorIds: [],
            AiPriority: 3);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(discount)
            .WithDefinition(mercenaryDefinition)
            .WithRace(TestHelpers.SimpleRace(WorldRace.Undead, new GlobalTextId("Undead Hordes"), WorldMapTerrain.Undead))
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(stack)
            .WithSite(site)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldPurchaseMercenaryCommand(stack.Id, site.Id, mercenaryDefinition.Id, 1, GridPosition.FrontCenter),
            [
                new WorldMercenaryPurchasedEvent(
                    site.Id,
                    stack.Id,
                    TestHelpers.SimpleUnit(new WorldUnitId("Unit0001"), mercenaryDefinition),
                    GridPosition.FrontCenter,
                    new WorldResourceSet(45, 0, 0, 0, 0, 0))
            ]
        );

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Players = engine.State.Players.SetItem(player.Id, player with
                {
                    Resources = new WorldResourceSet(55, 0, 0, 0, 0, 0)
                }),
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Units = new UnitGrid<WorldUnit>.Builder()
                        .Place(Assert.NotNull(stack.Units[GridPosition.FrontBottom]), GridPosition.FrontBottom, false)
                        .Place(TestHelpers.SimpleUnit(new WorldUnitId("Unit0001"), mercenaryDefinition), GridPosition.FrontCenter, false)
                        .Build()
                })
            }));
    }

    [Test]
    public void EquippedArtifactTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 7);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 6, leadership: 4) with
            {
                LeaderAbilities = LeaderAbility.WeaponArmorUse
            };

        UnitModifierDefinition discount = DiscountModifier("discount", 10);

        ItemDefinition luteDefinition = TestHelpers.SimpleItemDefinition(new ItemDefinitionId("lute"), ItemCategory.Weapon) with
        {
            Value = new WorldResourceSet(1500, 0, 0, 0, 0, 0),
            EquipmentModifierId = discount.Id
        };

        ItemDefinition itemDefinition = Item("potion", new WorldResourceSet(50, 0, 0, 0, 0, 0));

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0)
        };

        WorldItem luteItem = new(new WorldItemId("Lute"), luteDefinition.Id, Charges: null);
        WorldStack stack = StackWithModifiers(new WorldUnitId("leaderUnit"), player.Id, new WorldPosition(4, 4), leaderDefinition, []) with
        {
            Inventory = new WorldInventory([luteItem])
        };

        WorldMerchantSite site = new(
            new WorldSiteId("site"),
            new WorldFootprint(new WorldPosition(2, 2), 3, 3),
            Variant: new WorldObjectVariant(0),
            TitleTextId: new ScenarioTextId("Title"),
            GreetingTextId: new ScenarioTextId("Greeting"),
            [new WorldMerchantItem(itemDefinition.Id, 2)],
            VisitorIds: [],
            AiPriority: 3);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(discount)
            .WithDefinition(luteDefinition)
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
            ],
            dryRun: true);

        _ = engine.DispatchCommand(
            new WorldEquipItemCommand(stack.Id, luteItem.Id, LeaderEquipmentSlot.Artifact1));

        engine.CheckCommand(
            new WorldPurchaseItemCommand(stack.Id, site.Id, itemDefinition.Id),
            [
                new WorldItemPurchasedEvent(
                    site.Id,
                    stack.Id,
                    new WorldItem(new WorldItemId("Item0001"), itemDefinition.Id, Charges: null),
                    new WorldResourceSet(45, 0, 0, 0, 0, 0))
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Players = engine.State.Players.SetItem(player.Id, player with
                {
                    Resources = new WorldResourceSet(55, 0, 0, 0, 0, 0)
                }),
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Inventory = new WorldInventory([new WorldItem(new WorldItemId("Item0001"), itemDefinition.Id, Charges: null)]),
                    Equipment = LeaderEquipment.Empty.With(LeaderEquipmentSlot.Artifact1, luteItem),
                    Units = new UnitGrid<WorldUnit>.Builder()
                        .Place(TestHelpers.SimpleUnit(new WorldUnitId("leaderUnit"), leaderDefinition) with { Modifiers = [UnitModifier.FromEquipment(discount.Id)] }, GridPosition.FrontBottom, false)
                        .Build()
                }),
                Sites = engine.State.Sites.SetItem(site.Id, site with
                {
                    Items = [new WorldMerchantItem(itemDefinition.Id, 1)]
                })
            }));
    }

    [Test]
    public void StackedModifiersTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 7);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 4);
        UnitModifierDefinition first = DiscountModifier("first", 10);
        UnitModifierDefinition second = DiscountModifier("second", 10);
        ItemDefinition itemDefinition = Item("potion", new WorldResourceSet(50, 0, 0, 0, 0, 0));

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0)
        };

        WorldStack stack = StackWithModifiers(new WorldUnitId("leaderUnit"), player.Id, new WorldPosition(4, 4), leaderDefinition, [first.Id, second.Id]);

        WorldMerchantSite site = new(
            new WorldSiteId("site"),
            new WorldFootprint(new WorldPosition(2, 2), 3, 3),
            Variant: new WorldObjectVariant(0),
            TitleTextId: new ScenarioTextId("Title"),
            GreetingTextId: new ScenarioTextId("Greeting"),
            [new WorldMerchantItem(itemDefinition.Id, 2)],
            VisitorIds: [],
            AiPriority: 3);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(first)
            .WithDefinition(second)
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
                    new WorldResourceSet(40, 0, 0, 0, 0, 0))
            ]
        );

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Players = engine.State.Players.SetItem(player.Id, player with
                {
                    Resources = new WorldResourceSet(60, 0, 0, 0, 0, 0)
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
    public void RoundingTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 7);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 4);
        UnitModifierDefinition discount = DiscountModifier("discount", 10);
        ItemDefinition itemDefinition = Item("potion", new WorldResourceSet(155, 0, 0, 0, 0, 0));

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(200, 0, 0, 0, 0, 0)
        };

        WorldStack stack = StackWithModifiers(new WorldUnitId("leaderUnit"), player.Id, new WorldPosition(4, 4), leaderDefinition, [discount.Id]);

        WorldMerchantSite site = new(
            new WorldSiteId("site"),
            new WorldFootprint(new WorldPosition(2, 2), 3, 3),
            Variant: new WorldObjectVariant(0),
            TitleTextId: new ScenarioTextId("Title"),
            GreetingTextId: new ScenarioTextId("Greeting"),
            [new WorldMerchantItem(itemDefinition.Id, 2)],
            VisitorIds: [],
            AiPriority: 3);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(discount)
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
                    new WorldResourceSet(139, 0, 0, 0, 0, 0))
            ]
        );

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Players = engine.State.Players.SetItem(player.Id, player with
                {
                    Resources = new WorldResourceSet(61, 0, 0, 0, 0, 0)
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
    public void DiscountCapTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 7);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 4);
        UnitModifierDefinition discount = DiscountModifier("discount", 150);
        ItemDefinition itemDefinition = Item("potion", new WorldResourceSet(50, 0, 0, 0, 0, 0));

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(0, 0, 0, 0, 0, 0)
        };

        WorldStack stack = StackWithModifiers(new WorldUnitId("leaderUnit"), player.Id, new WorldPosition(4, 4), leaderDefinition, [discount.Id]);

        WorldMerchantSite site = new(
            new WorldSiteId("site"),
            new WorldFootprint(new WorldPosition(2, 2), 3, 3),
            Variant: new WorldObjectVariant(0),
            TitleTextId: new ScenarioTextId("Title"),
            GreetingTextId: new ScenarioTextId("Greeting"),
            [new WorldMerchantItem(itemDefinition.Id, 2)],
            VisitorIds: [],
            AiPriority: 3);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(discount)
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
                    WorldResourceSet.Empty)
            ]
        );

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Players = engine.State.Players.SetItem(player.Id, player with
                {
                    Resources = WorldResourceSet.Empty
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
    public void AffordableOnlyWithDiscountTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 7);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 4);
        UnitModifierDefinition discount = DiscountModifier("discount", 10);
        ItemDefinition itemDefinition = Item("potion", new WorldResourceSet(50, 0, 0, 0, 0, 0));

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(45, 0, 0, 0, 0, 0)
        };

        WorldStack discounted = StackWithModifiers(new WorldUnitId("leaderUnit"), player.Id, new WorldPosition(4, 4), leaderDefinition, [discount.Id]);
        WorldStack plain = StackWithModifiers(new WorldUnitId("leaderUnit2"), player.Id, new WorldPosition(4, 4), leaderDefinition, []);

        WorldMerchantSite site = new(
            new WorldSiteId("site"),
            new WorldFootprint(new WorldPosition(2, 2), 3, 3),
            Variant: new WorldObjectVariant(0),
            TitleTextId: new ScenarioTextId("Title"),
            GreetingTextId: new ScenarioTextId("Greeting"),
            [new WorldMerchantItem(itemDefinition.Id, 2)],
            VisitorIds: [],
            AiPriority: 3);

        WorldEngine plainEngine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(discount)
            .WithDefinition(itemDefinition)
            .WithRace(TestHelpers.SimpleRace(WorldRace.Undead, new GlobalTextId("Undead Hordes"), WorldMapTerrain.Undead))
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(plain)
            .WithSite(site)
            .Build();

        _ = plainEngine.DispatchCommand(new WorldStartGameCommand());

        plainEngine.CheckCommandError(
            new WorldPurchaseItemCommand(plain.Id, site.Id, itemDefinition.Id),
            WorldCommandError.InsufficientResources
        );

        WorldEngine discountedEngine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(discount)
            .WithDefinition(itemDefinition)
            .WithRace(TestHelpers.SimpleRace(WorldRace.Undead, new GlobalTextId("Undead Hordes"), WorldMapTerrain.Undead))
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(discounted)
            .WithSite(site)
            .Build();

        _ = discountedEngine.DispatchCommand(new WorldStartGameCommand());

        discountedEngine.CheckCommand(
            new WorldPurchaseItemCommand(discounted.Id, site.Id, itemDefinition.Id),
            [
                new WorldItemPurchasedEvent(
                    site.Id,
                    discounted.Id,
                    new WorldItem(new WorldItemId("Item0001"), itemDefinition.Id, Charges: null),
                    new WorldResourceSet(45, 0, 0, 0, 0, 0))
            ]
        );

        Assert.That(discountedEngine.State, Is.EqualTo(
            discountedEngine.State with
            {
                Players = discountedEngine.State.Players.SetItem(player.Id, player with
                {
                    Resources = WorldResourceSet.Empty
                }),
                Stacks = discountedEngine.State.Stacks.SetItem(discounted.Id, discounted with
                {
                    Inventory = new WorldInventory([new WorldItem(new WorldItemId("Item0001"), itemDefinition.Id, Charges: null)])
                }),
                Sites = discountedEngine.State.Sites.SetItem(site.Id, site with
                {
                    Items = [new WorldMerchantItem(itemDefinition.Id, 1)]
                })
            }));
    }

    [Test]
    public void TrainerTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 7);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 4);
        UnitModifierDefinition discount = DiscountModifier("discount", 10);

        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 10) with
        {
            TrainingCost = new WorldResourceSet(5, 0, 0, 0, 0, 0)
        };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(1000, 0, 0, 0, 0, 0)
        };

        WorldUnit leaderUnit = new(
            new WorldUnitId("leaderUnit"),
            leaderDefinition.Id,
            leaderDefinition.MaxHitpoints,
            0,
            1,
            [UnitModifier.Permanent(discount.Id)],
            Name: null,
            IsTypeLocked: false);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            player.Id,
            new WorldPosition(4, 4),
            new UnitGrid<WorldUnit>.Builder()
                .Place(leaderUnit, GridPosition.BackBottom, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition) with { Experience = 900 }, GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 6);

        WorldTrainerSite site = new(
            Id: new WorldSiteId("trainer"),
            Footprint: new WorldFootprint(new WorldPosition(2, 2), 3, 3),
            Variant: new WorldObjectVariant(0),
            TitleTextId: new ScenarioTextId("Title"),
            GreetingTextId: new ScenarioTextId("Greeting"),
            VisitorIds: [],
            AiPriority: 3);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(discount)
            .WithDefinition(soldierDefinition)
            .WithRace(TestHelpers.SimpleRace(WorldRace.Undead, new GlobalTextId("Undead Hordes"), WorldMapTerrain.Undead))
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(stack)
            .WithSite(site)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldTrainUnitCommand(stack.Id, new WorldUnitId("soldier")),
            [
                new WorldStackUnitTrainedEvent(stack.Id, new WorldUnitId("soldier"), 999, new WorldResourceSet(495, 0, 0, 0, 0, 0))
            ]
        );

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Players = engine.State.Players.SetItem(player.Id, player with
                {
                    Resources = new WorldResourceSet(505, 0, 0, 0, 0, 0)
                }),
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Units = new UnitGrid<WorldUnit>.Builder()
                        .Place(leaderUnit, GridPosition.BackBottom, false)
                        .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition) with { Experience = 999 }, GridPosition.FrontBottom, false)
                        .Build()
                })
            }));
    }
}
