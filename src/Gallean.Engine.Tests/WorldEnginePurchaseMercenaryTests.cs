namespace Gallean.Engine.Tests;

public sealed class WorldEnginePurchaseMercenaryTests
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

    private static WorldMercenarySite Site(IReadOnlyList<WorldMercenaryUnit> units)
    {
        return new WorldMercenarySite(
            new WorldSiteId("site"),
            new WorldFootprint(new WorldPosition(2, 2), 3, 3),
            Variant: new WorldObjectVariant(0),
            TitleTextId: new ScenarioTextId("Title"),
            GreetingTextId: new ScenarioTextId("Greeting"),
            [.. units],
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
        UnitDefinition mercenaryDefinition = TestHelpers.SimpleUnitDefinition("merc", maxHitpoints: 80, initiative: 40, damage: 15);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0)
        };

        WorldStack stack = Stack(new WorldUnitId("leader2"), player.Id, new WorldPosition(4, 4), leaderDefinition);
        WorldMercenarySite site = Site([new WorldMercenaryUnit(mercenaryDefinition.Id, 1, false)]);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
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
                    Units = new UnitGrid<WorldUnit>.Builder()
                        .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader2"), leaderDefinition), GridPosition.FrontBottom, false)
                        .Place(TestHelpers.SimpleUnit(new WorldUnitId("Unit0001"), mercenaryDefinition), GridPosition.FrontCenter, false)
                        .Build()
                })
            }));
    }

    [Test]
    public void UniqueTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 7);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 4);
        UnitDefinition mercenaryDefinition = TestHelpers.SimpleUnitDefinition("merc", maxHitpoints: 80, initiative: 40, damage: 15);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0)
        };

        WorldStack stack = Stack(new WorldUnitId("leader2"), player.Id, new WorldPosition(4, 4), leaderDefinition);
        WorldMercenarySite site = Site([
            new WorldMercenaryUnit(mercenaryDefinition.Id, 1, true),
            new WorldMercenaryUnit(mercenaryDefinition.Id, 2, false)
        ]);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
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
                    Units = new UnitGrid<WorldUnit>.Builder()
                        .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader2"), leaderDefinition), GridPosition.FrontBottom, false)
                        .Place(TestHelpers.SimpleUnit(new WorldUnitId("Unit0001"), mercenaryDefinition), GridPosition.FrontCenter, false)
                        .Build()
                }),
                Sites = engine.State.Sites.SetItem(site.Id, site with
                {
                    Units = [new WorldMercenaryUnit(mercenaryDefinition.Id, 2, false)]
                })
            }));
    }

    [Test]
    public void LeveledMercenaryTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 7);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 4);
        UnitDefinition mercenaryDefinition = TestHelpers.SimpleUnitDefinition("merc", maxHitpoints: 80, initiative: 40, damage: 15);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0)
        };

        WorldStack stack = Stack(new WorldUnitId("leader2"), player.Id, new WorldPosition(4, 4), leaderDefinition);
        WorldMercenarySite site = Site([new WorldMercenaryUnit(mercenaryDefinition.Id, 3, false)]);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(mercenaryDefinition)
            .WithUnitDynamicUpgrade(new UnitDynamicUpgradeDefinition(
                mercenaryDefinition.Id,
                UpgradeChangeLevel: 10,
                LowLevelUpgrade: UnitDynamicUpgrade.Empty with { MaxHitpoints = 10, ExperienceKilled = 2 },
                HighLevelUpgrade: UnitDynamicUpgrade.Empty with { MaxHitpoints = 5, ExperienceKilled = 1 }))
            .WithRace(TestHelpers.SimpleRace(WorldRace.Undead, new GlobalTextId("Undead Hordes"), WorldMapTerrain.Undead))
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(stack)
            .WithSite(site)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldPurchaseMercenaryCommand(stack.Id, site.Id, mercenaryDefinition.Id, 3, GridPosition.FrontCenter),
            [
                new WorldMercenaryPurchasedEvent(
                    site.Id,
                    stack.Id,
                    TestHelpers.SimpleUnit(new WorldUnitId("Unit0001"), mercenaryDefinition) with { Hitpoints = 100, Level = 3 },
                    GridPosition.FrontCenter,
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
                    Units = new UnitGrid<WorldUnit>.Builder()
                        .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader2"), leaderDefinition), GridPosition.FrontBottom, false)
                        .Place(TestHelpers.SimpleUnit(new WorldUnitId("Unit0001"), mercenaryDefinition) with { Hitpoints = 100, Level = 3 }, GridPosition.FrontCenter, false)
                        .Build()
                })
            }));
    }

    [Test]
    public void LeveledMercenaryCostTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 7);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 4);
        UnitDefinition mercenaryDefinition = TestHelpers.SimpleUnitDefinition("merc", maxHitpoints: 80, initiative: 40, damage: 15);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0)
        };

        WorldStack stack = Stack(new WorldUnitId("leader2"), player.Id, new WorldPosition(4, 4), leaderDefinition);
        WorldMercenarySite site = Site([new WorldMercenaryUnit(mercenaryDefinition.Id, 3, false)]);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(mercenaryDefinition)
            .WithUnitDynamicUpgrade(new UnitDynamicUpgradeDefinition(
                mercenaryDefinition.Id,
                UpgradeChangeLevel: 10,
                LowLevelUpgrade: UnitDynamicUpgrade.Empty with
                {
                    MaxHitpoints = 10,
                    PurchaseCost = new WorldResourceSet(15, 0, 0, 0, 0, 0)
                },
                HighLevelUpgrade: UnitDynamicUpgrade.Empty))
            .WithRace(TestHelpers.SimpleRace(WorldRace.Undead, new GlobalTextId("Undead Hordes"), WorldMapTerrain.Undead))
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(stack)
            .WithSite(site)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldPurchaseMercenaryCommand(stack.Id, site.Id, mercenaryDefinition.Id, 3, GridPosition.FrontCenter),
            [
                new WorldMercenaryPurchasedEvent(
                    site.Id,
                    stack.Id,
                    TestHelpers.SimpleUnit(new WorldUnitId("Unit0001"), mercenaryDefinition) with { Hitpoints = 100, Level = 3 },
                    GridPosition.FrontCenter,
                    new WorldResourceSet(80, 0, 0, 0, 0, 0))
            ]
        );

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Players = engine.State.Players.SetItem(player.Id, player with
                {
                    Resources = new WorldResourceSet(20, 0, 0, 0, 0, 0)
                }),
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Units = new UnitGrid<WorldUnit>.Builder()
                        .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader2"), leaderDefinition), GridPosition.FrontBottom, false)
                        .Place(TestHelpers.SimpleUnit(new WorldUnitId("Unit0001"), mercenaryDefinition) with { Hitpoints = 100, Level = 3 }, GridPosition.FrontCenter, false)
                        .Build()
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
        UnitDefinition mercenaryDefinition = TestHelpers.SimpleUnitDefinition("merc", maxHitpoints: 80, initiative: 40, damage: 15);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0)
        };

        WorldStack stack = Stack(new WorldUnitId("leader"), player.Id, new WorldPosition(4, 4), leaderDefinition);
        WorldMercenarySite site = Site([new WorldMercenaryUnit(mercenaryDefinition.Id, 1, false)]);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(mercenaryDefinition)
            .WithRace(TestHelpers.SimpleRace(WorldRace.Undead, new GlobalTextId("Undead Hordes"), WorldMapTerrain.Undead))
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(stack)
            .WithSite(site)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(
            new WorldPurchaseMercenaryCommand(stack.Id, new WorldSiteId("missing"), mercenaryDefinition.Id, 1, GridPosition.FrontCenter),
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
        UnitDefinition mercenaryDefinition = TestHelpers.SimpleUnitDefinition("merc", maxHitpoints: 80, initiative: 40, damage: 15);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0)
        };

        WorldStack stack = Stack(new WorldUnitId("leader"), player.Id, new WorldPosition(6, 6), leaderDefinition);
        WorldMercenarySite site = Site([new WorldMercenaryUnit(mercenaryDefinition.Id, 1, false)]);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(mercenaryDefinition)
            .WithRace(TestHelpers.SimpleRace(WorldRace.Undead, new GlobalTextId("Undead Hordes"), WorldMapTerrain.Undead))
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(stack)
            .WithSite(site)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(
            new WorldPurchaseMercenaryCommand(stack.Id, site.Id, mercenaryDefinition.Id, 1, GridPosition.FrontCenter),
            WorldCommandError.StackNotAtSite
        );
    }

    [Test]
    public void UnitNotAvailableTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 7);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 4);
        UnitDefinition mercenaryDefinition = TestHelpers.SimpleUnitDefinition("merc", maxHitpoints: 80, initiative: 40, damage: 15);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0)
        };

        WorldStack stack = Stack(new WorldUnitId("leader"), player.Id, new WorldPosition(4, 4), leaderDefinition);
        WorldMercenarySite site = Site([new WorldMercenaryUnit(mercenaryDefinition.Id, 1, false)]);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(mercenaryDefinition)
            .WithRace(TestHelpers.SimpleRace(WorldRace.Undead, new GlobalTextId("Undead Hordes"), WorldMapTerrain.Undead))
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(stack)
            .WithSite(site)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(
            new WorldPurchaseMercenaryCommand(stack.Id, site.Id, mercenaryDefinition.Id, 2, GridPosition.FrontCenter),
            WorldCommandError.UnitNotAvailable
        );
    }

    [Test]
    public void InsufficientResourcesTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 7);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 4);
        UnitDefinition mercenaryDefinition = TestHelpers.SimpleUnitDefinition("merc", maxHitpoints: 80, initiative: 40, damage: 15);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(10, 0, 0, 0, 0, 0)
        };

        WorldStack stack = Stack(new WorldUnitId("leader"), player.Id, new WorldPosition(4, 4), leaderDefinition);
        WorldMercenarySite site = Site([new WorldMercenaryUnit(mercenaryDefinition.Id, 1, false)]);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(mercenaryDefinition)
            .WithRace(TestHelpers.SimpleRace(WorldRace.Undead, new GlobalTextId("Undead Hordes"), WorldMapTerrain.Undead))
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(stack)
            .WithSite(site)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(
            new WorldPurchaseMercenaryCommand(stack.Id, site.Id, mercenaryDefinition.Id, 1, GridPosition.FrontCenter),
            WorldCommandError.InsufficientResources
        );
    }

    [Test]
    public void TargetOccupiedTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 7);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 4);
        UnitDefinition mercenaryDefinition = TestHelpers.SimpleUnitDefinition("merc", maxHitpoints: 80, initiative: 40, damage: 15);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0)
        };

        WorldStack stack = Stack(new WorldUnitId("leader"), player.Id, new WorldPosition(4, 4), leaderDefinition);
        WorldMercenarySite site = Site([new WorldMercenaryUnit(mercenaryDefinition.Id, 1, false)]);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(mercenaryDefinition)
            .WithRace(TestHelpers.SimpleRace(WorldRace.Undead, new GlobalTextId("Undead Hordes"), WorldMapTerrain.Undead))
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(stack)
            .WithSite(site)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(
            new WorldPurchaseMercenaryCommand(stack.Id, site.Id, mercenaryDefinition.Id, 1, GridPosition.FrontBottom),
            WorldCommandError.TargetOccupied
        );
    }

    [Test]
    public void LeadershipExceededTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 7);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 1);
        UnitDefinition mercenaryDefinition = TestHelpers.SimpleUnitDefinition("merc", maxHitpoints: 80, initiative: 40, damage: 15);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0)
        };

        WorldStack stack = Stack(new WorldUnitId("leader"), player.Id, new WorldPosition(4, 4), leaderDefinition);
        WorldMercenarySite site = Site([new WorldMercenaryUnit(mercenaryDefinition.Id, 1, false)]);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(mercenaryDefinition)
            .WithRace(TestHelpers.SimpleRace(WorldRace.Undead, new GlobalTextId("Undead Hordes"), WorldMapTerrain.Undead))
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(stack)
            .WithSite(site)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(
            new WorldPurchaseMercenaryCommand(stack.Id, site.Id, mercenaryDefinition.Id, 1, GridPosition.FrontCenter),
            WorldCommandError.LeadershipExceeded
        );
    }
}
