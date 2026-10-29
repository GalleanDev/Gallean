using Gallean.Common;
using Gallean.Engine;
using Gallean.Engine.Tests;
using Gallean.Tests.Common;

namespace Gallean.Client.Tests;

public sealed class MercenaryTabViewModelTests
{
    private static WorldStack Stack(WorldUnitId leaderId, WorldPlayerId ownerId, WorldPosition position, UnitDefinition leader)
    {
        return TestHelpers.SimpleStack(
            new WorldStackId("Virgile"),
            ownerId,
            position,
            new UnitGrid<WorldUnit>.Builder()
                .Place(new WorldUnit(leaderId, leader.Id, leader.MaxHitpoints, 0, leader.Level, [], null, false), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);
    }

    private static WorldMercenarySite Site(IReadOnlyList<WorldMercenaryUnit> units)
    {
        return new WorldMercenarySite(
            new WorldSiteId("camp"),
            new WorldFootprint(new WorldPosition(2, 2), 3, 3),
            Variant: new WorldObjectVariant(0),
            TitleTextId: new ScenarioTextId("Turion's Camp"),
            GreetingTextId: new ScenarioTextId("My lizards are the finest in the region."),
            [.. units],
            VisitorIds: [],
            AiPriority: 3);
    }

    private static UnitGridSlotViewModel FindSlot(UnitGridViewModel grid, GridPosition position)
    {
        foreach (UnitGridSlotViewModel slot in grid.Slots)
        {
            if (slot.Position == position)
            {
                return slot;
            }
        }

        throw new InvalidOperationException($"Slot ({position.Line}, {position.Flank}) was not found.");
    }

    private static IReadOnlyList<string> FormatMercenaries(IReadOnlyList<MercenaryItemViewModel> mercenaries)
    {
        List<string> rows = [];

        foreach (MercenaryItemViewModel mercenary in mercenaries)
        {
            rows.Add($"{mercenary.UnitDefinitionId.Value} | {mercenary.Name} | {mercenary.Description} | {mercenary.LevelText} | Cost: {FormatPrice(Assert.NotNull(mercenary.Status.Price))} | {FormatAvailability(mercenary.Status)} | {FormatSelection(mercenary.IsSelected)}");
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

    private static string FormatPrice(OfferPriceViewModel price)
    {
        List<string> items = [];

        foreach (OfferPriceItemViewModel item in price.Items)
        {
            if (item.IsUnaffordable)
            {
                items.Add($"{item.Resource.Value} {item.Resource.Name.ToLowerInvariant()} (unaffordable)");
            }
            else
            {
                items.Add($"{item.Resource.Value} {item.Resource.Name.ToLowerInvariant()}");
            }
        }

        return string.Join(", ", items);
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

    [Test]
    public async Task HireTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0)
        };

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 4);
        UnitDefinition mercenaryDefinition = TestHelpers.SimpleUnitDefinition("merc", maxHitpoints: 80, initiative: 40, damage: 15);

        WorldStack stack = Stack(new WorldUnitId("leader2"), player.Id, new WorldPosition(4, 4), leaderDefinition);
        WorldMercenarySite site = Site([new WorldMercenaryUnit(mercenaryDefinition.Id, 1, false)]);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithRace(TestHelpers.SimpleRace(WorldRace.Human, new GlobalTextId("Empire"), WorldMapTerrain.Human))
            .WithDefinition(leaderDefinition)
            .WithDefinition(mercenaryDefinition)
            .WithStack(stack)
            .WithSite(site)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel management = new(gameModel, gameViewModel, ScreenMusic.Keep, null, owner => [new MercenaryTabViewModel(gameModel, gameViewModel, owner, site.Id, stack.Id)]);
        MercenaryTabViewModel screen = Assert.IsOfType<MercenaryTabViewModel>(management.Tabs[0]);

        Assert.That(FormatMercenaries(screen.Mercenaries), Is.EqualTo([
            "merc | name | description | Level 1 | Cost: 50 gold | available | not selected"
        ]));
        Assert.That(screen.Stack.UnitGrid.Slots.Select(slot => slot.IsHighlighted), Is.EqualTo([false, false, false, false, false, false]));

        screen.DragDrop.BeginDrag(screen.Mercenaries[0].Reference);

        Assert.That(screen.Stack.UnitGrid.Slots.Select(slot => slot.IsHighlighted), Is.EqualTo([false, true, true, true, true, true]));

        await screen.DragDrop.DropAsync(FindSlot(screen.Stack.UnitGrid, GridPosition.FrontCenter).Reference);

        Assert.That(session.World.State.Stacks[stack.Id], Is.EqualTo(stack with
        {
            Units = new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader2"), leaderDefinition), GridPosition.FrontBottom, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("Unit0001"), mercenaryDefinition), GridPosition.FrontCenter, false)
                .Build()
        }));
        Assert.That(session.World.State.Players[player.Id].Resources, Is.EqualTo(new WorldResourceSet(50, 0, 0, 0, 0, 0)));
        Assert.That(management.PlayerResources, Is.EqualTo(new ResourceSetViewModel(new WorldResourceSet(50, 0, 0, 0, 0, 0), new WorldResourceSet(100, 0, 0, 0, 0, 0), ResourceSetProjection.Gold)));
        Assert.That(FormatMercenaries(screen.Mercenaries), Is.EqualTo([
            "merc | name | description | Level 1 | Cost: 50 gold | available | not selected"
        ]));
        Assert.That(screen.Game.Alert.Current, Is.Null);
    }

