namespace Gallean.Engine.Tests;

public sealed class WorldEngineGetMercenaryInfoTests
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

    [Test]
    public void UnitsTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 7);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 4);
        UnitModifierDefinition discount = DiscountModifier("discount", 10);
        UnitDefinition scoutDefinition = TestHelpers.SimpleUnitDefinition("scout", maxHitpoints: 80, initiative: 40, damage: 15);
        UnitDefinition knightDefinition = TestHelpers.SimpleUnitDefinition("knight", maxHitpoints: 200, initiative: 40, damage: 40) with
        {
            PurchaseCost = new WorldResourceSet(500, 0, 0, 0, 0, 0)
        };

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
            [new WorldMercenaryUnit(scoutDefinition.Id, 1, false), new WorldMercenaryUnit(knightDefinition.Id, 1, false)],
            VisitorIds: [],
            AiPriority: 3);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(discount)
            .WithDefinition(scoutDefinition)
            .WithDefinition(knightDefinition)
            .WithRace(TestHelpers.SimpleRace(WorldRace.Undead, new GlobalTextId("Undead Hordes"), WorldMapTerrain.Undead))
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(stack)
            .WithSite(site)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        WorldGetMercenaryInfoCommand.Result result = engine.CheckCommand(
            new WorldGetMercenaryInfoCommand(stack.Id, site.Id),
            []);

        Assert.That(result, Is.EqualTo(new WorldGetMercenaryInfoCommand.Result(
        [
            new WorldGetMercenaryInfoCommand.Mercenary(
                scoutDefinition.Id,
                1,
                new WorldResourceSet(45, 0, 0, 0, 0, 0),
                Error: null),
            new WorldGetMercenaryInfoCommand.Mercenary(
                knightDefinition.Id,
                1,
                new WorldResourceSet(450, 0, 0, 0, 0, 0),
                WorldCommandError.InsufficientResources)
        ])));
    }

    [Test]
    public void FullPartyTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 7);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 1);
        UnitDefinition scoutDefinition = TestHelpers.SimpleUnitDefinition("scout", maxHitpoints: 80, initiative: 40, damage: 15);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0)
        };

        WorldStack stack = StackWithModifiers(new WorldUnitId("leaderUnit"), player.Id, new WorldPosition(4, 4), leaderDefinition, []);

        WorldMercenarySite site = new(
            new WorldSiteId("site"),
            new WorldFootprint(new WorldPosition(2, 2), 3, 3),
            Variant: new WorldObjectVariant(0),
            TitleTextId: new ScenarioTextId("Title"),
            GreetingTextId: new ScenarioTextId("Greeting"),
            [new WorldMercenaryUnit(scoutDefinition.Id, 1, false)],
            VisitorIds: [],
            AiPriority: 3);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(scoutDefinition)
            .WithRace(TestHelpers.SimpleRace(WorldRace.Undead, new GlobalTextId("Undead Hordes"), WorldMapTerrain.Undead))
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(stack)
            .WithSite(site)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        WorldGetMercenaryInfoCommand.Result result = engine.CheckCommand(
            new WorldGetMercenaryInfoCommand(stack.Id, site.Id),
            []);

        Assert.That(result, Is.EqualTo(new WorldGetMercenaryInfoCommand.Result(
        [
            new WorldGetMercenaryInfoCommand.Mercenary(
                scoutDefinition.Id,
                1,
                new WorldResourceSet(50, 0, 0, 0, 0, 0),
                WorldCommandError.LeadershipExceeded)
        ])));
    }

    [Test]
    public void NoFreeSlotTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 7);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        UnitDefinition scoutDefinition = TestHelpers.SimpleUnitDefinition("scout", maxHitpoints: 80, initiative: 40, damage: 15);
        UnitDefinition giantDefinition = TestHelpers.SimpleUnitDefinition("giant", maxHitpoints: 300, initiative: 30, damage: 50) with
        {
            IsLarge = true
        };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0)
        };

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            player.Id,
            new WorldPosition(4, 4),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("scout"), scoutDefinition), GridPosition.BackCenter, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("scout2"), scoutDefinition), GridPosition.BackTop, false)
                .Build(),
            movementPoints: 6);

        WorldMercenarySite site = new(
            new WorldSiteId("site"),
            new WorldFootprint(new WorldPosition(2, 2), 3, 3),
            Variant: new WorldObjectVariant(0),
            TitleTextId: new ScenarioTextId("Title"),
            GreetingTextId: new ScenarioTextId("Greeting"),
            [new WorldMercenaryUnit(scoutDefinition.Id, 1, false), new WorldMercenaryUnit(giantDefinition.Id, 1, false)],
            VisitorIds: [],
            AiPriority: 3);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(scoutDefinition)
            .WithDefinition(giantDefinition)
            .WithRace(TestHelpers.SimpleRace(WorldRace.Undead, new GlobalTextId("Undead Hordes"), WorldMapTerrain.Undead))
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(stack)
            .WithSite(site)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        WorldGetMercenaryInfoCommand.Result result = engine.CheckCommand(
            new WorldGetMercenaryInfoCommand(stack.Id, site.Id),
            []);

        Assert.That(result, Is.EqualTo(new WorldGetMercenaryInfoCommand.Result(
        [
            new WorldGetMercenaryInfoCommand.Mercenary(
                scoutDefinition.Id,
                1,
                new WorldResourceSet(50, 0, 0, 0, 0, 0),
                Error: null),
            new WorldGetMercenaryInfoCommand.Mercenary(
                giantDefinition.Id,
                1,
                new WorldResourceSet(50, 0, 0, 0, 0, 0),
                WorldCommandError.TargetOccupied)
        ])));
    }
}