    [Test]
    public async Task UniqueMercenaryTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0)
        };

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 4);
        UnitDefinition mercenaryDefinition = TestHelpers.SimpleUnitDefinition("merc", maxHitpoints: 80, initiative: 40, damage: 15);

        WorldStack stack = Stack(new WorldUnitId("leader"), player.Id, new WorldPosition(4, 4), leaderDefinition);
        WorldMercenarySite site = Site([
            new WorldMercenaryUnit(mercenaryDefinition.Id, 1, true),
            new WorldMercenaryUnit(mercenaryDefinition.Id, 3, false)
        ]);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithRace(TestHelpers.SimpleRace(WorldRace.Human, new GlobalTextId("Empire"), WorldMapTerrain.Human))
            .WithDefinition(leaderDefinition)
            .WithDefinition(mercenaryDefinition)
            .WithUnitDynamicUpgrade(new UnitDynamicUpgradeDefinition(
                mercenaryDefinition.Id,
                UpgradeChangeLevel: 10,
                LowLevelUpgrade: UnitDynamicUpgrade.Empty with { MaxHitpoints = 10, ExperienceKilled = 2 },
                HighLevelUpgrade: UnitDynamicUpgrade.Empty with { MaxHitpoints = 5, ExperienceKilled = 1 }))
            .WithStack(stack)
            .WithSite(site)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel management = new(gameModel, gameViewModel, ScreenMusic.Keep, null, owner => [new MercenaryTabViewModel(gameModel, gameViewModel, owner, site.Id, stack.Id)]);
        MercenaryTabViewModel screen = Assert.IsOfType<MercenaryTabViewModel>(management.Tabs[0]);

        Assert.That(FormatMercenaries(screen.Mercenaries), Is.EqualTo([
            "merc | name | description | Level 1 | Cost: 50 gold | available | not selected",
            "merc | name | description | Level 3 | Cost: 50 gold | available | not selected"
        ]));

        screen.DragDrop.BeginDrag(screen.Mercenaries[0].Reference);
        await screen.DragDrop.DropAsync(FindSlot(screen.Stack.UnitGrid, GridPosition.FrontCenter).Reference);

        Assert.That(session.World.State.Sites[site.Id], Is.EqualTo(site with
        {
            Units = [new WorldMercenaryUnit(mercenaryDefinition.Id, 3, false)]
        }));
        Assert.That(FormatMercenaries(screen.Mercenaries), Is.EqualTo([
            "merc | name | description | Level 3 | Cost: 50 gold | available | not selected"
        ]));
        Assert.That(screen.Game.Alert.Current, Is.Null);
    }

    [Test]
    public async Task UnaffordableTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map) with
        {
            Resources = new WorldResourceSet(10, 0, 0, 0, 0, 0)
        };

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 4);
        UnitDefinition mercenaryDefinition = TestHelpers.SimpleUnitDefinition("merc", maxHitpoints: 80, initiative: 40, damage: 15);

        WorldStack stack = Stack(new WorldUnitId("leader"), player.Id, new WorldPosition(4, 4), leaderDefinition);
        WorldMercenarySite site = Site([new WorldMercenaryUnit(mercenaryDefinition.Id, 1, false)]);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithRace(TestHelpers.SimpleRace(WorldRace.Human, new GlobalTextId("Empire"), WorldMapTerrain.Human))
            .WithDefinition(leaderDefinition)
            .WithDefinition(mercenaryDefinition)
            .WithStack(stack)
            .WithSite(site)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel management = new(gameModel, gameViewModel, ScreenMusic.Keep, null, owner => [new MercenaryTabViewModel(gameModel, gameViewModel, owner, site.Id, stack.Id)]);
        MercenaryTabViewModel screen = Assert.IsOfType<MercenaryTabViewModel>(management.Tabs[0]);

        Assert.That(FormatMercenaries(screen.Mercenaries), Is.EqualTo([
            "merc | name | description | Level 1 | Cost: 50 gold (unaffordable) | unavailable | not selected"
        ]));

        screen.DragDrop.BeginDrag(screen.Mercenaries[0].Reference);
        await screen.DragDrop.DropAsync(FindSlot(screen.Stack.UnitGrid, GridPosition.FrontCenter).Reference);

        Assert.That(session.World.State.Stacks[stack.Id], Is.EqualTo(stack));
        Alert alert = Assert.NotNull(screen.Game.Alert.Current);
        Assert.That(alert, Is.EqualTo(new Alert(AlertType.Error, IconMoniker.Warning, "Not enough resources", Details: null)));
    }

    [Test]
    public async Task FullStackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0)
        };

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 1);
        UnitDefinition mercenaryDefinition = TestHelpers.SimpleUnitDefinition("merc", maxHitpoints: 80, initiative: 40, damage: 15);

        WorldStack stack = Stack(new WorldUnitId("leader"), player.Id, new WorldPosition(4, 4), leaderDefinition);
        WorldMercenarySite site = Site([new WorldMercenaryUnit(mercenaryDefinition.Id, 1, false)]);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithRace(TestHelpers.SimpleRace(WorldRace.Human, new GlobalTextId("Empire"), WorldMapTerrain.Human))
            .WithDefinition(leaderDefinition)
            .WithDefinition(mercenaryDefinition)
            .WithStack(stack)
            .WithSite(site)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel management = new(gameModel, gameViewModel, ScreenMusic.Keep, null, owner => [new MercenaryTabViewModel(gameModel, gameViewModel, owner, site.Id, stack.Id)]);
        MercenaryTabViewModel screen = Assert.IsOfType<MercenaryTabViewModel>(management.Tabs[0]);

        Assert.That(FormatMercenaries(screen.Mercenaries), Is.EqualTo([
            "merc | name | description | Level 1 | Cost: 50 gold | unavailable: The party does not have enough leadership | not selected"
        ]));

        screen.Mercenaries[0].SelectCommand.Execute(null);

        Assert.That(FormatOffer(Assert.NotNull(screen.Offer)), Is.EqualTo("name | Level 1 | description | 50 gold | Hire | unavailable: The party does not have enough leadership | disabled"));
        Assert.That(FormatMercenaries(screen.Mercenaries), Is.EqualTo([
            "merc | name | description | Level 1 | Cost: 50 gold | unavailable: The party does not have enough leadership | selected"
        ]));
        Assert.That(screen.Stack.UnitGrid.Slots.Select(slot => slot.IsHighlighted), Is.EqualTo([false, false, false, false, false, false]));

        screen.DragDrop.BeginDrag(screen.Mercenaries[0].Reference);
        await screen.DragDrop.DropAsync(FindSlot(screen.Stack.UnitGrid, GridPosition.FrontCenter).Reference);

        Assert.That(session.World.State.Stacks[stack.Id], Is.EqualTo(stack));
        Alert alert = Assert.NotNull(screen.Game.Alert.Current);
        Assert.That(alert, Is.EqualTo(new Alert(AlertType.Error, IconMoniker.Warning, "The party does not have enough leadership", Details: null)));
    }

    [Test]
    public void DiscountedCostTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0)
        };

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 4);
        UnitModifierDefinition discount = new(
            new UnitModifierDefinitionId("discount"),
            DescriptionTextId: new GlobalTextId("description"),
            MoveAbility: UnitMoveAbility.None,
            MaxMovement: default,
            Leadership: default,
            VisionRadius: default,
            Hitpoints: default,
            RegenerationPercent: default,
            Armor: default,
            Initiative: default,
            Damage: default,
            Accuracy: default,
            DrainPercent: 0,
            FastRetreat: false,
            PurchaseDiscountPercent: 10,
            LeaderAbilities: LeaderAbility.None,
            Wards: []);

        UnitDefinition scoutDefinition = TestHelpers.SimpleUnitDefinition("scout", maxHitpoints: 80, initiative: 40, damage: 15);
        UnitDefinition knightDefinition = TestHelpers.SimpleUnitDefinition("knight", maxHitpoints: 200, initiative: 40, damage: 40) with
        {
            PurchaseCost = new WorldResourceSet(500, 0, 0, 0, 0, 0)
        };

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Virgile"),
            player.Id,
            new WorldPosition(4, 4),
            new UnitGrid<WorldUnit>.Builder()
                .Place(
                    new WorldUnit(new WorldUnitId("leader"), leaderDefinition.Id, leaderDefinition.MaxHitpoints, 0, leaderDefinition.Level, [UnitModifier.Permanent(discount.Id)], null, false),
                    GridPosition.FrontBottom,
                    false)
                .Build(),
            movementPoints: 20);

        WorldMercenarySite site = Site([
            new WorldMercenaryUnit(scoutDefinition.Id, 1, false),
            new WorldMercenaryUnit(knightDefinition.Id, 1, false)
        ]);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithRace(TestHelpers.SimpleRace(WorldRace.Human, new GlobalTextId("Empire"), WorldMapTerrain.Human))
            .WithDefinition(leaderDefinition)
            .WithDefinition(discount)
            .WithDefinition(scoutDefinition)
            .WithDefinition(knightDefinition)
            .WithStack(stack)
            .WithSite(site)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel management = new(gameModel, gameViewModel, ScreenMusic.Keep, null, owner => [new MercenaryTabViewModel(gameModel, gameViewModel, owner, site.Id, stack.Id)]);
        MercenaryTabViewModel screen = Assert.IsOfType<MercenaryTabViewModel>(management.Tabs[0]);

        Assert.That(FormatMercenaries(screen.Mercenaries), Is.EqualTo([
            "scout | name | description | Level 1 | Cost: 45 gold | available | not selected",
            "knight | name | description | Level 1 | Cost: 450 gold (unaffordable) | unavailable | not selected"
        ]));
    }

    [Test]
    public async Task SelectAndHireTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0)
        };

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 4);
        UnitDefinition mercenaryDefinition = TestHelpers.SimpleUnitDefinition("merc", maxHitpoints: 80, initiative: 40, damage: 15);
        UnitDefinition scoutDefinition = TestHelpers.SimpleUnitDefinition("scout", maxHitpoints: 60, initiative: 60, damage: 10);

        WorldStack stack = Stack(new WorldUnitId("leader3"), player.Id, new WorldPosition(4, 4), leaderDefinition);
        WorldMercenarySite site = Site([
            new WorldMercenaryUnit(mercenaryDefinition.Id, 1, false),
            new WorldMercenaryUnit(scoutDefinition.Id, 1, false)
        ]);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithRace(TestHelpers.SimpleRace(WorldRace.Human, new GlobalTextId("Empire"), WorldMapTerrain.Human))
            .WithDefinition(leaderDefinition)
            .WithDefinition(mercenaryDefinition)
            .WithDefinition(scoutDefinition)
            .WithStack(stack)
            .WithSite(site)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel management = new(gameModel, gameViewModel, ScreenMusic.Keep, null, owner => [new MercenaryTabViewModel(gameModel, gameViewModel, owner, site.Id, stack.Id)]);
        MercenaryTabViewModel screen = Assert.IsOfType<MercenaryTabViewModel>(management.Tabs[0]);

        Assert.That(screen.Offer, Is.Null);

        screen.Mercenaries[0].SelectCommand.Execute(null);

        Assert.That(FormatOffer(Assert.NotNull(screen.Offer)), Is.EqualTo("name | Level 1 | description | 50 gold | Hire | available | enabled"));
        Assert.That(FormatMercenaries(screen.Mercenaries), Is.EqualTo([
            "merc | name | description | Level 1 | Cost: 50 gold | available | selected",
            "scout | name | description | Level 1 | Cost: 50 gold | available | not selected"
        ]));
        Assert.That(screen.Stack.UnitGrid.Slots.Select(slot => slot.IsHighlighted), Is.EqualTo([false, false, false, false, false, false]));

        await Assert.NotNull(screen.Offer).ActCommand.ExecuteAsync(null);

        Assert.That(session.World.State.Stacks[stack.Id], Is.EqualTo(stack with
        {
            Units = new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader3"), leaderDefinition), GridPosition.FrontBottom, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("Unit0001"), mercenaryDefinition), GridPosition.FrontCenter, false)
                .Build()
        }));
        Assert.That(session.World.State.Players[player.Id].Resources, Is.EqualTo(new WorldResourceSet(50, 0, 0, 0, 0, 0)));
        Assert.That(FormatOffer(Assert.NotNull(screen.Offer)), Is.EqualTo("name | Level 1 | description | 50 gold | Hire | available | enabled"));

        await FindSlot(screen.Stack.UnitGrid, GridPosition.BackBottom).SelectCommand.ExecuteAsync(null);

        Assert.That(session.World.State.Stacks[stack.Id], Is.EqualTo(stack with
        {
            Units = new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader3"), leaderDefinition), GridPosition.FrontBottom, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("Unit0001"), mercenaryDefinition), GridPosition.FrontCenter, false)
                .Build()
        }));
        Assert.That(session.World.State.Players[player.Id].Resources, Is.EqualTo(new WorldResourceSet(50, 0, 0, 0, 0, 0)));
        Assert.That(FormatOffer(Assert.NotNull(screen.Offer)), Is.EqualTo("name | Level 1 | description | 50 gold | Hire | available | enabled"));
        Assert.That(screen.Game.Alert.Current, Is.Null);
    }

    [Test]
    public void DetailsTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 4);
        UnitDefinition mercenaryDefinition = TestHelpers.SimpleUnitDefinition("merc", maxHitpoints: 80, initiative: 40, damage: 15);
        UnitDynamicUpgradeDefinition mercenaryDynamicUpgrade = new(
            mercenaryDefinition.Id,
            UpgradeChangeLevel: 5,
            UnitDynamicUpgrade.Empty with { MaxHitpoints = 10, Damage = 3, ExperienceNextLevel = 500 },
            UnitDynamicUpgrade.Empty with { MaxHitpoints = 5, Damage = 1, ExperienceNextLevel = 250 });

        WorldStack stack = Stack(new WorldUnitId("leader"), player.Id, new WorldPosition(4, 4), leaderDefinition);
        WorldMercenarySite site = Site([new WorldMercenaryUnit(mercenaryDefinition.Id, 3, false)]);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithRace(TestHelpers.SimpleRace(WorldRace.Human, new GlobalTextId("Empire"), WorldMapTerrain.Human))
            .WithDefinition(leaderDefinition)
            .WithDefinition(mercenaryDefinition)
            .WithUnitDynamicUpgrade(mercenaryDynamicUpgrade)
            .WithStack(stack)
            .WithSite(site)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel management = new(gameModel, gameViewModel, ScreenMusic.Keep, null, owner => [new MercenaryTabViewModel(gameModel, gameViewModel, owner, site.Id, stack.Id)]);
        MercenaryTabViewModel screen = Assert.IsOfType<MercenaryTabViewModel>(management.Tabs[0]);

        Assert.That(screen.Mercenaries[0].Details, Is.EqualTo(new UnitDetailsViewModel(
            new AssetSpriteKey.UnitEventPortrait(ScenarioEventPortraitId.Unit(mercenaryDefinition.Id)),
            "name",
            "description",
            3,
            100,
            100,
            0,
            2000,
            false,
            false,
            false,
            false,
            new UnitAttackViewModel("Damage", new UnitAttackSourceViewModel(IconMoniker.AttackSourceWeapon, "Weapon", Theme.AttackSource.WeaponColor)),
            new StatValueViewModel(TextSpans.FromText("21"), 0),
            null,
            null,
            [
                new StatRowViewModel(IconMoniker.Target, "Targets", new StatValueViewModel(new TextSpans([new TextSpan("1", TextStyle.Bold), new TextSpan(" ", TextStyle.None), new TextSpan("(Ranged)", TextStyle.Italic)]), 0)),
                new StatRowViewModel(IconMoniker.Armor, "Armor", new StatValueViewModel(TextSpans.FromText("0"), 0)),
                new StatRowViewModel(IconMoniker.Accuracy, "Accuracy", new StatValueViewModel(TextSpans.FromText("100%"), 0)),
                new StatRowViewModel(IconMoniker.Initiative, "Initiative", new StatValueViewModel(TextSpans.FromText("40"), 0))
            ],
            null,
            null,
            null,
            [],
            [])));
    }
}
