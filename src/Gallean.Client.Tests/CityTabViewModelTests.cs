using Gallean.Common;
using Gallean.Engine;
using Gallean.Engine.Tests;
using Gallean.Tests.Common;

namespace Gallean.Client.Tests;

public sealed class CityTabViewModelTests
{
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

    private static IReadOnlyList<string> FormatPurchaseOptions(IReadOnlyList<UnitPurchaseItemViewModel> items)
    {
        List<string> rows = [];

        foreach (UnitPurchaseItemViewModel item in items)
        {
            rows.Add($"{item.UnitDefinitionId.Value} | {item.Name} | {item.Description} | Cost: {FormatPrice(Assert.NotNull(item.Status.Price))} | {FormatAvailability(item.Status)}");
        }

        return rows;
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

    private static ItemDefinition HealingPotion(int hitpoints)
    {
        return new ItemDefinition(
            new ItemDefinitionId("potion1"),
            new GlobalTextId("Potion"),
            new GlobalTextId("Potion description"),
            ItemCategory.PotionHeal,
            WorldResourceSet.Empty,
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: new HealItemEffect(hitpoints),
            AnimationUnitId: null);
    }

    [Test]
    public async Task UnitToGarrisonTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 3);
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 35, initiative: 50, damage: 10);

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 100, 0, leaderDefinition.Level, [], null, false);
        WorldUnit soldier = new(new WorldUnitId("soldier"), soldierDefinition.Id, 35, 0, soldierDefinition.Level, [], null, false);
        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .AddUnit(soldierDefinition)
            .Build();

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(
                new WorldCityDefinitionId(WorldRace.Human, 3),
                WorldResourceSet.Empty) with
            {
                VisionRadius = 5
            };

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("Milonia"),
            cityDefinition,
            player.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Virgile"),
            player.Id,
            city.Entrance,
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [
                    (leader, GridPosition.FrontBottom),
                    (soldier, GridPosition.FrontCenter)
                ]),
            movementPoints: 20);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(cityDefinition)
            .WithCity(city)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel management = new(gameModel, gameViewModel, ScreenMusic.Keep, null, owner => [new CityTabViewModel(gameModel, gameViewModel, owner, city.Id)]);
        CityTabViewModel screen = Assert.IsOfType<CityTabViewModel>(management.Tabs[0]);

        UnitGridSlotViewModel source = FindSlot(screen.StackGrid, GridPosition.FrontCenter);
        UnitGridSlotViewModel target = FindSlot(screen.GarrisonGrid, GridPosition.FrontBottom);

        screen.DragDrop.TryBeginDrag(source.Reference);

        Assert.That(screen.StackGrid.Slots.Select(slot => slot.IsHighlighted), Is.EqualTo([true, false, true, true, true, true]));
        Assert.That(screen.GarrisonGrid.Slots.Select(slot => slot.IsHighlighted), Is.EqualTo([true, true, true, true, true, true]));

        await screen.DragDrop.DropAsync(target.Reference);

        WorldStack updatedStack = session.World.State.Stacks[stack.Id];
        WorldCity updatedCity = Assert.IsOfType<WorldCity>(session.World.State.Fortifications[city.Id]);

        Assert.That(updatedStack, Is.EqualTo(stack with
        {
            Units = TestHelpers.StackUnitGrid(stackDefinitions, [(leader, GridPosition.FrontBottom)])
        }));
        Assert.That(updatedCity, Is.EqualTo(city with
        {
            Garrison = new UnitGrid<WorldUnit>.Builder()
                .Place(soldier, GridPosition.FrontBottom, false)
                .Build()
        }));
        Assert.That(screen.Game.Alert.Current, Is.Null);
    }

    [Test]
    public async Task RepositionsUnitTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 3);
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 35, initiative: 50, damage: 10);

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 100, 0, leaderDefinition.Level, [], null, false);
        WorldUnit soldier = new(new WorldUnitId("soldier"), soldierDefinition.Id, 35, 0, soldierDefinition.Level, [], null, false);
        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .AddUnit(soldierDefinition)
            .Build();

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(
                new WorldCityDefinitionId(WorldRace.Human, 3),
                WorldResourceSet.Empty) with
            {
                VisionRadius = 5
            };

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("Milonia"),
            cityDefinition,
            player.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Virgile"),
            player.Id,
            city.Entrance,
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [
                    (leader, GridPosition.FrontBottom),
                    (soldier, GridPosition.FrontCenter)
                ]),
            movementPoints: 20);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(cityDefinition)
            .WithCity(city)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel management = new(gameModel, gameViewModel, ScreenMusic.Keep, null, owner => [new CityTabViewModel(gameModel, gameViewModel, owner, city.Id)]);
        CityTabViewModel screen = Assert.IsOfType<CityTabViewModel>(management.Tabs[0]);

        UnitGridSlotViewModel source = FindSlot(screen.StackGrid, GridPosition.FrontCenter);
        UnitGridSlotViewModel target = FindSlot(screen.StackGrid, GridPosition.BackCenter);

        screen.DragDrop.TryBeginDrag(source.Reference);
        await screen.DragDrop.DropAsync(target.Reference);

        WorldStack updatedStack = session.World.State.Stacks[stack.Id];

        Assert.That(updatedStack, Is.EqualTo(stack with
        {
            Units = TestHelpers.StackUnitGrid(
                stackDefinitions,
                [
                    (leader, GridPosition.FrontBottom),
                    (soldier, GridPosition.BackCenter)
                ])
        }));
        Assert.That(screen.Game.Alert.Current, Is.Null);
    }

    [Test]
    public async Task SwapUnitsTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 3);
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 35, initiative: 50, damage: 10);

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 100, 0, leaderDefinition.Level, [], null, false);
        WorldUnit soldier = new(new WorldUnitId("soldier"), soldierDefinition.Id, 35, 0, soldierDefinition.Level, [], null, false);
        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .AddUnit(soldierDefinition)
            .Build();

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(
                new WorldCityDefinitionId(WorldRace.Human, 3),
                WorldResourceSet.Empty) with
            {
                VisionRadius = 5
            };

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("Milonia"),
            cityDefinition,
            player.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Virgile"),
            player.Id,
            city.Entrance,
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [
                    (leader, GridPosition.FrontBottom),
                    (soldier, GridPosition.FrontCenter)
                ]),
            movementPoints: 20);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(cityDefinition)
            .WithCity(city)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel management = new(gameModel, gameViewModel, ScreenMusic.Keep, null, owner => [new CityTabViewModel(gameModel, gameViewModel, owner, city.Id)]);
        CityTabViewModel screen = Assert.IsOfType<CityTabViewModel>(management.Tabs[0]);

        UnitGridSlotViewModel source = FindSlot(screen.StackGrid, GridPosition.FrontBottom);
        UnitGridSlotViewModel target = FindSlot(screen.StackGrid, GridPosition.FrontCenter);

        screen.DragDrop.TryBeginDrag(source.Reference);
        await screen.DragDrop.DropAsync(target.Reference);

        WorldStack updatedStack = session.World.State.Stacks[stack.Id];

        Assert.That(updatedStack, Is.EqualTo(stack with
        {
            Units = TestHelpers.StackUnitGrid(
                stackDefinitions,
                [
                    (soldier, GridPosition.FrontBottom),
                    (leader, GridPosition.FrontCenter)
                ])
        }));
        Assert.That(screen.Game.Alert.Current, Is.Null);
    }

    [Test]
    public void LargeUnitGridTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 3);
        UnitDefinition largeDefinition = TestHelpers.SimpleUnitDefinition("large", maxHitpoints: 80, initiative: 40, damage: 20) with
        {
            IsLarge = true
        };

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 100, 0, leaderDefinition.Level, [], null, false);
        WorldUnit stackLarge = new(new WorldUnitId("stackLarge"), largeDefinition.Id, 80, 0, largeDefinition.Level, [], null, false);
        WorldUnit garrisonLarge = new(new WorldUnitId("garrisonLarge"), largeDefinition.Id, 60, 0, largeDefinition.Level, [], null, false);
        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .AddUnit(largeDefinition)
            .Build();

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(
                new WorldCityDefinitionId(WorldRace.Human, 3),
                WorldResourceSet.Empty) with
            {
                VisionRadius = 5
            };

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("Milonia"),
            cityDefinition,
            player.Id,
            new WorldPosition(4, 4),
            new UnitGrid<WorldUnit>.Builder()
                .Place(garrisonLarge, GridPosition.FrontCenter, true)
                .Build());

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Virgile"),
            player.Id,
            city.Entrance,
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [
                    (leader, GridPosition.FrontBottom),
                    (stackLarge, GridPosition.FrontCenter)
                ]),
            movementPoints: 20);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(largeDefinition)
            .WithDefinition(cityDefinition)
            .WithCity(city)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel management = new(gameModel, gameViewModel, ScreenMusic.Keep, null, owner => [new CityTabViewModel(gameModel, gameViewModel, owner, city.Id)]);
        CityTabViewModel screen = Assert.IsOfType<CityTabViewModel>(management.Tabs[0]);

        Assert.That(screen.StackGrid.Slots.Select(slot => (slot.Position, slot.Row, slot.Column, slot.ColumnSpan, slot.HasUnit, slot.IsVisible)), Is.EqualTo([
            (GridPosition.FrontBottom, Row: 2, Column: 1, ColumnSpan: 1, HasUnit: true, IsVisible: true),
            (GridPosition.FrontCenter, Row: 1, Column: 0, ColumnSpan: 2, HasUnit: true, IsVisible: true),
            (GridPosition.FrontTop, Row: 0, Column: 1, ColumnSpan: 1, HasUnit: false, IsVisible: true),
            (GridPosition.BackBottom, Row: 2, Column: 0, ColumnSpan: 1, HasUnit: false, IsVisible: true),
            (GridPosition.BackCenter, Row: 1, Column: 0, ColumnSpan: 2, HasUnit: false, IsVisible: false),
            (GridPosition.BackTop, Row: 0, Column: 0, ColumnSpan: 1, HasUnit: false, IsVisible: true)
        ]));

        Assert.That(screen.GarrisonGrid.Slots.Select(slot => (slot.Position, slot.Row, slot.Column, slot.ColumnSpan, slot.HasUnit, slot.IsVisible)), Is.EqualTo([
            (GridPosition.FrontBottom, Row: 2, Column: 0, ColumnSpan: 1, HasUnit: false, IsVisible: true),
            (GridPosition.FrontCenter, Row: 1, Column: 0, ColumnSpan: 2, HasUnit: true, IsVisible: true),
            (GridPosition.FrontTop, Row: 0, Column: 0, ColumnSpan: 1, HasUnit: false, IsVisible: true),
            (GridPosition.BackBottom, Row: 2, Column: 1, ColumnSpan: 1, HasUnit: false, IsVisible: true),
            (GridPosition.BackCenter, Row: 1, Column: 0, ColumnSpan: 2, HasUnit: false, IsVisible: false),
            (GridPosition.BackTop, Row: 0, Column: 1, ColumnSpan: 1, HasUnit: false, IsVisible: true)
        ]));
    }

    [Test]
    public async Task PurchaseUnitAtCapitalTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0)
        };

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 3);
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 35, initiative: 50, damage: 10);

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 100, 0, leaderDefinition.Level, [], null, false);

        WorldRaceDefinition raceDefinition = new(
            Race: WorldRace.Human,
            NameTextId: new GlobalTextId("Human"),
            Income: WorldResourceSet.Empty,
            NativeTerrain: WorldMapTerrain.Human,
            IsPlayable: true,
            CapitalRegeneration: 35,
            Cities: [],
            Leaders: [leaderDefinition.Id],
            Units: [soldierDefinition.Id],
            MaleLeaderNames: [],
            FemaleLeaderNames: []);

        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("cap"),
            player.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Virgile"),
            player.Id,
            capital.Entrance,
            new UnitGrid<WorldUnit>.Builder()
                .Place(leader, GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithRace(raceDefinition)
            .WithCapital(capital)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel management = new(gameModel, gameViewModel, ScreenMusic.Keep, null, owner => [new CityTabViewModel(gameModel, gameViewModel, owner, capital.Id)]);
        CityTabViewModel screen = Assert.IsOfType<CityTabViewModel>(management.Tabs[0]);

        Assert.That(FindSlot(screen.GarrisonGrid, GridPosition.FrontCenter).ShowsPurchaseHint, Is.True);
        Assert.That(FindSlot(screen.StackGrid, GridPosition.FrontCenter).ShowsPurchaseHint, Is.False);

        UnitGridSlotViewModel slot = FindSlot(screen.GarrisonGrid, GridPosition.FrontCenter);
        await slot.SelectCommand.ExecuteAsync(null);

        UnitPurchaseViewModel purchase = Assert.IsOfType<UnitPurchaseViewModel>(gameViewModel.TopScreen);

        Assert.That(
            FormatPurchaseOptions(purchase.Options),
            Is.EqualTo(["soldier | name | description | Cost: 50 gold | available"]));

        WorldCapital capitalBeforePurchase = Assert.IsOfType<WorldCapital>(session.World.State.Fortifications[capital.Id]);
        WorldPlayer playerBeforePurchase = session.World.State.Players[player.Id];

        await purchase.Options[0].PurchaseCommand.ExecuteAsync(null);

        WorldUnit purchasedSoldier = new(new WorldUnitId("Unit0001"), soldierDefinition.Id, 35, 0, 1, [], null, false);

        Assert.That(session.World.State.Fortifications[capital.Id], Is.EqualTo(capitalBeforePurchase with
        {
            Garrison = new UnitGrid<WorldUnit>.Builder()
                .Place(purchasedSoldier, GridPosition.FrontCenter, false)
                .Build()
        }));
        Assert.That(session.World.State.Players[player.Id], Is.EqualTo(playerBeforePurchase with
        {
            Resources = new WorldResourceSet(50, 0, 0, 0, 0, 0)
        }));
        Assert.That(gameViewModel.TopScreen, Is.Null);
    }

    [Test]
    public async Task PurchaseUnavailableReasonsTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0)
        };

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 3);

        WorldPlayerBuildingDefinition temple = new(
            Id: new WorldPlayerBuildingDefinitionId("temple"),
            NameTextId: new GlobalTextId("temple"),
            DescriptionTextId: new GlobalTextId("temple_description"),
            RequiredId: null,
            Category: WorldPlayerBuildingCategory.Unit,
            UnitBranch: UnitBranch.Fighter,
            Level: 1,
            Cost: new WorldResourceSet(100, 0, 0, 0, 0, 0),
            UnitIds: []);

        UnitDefinition expensiveDefinition = TestHelpers.SimpleUnitDefinition("expensive", maxHitpoints: 35, initiative: 50, damage: 10) with
        {
            PurchaseCost = new WorldResourceSet(500, 0, 0, 0, 0, 0)
        };
        UnitDefinition gatedDefinition = TestHelpers.SimpleUnitDefinition("gated", maxHitpoints: 35, initiative: 50, damage: 10) with
        {
            RequiredBuildingId = temple.Id
        };

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 100, 0, leaderDefinition.Level, [], null, false);

        WorldRaceDefinition raceDefinition = new(
            Race: WorldRace.Human,
            NameTextId: new GlobalTextId("Human"),
            Income: WorldResourceSet.Empty,
            NativeTerrain: WorldMapTerrain.Human,
            IsPlayable: true,
            CapitalRegeneration: 35,
            Cities: [],
            Leaders: [leaderDefinition.Id],
            Units: [expensiveDefinition.Id, gatedDefinition.Id],
            MaleLeaderNames: [],
            FemaleLeaderNames: []);

        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("cap"),
            player.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Virgile"),
            player.Id,
            capital.Entrance,
            new UnitGrid<WorldUnit>.Builder()
                .Place(leader, GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(expensiveDefinition)
            .WithDefinition(gatedDefinition)
            .WithDefinition(temple)
            .WithRace(raceDefinition)
            .WithCapital(capital)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel management = new(gameModel, gameViewModel, ScreenMusic.Keep, null, owner => [new CityTabViewModel(gameModel, gameViewModel, owner, capital.Id)]);
        CityTabViewModel screen = Assert.IsOfType<CityTabViewModel>(management.Tabs[0]);

        UnitGridSlotViewModel slot = FindSlot(screen.GarrisonGrid, GridPosition.FrontCenter);
        await slot.SelectCommand.ExecuteAsync(null);

        UnitPurchaseViewModel purchase = Assert.IsOfType<UnitPurchaseViewModel>(gameViewModel.TopScreen);

        Assert.That(
            FormatPurchaseOptions(purchase.Options),
            Is.EqualTo([
                "expensive | name | description | Cost: 500 gold (unaffordable) | unavailable",
                "gated | name | description | Cost: 50 gold | unavailable: Requires temple"
            ]));
    }

    [Test]
    public void CitySceneTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0)
        };

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 3);

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 100, 0, leaderDefinition.Level, [], null, false);

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(
                new WorldCityDefinitionId(WorldRace.Human, 3),
                WorldResourceSet.Empty) with
            {
                VisionRadius = 5
            };

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("Milonia"),
            cityDefinition,
            player.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Virgile"),
            player.Id,
            city.Entrance,
            new UnitGrid<WorldUnit>.Builder()
                .Place(leader, GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(cityDefinition)
            .WithCity(city)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel management = new(gameModel, gameViewModel, ScreenMusic.Keep, null, owner => [new CityTabViewModel(gameModel, gameViewModel, owner, city.Id)]);
        CityTabViewModel screen = Assert.IsOfType<CityTabViewModel>(management.Tabs[0]);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(screen.CityTitle, Is.EqualTo("Name"));
            Assert.That(screen.CitySummary, Is.EqualTo(new TextSpans([new TextSpan("City level: ", TextStyle.None), new TextSpan("3", TextStyle.Highlight)])));
            Assert.That(screen.CityImageKey, Is.EqualTo(new AssetSpriteKey.CityImage(3)));
            Assert.That(screen.CapitalScene, Is.Null);
        }
    }

    [Test]
    public async Task PurchaseUnitAtCityTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0)
        };

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 3);
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 35, initiative: 50, damage: 10);

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 100, 0, leaderDefinition.Level, [], null, false);

        WorldRaceDefinition raceDefinition = new(
            Race: WorldRace.Human,
            NameTextId: new GlobalTextId("Human"),
            Income: WorldResourceSet.Empty,
            NativeTerrain: WorldMapTerrain.Human,
            IsPlayable: true,
            CapitalRegeneration: 35,
            Cities: [],
            Leaders: [leaderDefinition.Id],
            Units: [soldierDefinition.Id],
            MaleLeaderNames: [],
            FemaleLeaderNames: []);

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Human, 3),
            WorldResourceSet.Empty);

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("Milonia"),
            cityDefinition,
            player.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Virgile"),
            player.Id,
            city.Entrance,
            new UnitGrid<WorldUnit>.Builder()
                .Place(leader, GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(cityDefinition)
            .WithRace(raceDefinition)
            .WithCity(city)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel management = new(gameModel, gameViewModel, ScreenMusic.Keep, null, owner => [new CityTabViewModel(gameModel, gameViewModel, owner, city.Id)]);
        CityTabViewModel screen = Assert.IsOfType<CityTabViewModel>(management.Tabs[0]);

        Assert.That(FindSlot(screen.GarrisonGrid, GridPosition.FrontCenter).ShowsPurchaseHint, Is.True);

        UnitGridSlotViewModel slot = FindSlot(screen.GarrisonGrid, GridPosition.FrontCenter);
        await slot.SelectCommand.ExecuteAsync(null);

        UnitPurchaseViewModel purchase = Assert.IsOfType<UnitPurchaseViewModel>(gameViewModel.TopScreen);

        Assert.That(
            FormatPurchaseOptions(purchase.Options),
            Is.EqualTo(["soldier | name | description | Cost: 50 gold | available"]));

        WorldCity cityBeforePurchase = Assert.IsOfType<WorldCity>(session.World.State.Fortifications[city.Id]);
        WorldPlayer playerBeforePurchase = session.World.State.Players[player.Id];

        await purchase.Options[0].PurchaseCommand.ExecuteAsync(null);

        WorldUnit purchasedSoldier = new(new WorldUnitId("Unit0001"), soldierDefinition.Id, 35, 0, 1, [], null, false);

        Assert.That(session.World.State.Fortifications[city.Id], Is.EqualTo(cityBeforePurchase with
        {
            Garrison = new UnitGrid<WorldUnit>.Builder()
                .Place(purchasedSoldier, GridPosition.FrontCenter, false)
                .Build()
        }));
        Assert.That(session.World.State.Players[player.Id], Is.EqualTo(playerBeforePurchase with
        {
            Resources = new WorldResourceSet(50, 0, 0, 0, 0, 0)
        }));
        Assert.That(gameViewModel.TopScreen, Is.Null);
    }

    [Test]
    public async Task VisitingStackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 3);
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 35, initiative: 50, damage: 10);

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 100, 0, leaderDefinition.Level, [], null, false);
        WorldUnit soldier = new(new WorldUnitId("soldier"), soldierDefinition.Id, 35, 0, soldierDefinition.Level, [], null, false);
        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .AddUnit(soldierDefinition)
            .Build();

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(
                new WorldCityDefinitionId(WorldRace.Human, 3),
                WorldResourceSet.Empty) with
            {
                VisionRadius = 5
            };

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("Milonia"),
            cityDefinition,
            player.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Virgile"),
            player.Id,
            city.Entrance,
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [
                    (leader, GridPosition.FrontBottom),
                    (soldier, GridPosition.FrontCenter)
                ]),
            movementPoints: 20);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(cityDefinition)
            .WithCity(city)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel management = new(gameModel, gameViewModel, ScreenMusic.Keep, null, owner => [new CityTabViewModel(gameModel, gameViewModel, owner, city.Id)]);
        CityTabViewModel screen = Assert.IsOfType<CityTabViewModel>(management.Tabs[0]);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(screen.HasStack, Is.True);
            Assert.That(screen.Leader, Is.EqualTo(new StackLeaderViewModel(
                new AssetSpriteKey.UnitEventPortrait(ScenarioEventPortraitId.Unit(leaderDefinition.Id)),
                "name",
                [
                    new StatRowViewModel(IconMoniker.Hitpoint, "Hit points", new StatValueViewModel("100 / 100", 0)),
                    new StatRowViewModel(IconMoniker.Movement, "Movement", new StatValueViewModel("20 / 20", 0)),
                    new StatRowViewModel(IconMoniker.Stack, "Units", new StatValueViewModel("2 / 6", 0)),
                    new StatRowViewModel(IconMoniker.Leadership, "Leadership", new StatValueViewModel("3", 0))
                ],
                [],
                new UnitDetailsViewModel(
                    new AssetSpriteKey.UnitEventPortrait(ScenarioEventPortraitId.Unit(leaderDefinition.Id)),
                    "name",
                    "description",
                    1,
                    100,
                    100,
                    0,
                    1000,
                    false,
                    false,
                    false,
                    false,
                    "Damage (Weapon)",
                    new StatValueViewModel("10", 0),
                    null,
                    [
                        new StatRowViewModel(IconMoniker.Target, "Targets", new StatValueViewModel("1", 0)),
                        new StatRowViewModel(IconMoniker.Armor, "Armor", new StatValueViewModel("0", 0)),
                        new StatRowViewModel(IconMoniker.Accuracy, "Accuracy", new StatValueViewModel("100%", 0)),
                        new StatRowViewModel(IconMoniker.Initiative, "Initiative", new StatValueViewModel("50", 0)),
                        new StatRowViewModel(IconMoniker.Immunity, "Immunities", new StatValueViewModel("None", 0)),
                        new StatRowViewModel(IconMoniker.Ward, "Wards", new StatValueViewModel("None", 0)),
                        new StatRowViewModel(IconMoniker.Leadership, "Leadership", new StatValueViewModel("3", 0))
                    ],
                    [],
                    []))));
            Assert.That(screen.StackGridTitle, Is.EqualTo("name's party"));
            Assert.That(screen.StackLeadership, Is.EqualTo("2 / 3"));
            Assert.That(screen.GarrisonCapacity, Is.EqualTo("0 / 3"));
            Assert.That(management.PlayerResources, Is.EqualTo(new ResourceSetViewModel(new WorldResourceSet(100, 0, 0, 25, 0, 0), ResourceSetProjection.Gold)));
        }

        UnitGridSlotViewModel soldierSlot = FindSlot(screen.StackGrid, GridPosition.FrontCenter);
        await soldierSlot.SelectCommand.ExecuteAsync(null);

        Assert.That(screen.Leader, Is.EqualTo(new StackLeaderViewModel(
            new AssetSpriteKey.UnitEventPortrait(ScenarioEventPortraitId.Unit(leaderDefinition.Id)),
            "name",
            [
                new StatRowViewModel(IconMoniker.Hitpoint, "Hit points", new StatValueViewModel("100 / 100", 0)),
                new StatRowViewModel(IconMoniker.Movement, "Movement", new StatValueViewModel("20 / 20", 0)),
                new StatRowViewModel(IconMoniker.Stack, "Units", new StatValueViewModel("2 / 6", 0)),
                new StatRowViewModel(IconMoniker.Leadership, "Leadership", new StatValueViewModel("3", 0))
            ],
            [],
            new UnitDetailsViewModel(
                new AssetSpriteKey.UnitEventPortrait(ScenarioEventPortraitId.Unit(leaderDefinition.Id)),
                "name",
                "description",
                1,
                100,
                100,
                0,
                1000,
                false,
                false,
                false,
                false,
                "Damage (Weapon)",
                new StatValueViewModel("10", 0),
                null,
                [
                    new StatRowViewModel(IconMoniker.Target, "Targets", new StatValueViewModel("1", 0)),
                    new StatRowViewModel(IconMoniker.Armor, "Armor", new StatValueViewModel("0", 0)),
                    new StatRowViewModel(IconMoniker.Accuracy, "Accuracy", new StatValueViewModel("100%", 0)),
                    new StatRowViewModel(IconMoniker.Initiative, "Initiative", new StatValueViewModel("50", 0)),
                    new StatRowViewModel(IconMoniker.Immunity, "Immunities", new StatValueViewModel("None", 0)),
                    new StatRowViewModel(IconMoniker.Ward, "Wards", new StatValueViewModel("None", 0)),
                    new StatRowViewModel(IconMoniker.Leadership, "Leadership", new StatValueViewModel("3", 0))
                ],
                [],
                []))));
    }

    [Test]
    public async Task ExitStackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 3);

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 100, 0, leaderDefinition.Level, [], null, false);
        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .Build();

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(
                new WorldCityDefinitionId(WorldRace.Human, 3),
                WorldResourceSet.Empty) with
            {
                VisionRadius = 5
            };

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("Milonia"),
            cityDefinition,
            player.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Virgile"),
            player.Id,
            city.Entrance,
            TestHelpers.StackUnitGrid(stackDefinitions, [(leader, GridPosition.FrontBottom)]),
            movementPoints: 20);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(cityDefinition)
            .WithCity(city)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel management = new(gameModel, gameViewModel, ScreenMusic.Keep, null, owner => [new CityTabViewModel(gameModel, gameViewModel, owner, city.Id)]);
        CityTabViewModel screen = Assert.IsOfType<CityTabViewModel>(management.Tabs[0]);

        Assert.That(screen.ExitStackCommand.CanExecute(null), Is.True);

        await screen.ExitStackCommand.ExecuteAsync(null);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(session.World.State.Stacks[stack.Id], Is.EqualTo(stack with { Position = new WorldPosition(5, 5) }));
            Assert.That(screen.HasStack, Is.False);
            Assert.That(screen.Game.Alert.Current, Is.Null);
            Assert.That(screen.ExitStackCommand.CanExecute(null), Is.False);
        }
    }

    [Test]
    public async Task NoVisitingStackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayerBuildingDefinition tower = new(
            Id: new WorldPlayerBuildingDefinitionId("G000BB0012"),
            NameTextId: new GlobalTextId("Tower"),
            DescriptionTextId: new GlobalTextId("Tower description"),
            RequiredId: null,
            Category: WorldPlayerBuildingCategory.Guild,
            UnitBranch: null,
            Level: 1,
            Cost: WorldResourceSet.Empty,
            UnitIds: []);
        WorldPlayerBuildingDefinition hall = new(
            Id: new WorldPlayerBuildingDefinitionId("G000BB0023"),
            NameTextId: new GlobalTextId("Hall"),
            DescriptionTextId: new GlobalTextId("Hall description"),
            RequiredId: null,
            Category: WorldPlayerBuildingCategory.Guild,
            UnitBranch: null,
            Level: 1,
            Cost: WorldResourceSet.Empty,
            UnitIds: []);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0),
            Buildings = new WorldPlayerBuildings([], [tower.Id, hall.Id])
        };

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 3);
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 35, initiative: 50, damage: 10);

        WorldUnit soldier = new(new WorldUnitId("soldier"), soldierDefinition.Id, 35, 0, soldierDefinition.Level, [], null, false);

        WorldRaceDefinition raceDefinition = new(
            Race: WorldRace.Human,
            NameTextId: new GlobalTextId("Human"),
            Income: WorldResourceSet.Empty,
            NativeTerrain: WorldMapTerrain.Human,
            IsPlayable: true,
            CapitalRegeneration: 35,
            Cities: [],
            Leaders: [leaderDefinition.Id],
            Units: [soldierDefinition.Id],
            MaleLeaderNames: [],
            FemaleLeaderNames: []);

        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("cap"),
            player.Id,
            new WorldPosition(4, 4),
            new UnitGrid<WorldUnit>.Builder()
                .Place(soldier, GridPosition.FrontCenter, false)
                .Build());

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(tower)
            .WithDefinition(hall)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithRace(raceDefinition)
            .WithCapital(capital)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel management = new(gameModel, gameViewModel, ScreenMusic.Keep, null, owner => [new CityTabViewModel(gameModel, gameViewModel, owner, capital.Id)]);
        CityTabViewModel screen = Assert.IsOfType<CityTabViewModel>(management.Tabs[0]);

        Assert.That(screen.CapitalScene, Is.Not.Null);

        IReadOnlyList<CapitalSceneLayerViewModel> expectedLayers =
        [
            new(
                new AssetSpriteKey.CapitalBackground(WorldRace.Human, 0),
                AnimationKey: null,
                IsConstructed: false),
            new(
                new AssetSpriteKey.CapitalBackground(WorldRace.Human, 1),
                AnimationKey: null,
                IsConstructed: false),
            new(
                SpriteKey: null,
                AnimationKey: new AssetAnimationKey.CapitalBackgroundOverlay(WorldRace.Human, 0),
                IsConstructed: false),
            new(
                SpriteKey: null,
                AnimationKey: new AssetAnimationKey.CapitalBuilding(new WorldPlayerBuildingDefinitionId("G000BB0012")),
                IsConstructed: false),
            new(
                new AssetSpriteKey.CapitalBackground(WorldRace.Human, 2),
                AnimationKey: null,
                IsConstructed: false),
            new(
                SpriteKey: null,
                AnimationKey: new AssetAnimationKey.CapitalBackgroundOverlay(WorldRace.Human, 1),
                IsConstructed: false),
            new(
                SpriteKey: null,
                AnimationKey: new AssetAnimationKey.CapitalBuilding(new WorldPlayerBuildingDefinitionId("G000BB0023")),
                IsConstructed: false)
        ];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(screen.HasStack, Is.False);
            Assert.That(screen.Leader, Is.Null);
            Assert.That(screen.StackGridTitle, Is.Empty);
            Assert.That(management.PlayerResources, Is.EqualTo(new ResourceSetViewModel(new WorldResourceSet(100, 0, 0, 0, 0, 0), ResourceSetProjection.Gold)));
            Assert.That(screen.CityTitle, Is.EqualTo("Name"));
            Assert.That(screen.CitySummary, Is.EqualTo(TextSpans.FromText("Capital")));
            Assert.That(screen.InventoryTransfer.Inventory.Items, Is.Empty);
            Assert.That(screen.CityImageKey, Is.EqualTo(new AssetSpriteKey.CapitalImage(WorldRace.Human)));
            Assert.That(screen.CapitalScene.Layers, Is.EqualTo(expectedLayers));
        }

        Assert.That(screen.StackGrid.Slots.Select(slot => (slot.Position, slot.Row, slot.Column, slot.ColumnSpan, slot.HasUnit, slot.IsVisible)), Is.EqualTo([
            (new GridPosition(0, 0), 2, 1, 1, false, true),
            (new GridPosition(0, 1), 1, 1, 1, false, true),
            (new GridPosition(0, 2), 0, 1, 1, false, true),
            (new GridPosition(1, 0), 2, 0, 1, false, true),
            (new GridPosition(1, 1), 1, 0, 1, false, true),
            (new GridPosition(1, 2), 0, 0, 1, false, true)
        ]));

        Assert.That(FindSlot(screen.GarrisonGrid, GridPosition.FrontCenter).HasUnit, Is.True);
        Assert.That(FindSlot(screen.GarrisonGrid, GridPosition.BackCenter).ShowsPurchaseHint, Is.True);

        WorldCapital capitalBeforeReposition = Assert.IsOfType<WorldCapital>(session.World.State.Fortifications[capital.Id]);

        UnitGridSlotViewModel source = FindSlot(screen.GarrisonGrid, GridPosition.FrontCenter);
        UnitGridSlotViewModel target = FindSlot(screen.GarrisonGrid, GridPosition.BackCenter);

        screen.DragDrop.TryBeginDrag(source.Reference);
        await screen.DragDrop.DropAsync(target.Reference);

        Assert.That(session.World.State.Fortifications[capital.Id], Is.EqualTo(capitalBeforeReposition with
        {
            Garrison = new UnitGrid<WorldUnit>.Builder()
                .Place(soldier, GridPosition.BackCenter, false)
                .Build()
        }));
    }

    [Test]
    public async Task ItemToCityTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 3);
        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 100, 0, leaderDefinition.Level, [], null, false);
        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .Build();

        ItemDefinition potionDefinition = new(
            new ItemDefinitionId("potion"),
            new GlobalTextId("Potion"),
            new GlobalTextId("Potion description"),
            ItemCategory.PotionHeal,
            WorldResourceSet.Empty,
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: null,
            AnimationUnitId: null);

        WorldItem potionItem = new(new WorldItemId("Potion"), potionDefinition.Id, Charges: null);

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(
                new WorldCityDefinitionId(WorldRace.Human, 3),
                WorldResourceSet.Empty) with
            {
                VisionRadius = 5
            };

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("Milonia"),
            cityDefinition,
            player.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Virgile"),
                player.Id,
                city.Entrance,
                TestHelpers.StackUnitGrid(stackDefinitions, [(leader, GridPosition.FrontBottom)]),
                movementPoints: 20) with
            {
                Inventory = new WorldInventory([potionItem])
            };

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(potionDefinition)
            .WithDefinition(cityDefinition)
            .WithCity(city)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel management = new(gameModel, gameViewModel, ScreenMusic.Keep, null, owner => [new CityTabViewModel(gameModel, gameViewModel, owner, city.Id)]);
        CityTabViewModel screen = Assert.IsOfType<CityTabViewModel>(management.Tabs[0]);

        InventoryItemViewModel source = screen.InventoryTransfer.Inventory.Items[0];
        InventoryViewModel target = screen.InventoryTransfer.OtherInventory;

        screen.DragDrop.TryBeginDrag(source.Reference);

        Assert.That(target.IsHighlighted, Is.True);
        Assert.That(screen.InventoryTransfer.Inventory.IsHighlighted, Is.False);

        await screen.DragDrop.DropAsync(target.DropReference);

        WorldStack updatedStack = session.World.State.Stacks[stack.Id];
        WorldCity updatedCity = Assert.IsOfType<WorldCity>(session.World.State.Fortifications[city.Id]);

        Assert.That(updatedStack, Is.EqualTo(stack with { Inventory = WorldInventory.Empty }));
        Assert.That(updatedCity, Is.EqualTo(city with { Inventory = new WorldInventory([potionItem]) }));
        Assert.That(screen.Game.Alert.Current, Is.Null);
    }

    [Test]
    public async Task ItemToStackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 3);
        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 100, 0, leaderDefinition.Level, [], null, false);
        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .Build();

        ItemDefinition potionDefinition = new(
            new ItemDefinitionId("potion"),
            new GlobalTextId("Potion"),
            new GlobalTextId("Potion description"),
            ItemCategory.PotionHeal,
            WorldResourceSet.Empty,
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: null,
            AnimationUnitId: null);

        WorldItem potionItem = new(new WorldItemId("Potion"), potionDefinition.Id, Charges: null);

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(
                new WorldCityDefinitionId(WorldRace.Human, 3),
                WorldResourceSet.Empty) with
            {
                VisionRadius = 5
            };

        WorldCity city = TestHelpers.SimpleCity(
                new WorldFortificationId("Milonia"),
                cityDefinition,
                player.Id,
                new WorldPosition(4, 4),
                UnitGrid<WorldUnit>.Empty) with
            {
                Inventory = new WorldInventory([potionItem])
            };

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Virgile"),
            player.Id,
            city.Entrance,
            TestHelpers.StackUnitGrid(stackDefinitions, [(leader, GridPosition.FrontBottom)]),
            movementPoints: 20);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(potionDefinition)
            .WithDefinition(cityDefinition)
            .WithCity(city)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel management = new(gameModel, gameViewModel, ScreenMusic.Keep, null, owner => [new CityTabViewModel(gameModel, gameViewModel, owner, city.Id)]);
        CityTabViewModel screen = Assert.IsOfType<CityTabViewModel>(management.Tabs[0]);

        InventoryItemViewModel source = screen.InventoryTransfer.OtherInventory.Items[0];
        InventoryViewModel target = screen.InventoryTransfer.Inventory;

        screen.DragDrop.TryBeginDrag(source.Reference);

        Assert.That(target.IsHighlighted, Is.True);
        Assert.That(screen.InventoryTransfer.OtherInventory.IsHighlighted, Is.False);

        await screen.DragDrop.DropAsync(target.DropReference);

        WorldStack updatedStack = session.World.State.Stacks[stack.Id];
        WorldCity updatedCity = Assert.IsOfType<WorldCity>(session.World.State.Fortifications[city.Id]);

        Assert.That(updatedStack, Is.EqualTo(stack with { Inventory = new WorldInventory([potionItem]) }));
        Assert.That(updatedCity, Is.EqualTo(city with { Inventory = WorldInventory.Empty }));
        Assert.That(screen.Game.Alert.Current, Is.Null);
    }

    [Test]
    public async Task HealingActionsTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayerBuildingDefinition temple = new(
            Id: new WorldPlayerBuildingDefinitionId("temple"),
            NameTextId: new GlobalTextId("Temple"),
            DescriptionTextId: new GlobalTextId("Temple description"),
            RequiredId: null,
            Category: WorldPlayerBuildingCategory.Heal,
            UnitBranch: null,
            Level: 1,
            Cost: WorldResourceSet.Empty,
            UnitIds: []);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map) with
        {
            Resources = new WorldResourceSet(200, 0, 0, 0, 0, 0),
            Buildings = new WorldPlayerBuildings([temple.Id], [temple.Id]),
            Spellbook = new WorldPlayerSpellbook([], [])
        };

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 3);
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            HealCost = new WorldResourceSet(1, 0, 0, 0, 0, 0),
            ReviveCost = new WorldResourceSet(50, 0, 0, 0, 0, 0)
        };

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 100, 0, leaderDefinition.Level, [], null, false);
        WorldUnit woundedSoldier = new(new WorldUnitId("woundedSoldier"), soldierDefinition.Id, 30, 0, soldierDefinition.Level, [], null, false);
        WorldUnit deadSoldier = new(new WorldUnitId("deadSoldier"), soldierDefinition.Id, 0, 0, soldierDefinition.Level, [], null, false);

        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .AddUnit(soldierDefinition)
            .Build();

        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("cap"),
            player.Id,
            new WorldPosition(4, 4),
            new UnitGrid<WorldUnit>.Builder()
                .Place(deadSoldier, GridPosition.FrontCenter, false)
                .Build());

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            player.Id,
            capital.Entrance,
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [
                    (leader, GridPosition.FrontBottom),
                    (woundedSoldier, GridPosition.FrontCenter)
                ]),
            movementPoints: 20);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Mage))
            .WithPlayer(player)
            .WithDefinition(temple)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithCapital(capital)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());
        WorldState initialState = session.World.State;
        WorldStack initialStack = initialState.Stacks[stack.Id];
        WorldCapital initialCapital = Assert.IsOfType<WorldCapital>(initialState.Fortifications[capital.Id]);

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel management = new(gameModel, gameViewModel, ScreenMusic.Keep, null, owner => [new CityTabViewModel(gameModel, gameViewModel, owner, capital.Id)]);
        CityTabViewModel screen = Assert.IsOfType<CityTabViewModel>(management.Tabs[0]);

        screen.HealCommand.Execute(null);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(screen.TargetedAction, Is.EqualTo(CityTabViewModel.UnitActionType.Heal));
            Assert.That(screen.StackGrid.Slots.Select(slot => slot.IsHighlighted), Is.EqualTo([false, true, false, false, false, false]));
            Assert.That(screen.GarrisonGrid.Slots.Select(slot => slot.IsHighlighted), Is.EqualTo([false, false, false, false, false, false]));
        }

        UnitGridSlotViewModel stackSlot = FindSlot(screen.StackGrid, GridPosition.FrontCenter);
        await stackSlot.SelectCommand.ExecuteAsync(null);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(screen.TargetedAction, Is.EqualTo(CityTabViewModel.UnitActionType.Heal));
            Assert.That(screen.StackGrid.Slots.Select(slot => slot.IsHighlighted), Is.EqualTo([false, false, false, false, false, false]));
        }

        screen.ResurrectCommand.Execute(null);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(screen.TargetedAction, Is.EqualTo(CityTabViewModel.UnitActionType.Resurrect));
            Assert.That(screen.StackGrid.Slots.Select(slot => slot.IsHighlighted), Is.EqualTo([false, false, false, false, false, false]));
            Assert.That(screen.GarrisonGrid.Slots.Select(slot => slot.IsHighlighted), Is.EqualTo([false, true, false, false, false, false]));
        }

        UnitGridSlotViewModel garrisonSlot = FindSlot(screen.GarrisonGrid, GridPosition.FrontCenter);
        await garrisonSlot.SelectCommand.ExecuteAsync(null);

        management.Close();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(screen.TargetedAction, Is.Null);
            Assert.That(screen.GarrisonGrid.Slots.Select(slot => slot.IsHighlighted), Is.EqualTo([false, false, false, false, false, false]));
        }

        Assert.That(session.World.State, Is.EqualTo(initialState with
        {
            Players = initialState.Players.SetItem(player.Id, initialState.Players[player.Id] with
            {
                Resources = new WorldResourceSet(220, 0, 0, 25, 0, 0)
            }),
            Stacks = initialState.Stacks.SetItem(stack.Id, initialStack with
            {
                Units = TestHelpers.StackUnitGrid(
                    stackDefinitions,
                    [
                        (leader, GridPosition.FrontBottom),
                        (woundedSoldier with { Hitpoints = 100 }, GridPosition.FrontCenter)
                    ])
            }),
            Fortifications = initialState.Fortifications.SetItem(capital.Id, initialCapital with
            {
                Garrison = new UnitGrid<WorldUnit>.Builder()
                    .Place(deadSoldier with { Hitpoints = 1 }, GridPosition.FrontCenter, false)
                    .Build()
            })
        }));
        Assert.That(screen.Game.Alert.Current, Is.Null);
    }

    [Test]
    public async Task ItemOnStackUnitTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 3);
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 35, initiative: 50, damage: 10);

        ItemDefinition potion = HealingPotion(50);
        WorldItem potionItem = new(new WorldItemId("Potion"), potion.Id, Charges: null);

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 100, 0, leaderDefinition.Level, [], null, false);
        WorldUnit soldier = new(new WorldUnitId("soldier"), soldierDefinition.Id, 10, 0, soldierDefinition.Level, [], null, false);
        WorldUnit garrisonSoldier = new(new WorldUnitId("garrisonSoldier"), soldierDefinition.Id, 20, 0, soldierDefinition.Level, [], null, false);

        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .AddUnit(soldierDefinition)
            .Build();

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(
                new WorldCityDefinitionId(WorldRace.Human, 3),
                WorldResourceSet.Empty) with
            {
                VisionRadius = 5
            };

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("Milonia"),
            cityDefinition,
            player.Id,
            new WorldPosition(4, 4),
            new UnitGrid<WorldUnit>.Builder()
                .Place(garrisonSoldier, GridPosition.FrontCenter, false)
                .Build());

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Virgile"),
                player.Id,
                city.Entrance,
                TestHelpers.StackUnitGrid(
                    stackDefinitions,
                    [
                        (leader, GridPosition.FrontBottom),
                        (soldier, GridPosition.FrontCenter)
                    ]),
                movementPoints: 20) with
            {
                Inventory = new WorldInventory([potionItem])
            };

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(potion)
            .WithDefinition(cityDefinition)
            .WithCity(city)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());
        WorldState initialState = session.World.State;
        WorldStack initialStack = initialState.Stacks[stack.Id];
        WorldUnit initialLeader = Assert.NotNull(initialStack.Units[GridPosition.FrontBottom]);
        WorldUnit initialSoldier = Assert.NotNull(initialStack.Units[GridPosition.FrontCenter]);

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel management = new(gameModel, gameViewModel, ScreenMusic.Keep, null, owner => [new CityTabViewModel(gameModel, gameViewModel, owner, city.Id)]);
        CityTabViewModel screen = Assert.IsOfType<CityTabViewModel>(management.Tabs[0]);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(FindSlot(screen.StackGrid, GridPosition.FrontBottom).IsHighlighted, Is.False);
            Assert.That(FindSlot(screen.StackGrid, GridPosition.FrontCenter).IsHighlighted, Is.False);
            Assert.That(FindSlot(screen.GarrisonGrid, GridPosition.FrontCenter).IsHighlighted, Is.False);
        }

        screen.DragDrop.TryBeginDrag(screen.InventoryTransfer.Inventory.Items[0].Reference);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(FindSlot(screen.StackGrid, GridPosition.FrontBottom).IsHighlighted, Is.False);
            Assert.That(FindSlot(screen.StackGrid, GridPosition.FrontCenter).IsHighlighted, Is.True);
            Assert.That(FindSlot(screen.GarrisonGrid, GridPosition.FrontCenter).IsHighlighted, Is.False);
        }

        await screen.DragDrop.DropAsync(FindSlot(screen.StackGrid, GridPosition.FrontCenter).Reference);

        Assert.That(session.World.State, Is.EqualTo(initialState with
        {
            Stacks = initialState.Stacks.SetItem(stack.Id, initialStack with
            {
                Inventory = WorldInventory.Empty,
                Units = TestHelpers.StackUnitGrid(
                    stackDefinitions,
                    [
                        (initialLeader, GridPosition.FrontBottom),
                        (initialSoldier with { Hitpoints = 35 }, GridPosition.FrontCenter)
                    ])
            })
        }));
        Assert.That(FindSlot(screen.StackGrid, GridPosition.FrontCenter).IsHighlighted, Is.False);
        Assert.That(screen.Game.Alert.Current, Is.Null);
    }

    [Test]
    public async Task DisbandStackUnitTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 3);
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 35, initiative: 50, damage: 10);

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 100, 0, leaderDefinition.Level, [], null, false);
        WorldUnit soldier = new(new WorldUnitId("soldier"), soldierDefinition.Id, 35, 0, soldierDefinition.Level, [], null, false);
        WorldUnit garrisonSoldier = new(new WorldUnitId("garrisonSoldier"), soldierDefinition.Id, 20, 0, soldierDefinition.Level, [], null, false);

        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .AddUnit(soldierDefinition)
            .Build();

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(
                new WorldCityDefinitionId(WorldRace.Human, 3),
                WorldResourceSet.Empty) with
            {
                VisionRadius = 5
            };

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("Milonia"),
            cityDefinition,
            player.Id,
            new WorldPosition(4, 4),
            new UnitGrid<WorldUnit>.Builder()
                .Place(garrisonSoldier, GridPosition.FrontCenter, false)
                .Build());

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Virgile"),
            player.Id,
            city.Entrance,
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [
                    (leader, GridPosition.FrontBottom),
                    (soldier, GridPosition.FrontCenter)
                ]),
            movementPoints: 20);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(cityDefinition)
            .WithCity(city)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());
        WorldState initialState = session.World.State;

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel management = new(gameModel, gameViewModel, ScreenMusic.Keep, null, owner => [new CityTabViewModel(gameModel, gameViewModel, owner, city.Id)]);
        CityTabViewModel screen = Assert.IsOfType<CityTabViewModel>(management.Tabs[0]);

        screen.DragDrop.TryBeginDrag(FindSlot(screen.StackGrid, GridPosition.FrontBottom).Reference);

        Assert.That(screen.DismissDropZone.IsVisible, Is.False);

        screen.DragDrop.EndDrag();
        screen.DragDrop.TryBeginDrag(FindSlot(screen.StackGrid, GridPosition.FrontCenter).Reference);

        Assert.That(screen.DismissDropZone.IsVisible, Is.True);

        await screen.DragDrop.DropAsync(screen.DismissDropZone.DropReference);

        ConfirmPromptViewModel prompt = Assert.IsOfType<ConfirmPromptViewModel>(gameViewModel.TopScreen);
        await prompt.ConfirmCommand.ExecuteAsync(null);

        Assert.That(session.World.State, Is.EqualTo(initialState with
        {
            Stacks = initialState.Stacks.SetItem(stack.Id, initialState.Stacks[stack.Id] with
            {
                Units = TestHelpers.StackUnitGrid(stackDefinitions, [(leader, GridPosition.FrontBottom)])
            })
        }));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(gameViewModel.TopScreen, Is.Null);
            Assert.That(screen.DismissDropZone.IsVisible, Is.False);
            Assert.That(screen.Game.Alert.Current, Is.Null);
        }
    }

    [Test]
    public async Task UpgradeCityTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map) with
        {
            Resources = new WorldResourceSet(500, 0, 0, 0, 0, 0)
        };

        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 35, initiative: 50, damage: 10);
        WorldUnit garrisonSoldier = new(new WorldUnitId("garrisonSoldier"), soldierDefinition.Id, 35, 0, soldierDefinition.Level, [], null, false);

        WorldCityDefinition level1 = TestHelpers.SimpleCityDefinition(
                new WorldCityDefinitionId(WorldRace.Human, 1),
                new WorldResourceSet(150, 0, 0, 0, 0, 0)) with
            {
                VisionRadius = 5
            };

        WorldCityDefinition level2 = TestHelpers.SimpleCityDefinition(
                new WorldCityDefinitionId(WorldRace.Human, 2),
                new WorldResourceSet(250, 0, 0, 0, 0, 0)) with
            {
                VisionRadius = 5
            };

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("Milonia"),
            level1,
            player.Id,
            new WorldPosition(4, 4),
            new UnitGrid<WorldUnit>.Builder()
                .Place(garrisonSoldier, GridPosition.FrontCenter, false)
                .Build());

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(soldierDefinition)
            .WithDefinition(level1)
            .WithDefinition(level2)
            .WithCity(city)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel management = new(gameModel, gameViewModel, ScreenMusic.Keep, null, owner => [new CityTabViewModel(gameModel, gameViewModel, owner, city.Id)]);
        CityTabViewModel screen = Assert.IsOfType<CityTabViewModel>(management.Tabs[0]);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(screen.UpgradeLabel, Is.EqualTo("Upgrade city"));
            Assert.That(screen.UpgradeCost, Is.EqualTo(new ResourceSetViewModel(new WorldResourceSet(150, 0, 0, 0, 0, 0), ResourceSetProjection.All)));
            Assert.That(screen.GarrisonSummary.Text, Is.EqualTo("Units in city: 1"));
            Assert.That(screen.UpgradeCommand.CanExecute(null), Is.True);
        }

        screen.UpgradeCommand.Execute(null);

        ConfirmPromptViewModel prompt = Assert.IsOfType<ConfirmPromptViewModel>(gameViewModel.TopScreen);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(prompt.Message, Is.EqualTo(new TextSpans([new TextSpan("Upgrade the city to ", TextStyle.None), new TextSpan("level 2", TextStyle.Highlight), new TextSpan("? This will increase garrison capacity, regeneration and armor bonus.", TextStyle.None)])));
            Assert.That(prompt.ConfirmPrice, Is.EqualTo(new ResourceSetViewModel(new WorldResourceSet(150, 0, 0, 0, 0, 0), ResourceSetProjection.All)));
        }

        await prompt.ConfirmCommand.ExecuteAsync(null);

        WorldCity upgradedCity = Assert.IsOfType<WorldCity>(session.World.State.Fortifications[city.Id]);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(upgradedCity.DefinitionId, Is.EqualTo(level2.Id));
            Assert.That(screen.UpgradeLabel, Is.EqualTo("City fully upgraded"));
            Assert.That(screen.UpgradeCost, Is.Null);
            Assert.That(screen.UpgradeCommand.CanExecute(null), Is.False);
        }
    }

    [Test]
    public async Task UpgradeCityAsGuildmasterTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map) with
        {
            Resources = new WorldResourceSet(500, 0, 0, 0, 0, 0)
        };

        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 35, initiative: 50, damage: 10);
        WorldUnit garrisonSoldier = new(new WorldUnitId("garrisonSoldier"), soldierDefinition.Id, 35, 0, soldierDefinition.Level, [], null, false);

        WorldCityDefinition level1 = TestHelpers.SimpleCityDefinition(
                new WorldCityDefinitionId(WorldRace.Human, 1),
                new WorldResourceSet(150, 0, 0, 0, 0, 0)) with
            {
                VisionRadius = 5
            };

        WorldCityDefinition level2 = TestHelpers.SimpleCityDefinition(
                new WorldCityDefinitionId(WorldRace.Human, 2),
                new WorldResourceSet(250, 0, 0, 0, 0, 0)) with
            {
                VisionRadius = 5
            };

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("Milonia"),
            level1,
            player.Id,
            new WorldPosition(4, 4),
            new UnitGrid<WorldUnit>.Builder()
                .Place(garrisonSoldier, GridPosition.FrontCenter, false)
                .Build());

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Guildmaster))
            .WithPlayer(player)
            .WithDefinition(soldierDefinition)
            .WithDefinition(level1)
            .WithDefinition(level2)
            .WithCity(city)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel management = new(gameModel, gameViewModel, ScreenMusic.Keep, null, owner => [new CityTabViewModel(gameModel, gameViewModel, owner, city.Id)]);
        CityTabViewModel screen = Assert.IsOfType<CityTabViewModel>(management.Tabs[0]);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(screen.UpgradeLabel, Is.EqualTo("Upgrade city"));
            Assert.That(screen.UpgradeCost, Is.EqualTo(new ResourceSetViewModel(new WorldResourceSet(75, 0, 0, 0, 0, 0), ResourceSetProjection.All)));
            Assert.That(screen.GarrisonSummary.Text, Is.EqualTo("Units in city: 1"));
            Assert.That(screen.UpgradeCommand.CanExecute(null), Is.True);
        }

        screen.UpgradeCommand.Execute(null);

        ConfirmPromptViewModel prompt = Assert.IsOfType<ConfirmPromptViewModel>(gameViewModel.TopScreen);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(prompt.Message, Is.EqualTo(new TextSpans([new TextSpan("Upgrade the city to ", TextStyle.None), new TextSpan("level 2", TextStyle.Highlight), new TextSpan("? This will increase garrison capacity, regeneration and armor bonus.", TextStyle.None)])));
            Assert.That(prompt.ConfirmPrice, Is.EqualTo(new ResourceSetViewModel(new WorldResourceSet(75, 0, 0, 0, 0, 0), ResourceSetProjection.All)));
        }

        await prompt.ConfirmCommand.ExecuteAsync(null);

        WorldCity upgradedCity = Assert.IsOfType<WorldCity>(session.World.State.Fortifications[city.Id]);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(upgradedCity.DefinitionId, Is.EqualTo(level2.Id));
            Assert.That(screen.UpgradeLabel, Is.EqualTo("City fully upgraded"));
            Assert.That(screen.UpgradeCost, Is.Null);
            Assert.That(screen.UpgradeCommand.CanExecute(null), Is.False);
        }
    }

    [Test]
    public async Task DragItemToStackUnitTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 3);
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 35, initiative: 50, damage: 10);

        ItemDefinition potion = HealingPotion(50);
        WorldItem potionItem = new(new WorldItemId("Potion"), potion.Id, Charges: null);

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 100, 0, leaderDefinition.Level, [], null, false);
        WorldUnit soldier = new(new WorldUnitId("soldier"), soldierDefinition.Id, 10, 0, soldierDefinition.Level, [], null, false);
        WorldUnit garrisonSoldier = new(new WorldUnitId("garrisonSoldier"), soldierDefinition.Id, 20, 0, soldierDefinition.Level, [], null, false);

        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .AddUnit(soldierDefinition)
            .Build();

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(
                new WorldCityDefinitionId(WorldRace.Human, 3),
                WorldResourceSet.Empty) with
            {
                VisionRadius = 5
            };

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("Milonia"),
            cityDefinition,
            player.Id,
            new WorldPosition(4, 4),
            new UnitGrid<WorldUnit>.Builder()
                .Place(garrisonSoldier, GridPosition.FrontCenter, false)
                .Build());

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Virgile"),
                player.Id,
                city.Entrance,
                TestHelpers.StackUnitGrid(
                    stackDefinitions,
                    [
                        (leader, GridPosition.FrontBottom),
                        (soldier, GridPosition.FrontCenter)
                    ]),
                movementPoints: 20) with
            {
                Inventory = new WorldInventory([potionItem])
            };

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(potion)
            .WithDefinition(cityDefinition)
            .WithCity(city)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());
        WorldState initialState = session.World.State;
        WorldStack initialStack = initialState.Stacks[stack.Id];
        WorldUnit initialLeader = Assert.NotNull(initialStack.Units[GridPosition.FrontBottom]);
        WorldUnit initialSoldier = Assert.NotNull(initialStack.Units[GridPosition.FrontCenter]);

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel management = new(gameModel, gameViewModel, ScreenMusic.Keep, null, owner => [new CityTabViewModel(gameModel, gameViewModel, owner, city.Id)]);
        CityTabViewModel screen = Assert.IsOfType<CityTabViewModel>(management.Tabs[0]);

        screen.DragDrop.TryBeginDrag(screen.InventoryTransfer.Inventory.Items[0].Reference);
        await screen.DragDrop.DropAsync(FindSlot(screen.StackGrid, GridPosition.FrontCenter).Reference);

        Assert.That(session.World.State, Is.EqualTo(initialState with
        {
            Stacks = initialState.Stacks.SetItem(stack.Id, initialStack with
            {
                Inventory = WorldInventory.Empty,
                Units = TestHelpers.StackUnitGrid(
                    stackDefinitions,
                    [
                        (initialLeader, GridPosition.FrontBottom),
                        (initialSoldier with { Hitpoints = 35 }, GridPosition.FrontCenter)
                    ])
            })
        }));
        Assert.That(screen.Game.Alert.Current, Is.Null);
    }

    [Test]
    public async Task DragItemToGarrisonUnitTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 3);
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 35, initiative: 50, damage: 10);

        ItemDefinition potion = HealingPotion(50);
        WorldItem potionItem = new(new WorldItemId("Potion"), potion.Id, Charges: null);

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 100, 0, leaderDefinition.Level, [], null, false);
        WorldUnit garrisonSoldier = new(new WorldUnitId("garrisonSoldier"), soldierDefinition.Id, 20, 0, soldierDefinition.Level, [], null, false);

        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .AddUnit(soldierDefinition)
            .Build();

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(
                new WorldCityDefinitionId(WorldRace.Human, 3),
                WorldResourceSet.Empty) with
            {
                VisionRadius = 5
            };

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("Milonia"),
            cityDefinition,
            player.Id,
            new WorldPosition(4, 4),
            new UnitGrid<WorldUnit>.Builder()
                .Place(garrisonSoldier, GridPosition.FrontCenter, false)
                .Build());

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Virgile"),
                player.Id,
                city.Entrance,
                TestHelpers.StackUnitGrid(stackDefinitions, [(leader, GridPosition.FrontBottom)]),
                movementPoints: 20) with
            {
                Inventory = new WorldInventory([potionItem])
            };

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(potion)
            .WithDefinition(cityDefinition)
            .WithCity(city)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());
        WorldState initialState = session.World.State;

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel management = new(gameModel, gameViewModel, ScreenMusic.Keep, null, owner => [new CityTabViewModel(gameModel, gameViewModel, owner, city.Id)]);
        CityTabViewModel screen = Assert.IsOfType<CityTabViewModel>(management.Tabs[0]);

        screen.DragDrop.TryBeginDrag(screen.InventoryTransfer.Inventory.Items[0].Reference);
        await screen.DragDrop.DropAsync(FindSlot(screen.GarrisonGrid, GridPosition.FrontCenter).Reference);

        Assert.That(session.World.State, Is.EqualTo(initialState));
        Assert.That(screen.Game.Alert.Current, Is.Null);
    }

    [Test]
    public void NoUpgradeAtCapitalTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("cap"),
            player.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Guildmaster))
            .WithPlayer(player)
            .WithCapital(capital)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel management = new(gameModel, gameViewModel, ScreenMusic.Keep, null, owner => [new CityTabViewModel(gameModel, gameViewModel, owner, capital.Id)]);
        CityTabViewModel screen = Assert.IsOfType<CityTabViewModel>(management.Tabs[0]);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(screen.UpgradeLabel, Is.Empty);
            Assert.That(screen.UpgradeCommand.CanExecute(null), Is.False);
        }
    }

    [Test]
    public async Task DisbandGarrisonUnitTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 35, initiative: 50, damage: 10);

        WorldUnit frontSoldier = new(new WorldUnitId("frontSoldier"), soldierDefinition.Id, 35, 0, soldierDefinition.Level, [], null, false);
        WorldUnit backSoldier = new(new WorldUnitId("backSoldier"), soldierDefinition.Id, 20, 0, soldierDefinition.Level, [], null, false);

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(
                new WorldCityDefinitionId(WorldRace.Human, 3),
                WorldResourceSet.Empty) with
            {
                VisionRadius = 5
            };

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("Milonia"),
            cityDefinition,
            player.Id,
            new WorldPosition(4, 4),
            new UnitGrid<WorldUnit>.Builder()
                .Place(frontSoldier, GridPosition.FrontCenter, false)
                .Place(backSoldier, GridPosition.BackCenter, false)
                .Build());

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(soldierDefinition)
            .WithDefinition(cityDefinition)
            .WithCity(city)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());
        WorldState initialState = session.World.State;

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel management = new(gameModel, gameViewModel, ScreenMusic.Keep, null, owner => [new CityTabViewModel(gameModel, gameViewModel, owner, city.Id)]);
        CityTabViewModel screen = Assert.IsOfType<CityTabViewModel>(management.Tabs[0]);

        Assert.That(screen.DismissDropZone.IsVisible, Is.False);

        screen.DragDrop.TryBeginDrag(FindSlot(screen.GarrisonGrid, GridPosition.BackCenter).Reference);

        Assert.That(screen.DismissDropZone.IsVisible, Is.True);
        Assert.That(screen.DismissDropZone.Content, Is.EqualTo("Dismiss name"));

        await screen.DragDrop.DropAsync(screen.DismissDropZone.DropReference);

        ConfirmPromptViewModel prompt = Assert.IsOfType<ConfirmPromptViewModel>(gameViewModel.TopScreen);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(prompt.Title, Is.EqualTo("Dismiss unit"));
            Assert.That(prompt.Message, Is.EqualTo(new TextSpans([new TextSpan("Are you sure you want to dismiss ", TextStyle.None), new TextSpan("name", TextStyle.Highlight), new TextSpan("?", TextStyle.None)])));
            Assert.That(prompt.ConfirmLabel, Is.EqualTo("Dismiss"));
        }

        await prompt.ConfirmCommand.ExecuteAsync(null);

        Assert.That(session.World.State, Is.EqualTo(initialState with
        {
            Fortifications = initialState.Fortifications.SetItem(city.Id, initialState.Fortifications[city.Id] with
            {
                Garrison = new UnitGrid<WorldUnit>.Builder()
                    .Place(frontSoldier, GridPosition.FrontCenter, false)
                    .Build()
            })
        }));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(gameViewModel.TopScreen, Is.Null);
            Assert.That(FindSlot(screen.GarrisonGrid, GridPosition.BackCenter).HasUnit, Is.False);
            Assert.That(screen.DismissDropZone.IsVisible, Is.False);
            Assert.That(screen.Game.Alert.Current, Is.Null);
        }
    }

    [Test]
    public async Task NoHealBuildingTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            HealCost = new WorldResourceSet(1, 0, 0, 0, 0, 0),
            ReviveCost = new WorldResourceSet(50, 0, 0, 0, 0, 0)
        };

        WorldUnit woundedSoldier = new(new WorldUnitId("woundedSoldier"), soldierDefinition.Id, 30, 0, soldierDefinition.Level, [], null, false);

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty);

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            player.Id,
            new WorldPosition(4, 4),
            new UnitGrid<WorldUnit>.Builder()
                .Place(woundedSoldier, GridPosition.FrontCenter, false)
                .Build());

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(soldierDefinition)
            .WithDefinition(cityDefinition)
            .WithCity(city)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());
        WorldState initialState = session.World.State;

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel management = new(gameModel, gameViewModel, ScreenMusic.Keep, null, owner => [new CityTabViewModel(gameModel, gameViewModel, owner, city.Id)]);
        CityTabViewModel screen = Assert.IsOfType<CityTabViewModel>(management.Tabs[0]);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(screen.HealCommand.CanExecute(null), Is.True);
            Assert.That(screen.ResurrectCommand.CanExecute(null), Is.True);
        }

        screen.HealCommand.Execute(null);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(screen.TargetedAction, Is.Null);
            Assert.That(screen.GarrisonGrid.Slots.Select(slot => slot.IsHighlighted), Is.EqualTo([false, false, false, false, false, false]));
            Assert.That(screen.Game.Alert.Current, Is.EqualTo(new Alert(AlertType.Error, IconMoniker.Warning, "Temple must be built in your capital", Details: null)));
        }

        UnitGridSlotViewModel slot = FindSlot(screen.GarrisonGrid, GridPosition.FrontCenter);
        await slot.SelectCommand.ExecuteAsync(null);

        Assert.That(session.World.State, Is.EqualTo(initialState));
    }

    [Test]
    public async Task LockTypeTargetTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        UnitDefinition squireDefinition = TestHelpers.SimpleUnitDefinition("squire", maxHitpoints: 100, initiative: 40, damage: 10);
        UnitDefinition knightDefinition = TestHelpers.SimpleUnitDefinition("knight", maxHitpoints: 200, initiative: 50, damage: 20);

        WorldUnit squire = TestHelpers.SimpleUnit(new WorldUnitId("squire"), squireDefinition);
        WorldUnit knight = TestHelpers.SimpleUnit(new WorldUnitId("knight"), knightDefinition);

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty);

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            player.Id,
            new WorldPosition(4, 4),
            new UnitGrid<WorldUnit>.Builder()
                .Place(squire, GridPosition.FrontBottom, false)
                .Place(knight, GridPosition.FrontCenter, false)
                .Build());

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(squireDefinition)
            .WithDefinition(knightDefinition)
            .WithUnitUpgrade(new UnitUpgradeDefinition(squireDefinition.Id, knightDefinition.Id, RequiredBuildingId: null))
            .WithDefinition(cityDefinition)
            .WithCity(city)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());
        WorldState initialState = session.World.State;

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel management = new(gameModel, gameViewModel, ScreenMusic.Keep, null, owner => [new CityTabViewModel(gameModel, gameViewModel, owner, city.Id)]);
        CityTabViewModel screen = Assert.IsOfType<CityTabViewModel>(management.Tabs[0]);

        screen.LockTypeCommand.Execute(null);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(screen.TargetedAction, Is.EqualTo(CityTabViewModel.UnitActionType.LockType));
            Assert.That(screen.GarrisonGrid.Slots.Select(slot => slot.IsHighlighted), Is.EqualTo([true, false, false, false, false, false]));
        }

        UnitGridSlotViewModel slot = FindSlot(screen.GarrisonGrid, GridPosition.FrontBottom);
        await slot.SelectCommand.ExecuteAsync(null);

        ConfirmPromptViewModel prompt = Assert.IsOfType<ConfirmPromptViewModel>(gameViewModel.TopScreen);
        await prompt.ConfirmCommand.ExecuteAsync(null);

        Assert.That(session.World.State, Is.EqualTo(initialState with
        {
            Fortifications = initialState.Fortifications.SetItem(city.Id, initialState.Fortifications[city.Id] with
            {
                Garrison = new UnitGrid<WorldUnit>.Builder()
                    .Place(squire with { IsTypeLocked = true }, GridPosition.FrontBottom, false)
                    .Place(knight, GridPosition.FrontCenter, false)
                    .Build()
            })
        }));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(screen.TargetedAction, Is.EqualTo(CityTabViewModel.UnitActionType.LockType));
            Assert.That(screen.GarrisonGrid.Slots.Select(slot => slot.IsHighlighted), Is.EqualTo([false, false, false, false, false, false]));
        }

        screen.CancelTargetingCommand.Execute(null);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(screen.TargetedAction, Is.Null);
            Assert.That(screen.Game.Alert.Current, Is.Null);
        }

        screen.LockTypeCommand.Execute(null);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(screen.TargetedAction, Is.Null);
            Assert.That(screen.Game.Alert.Current, Is.EqualTo(new Alert(AlertType.Info, IconMoniker.Locked, "No units can have their type locked", Details: null)));
        }
    }

    [Test]
    public async Task NoGoldHealTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayerBuildingDefinition temple = new(
            Id: new WorldPlayerBuildingDefinitionId("temple"),
            NameTextId: new GlobalTextId("Temple"),
            DescriptionTextId: new GlobalTextId("Temple description"),
            RequiredId: null,
            Category: WorldPlayerBuildingCategory.Heal,
            UnitBranch: null,
            Level: 1,
            Cost: WorldResourceSet.Empty,
            UnitIds: []);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map) with
        {
            Resources = WorldResourceSet.Empty,
            Buildings = new WorldPlayerBuildings([temple.Id], [temple.Id])
        };

        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            HealCost = new WorldResourceSet(1000, 0, 0, 0, 0, 0),
            ReviveCost = new WorldResourceSet(50, 0, 0, 0, 0, 0)
        };

        WorldUnit woundedSoldier = new(new WorldUnitId("woundedSoldier"), soldierDefinition.Id, 30, 0, soldierDefinition.Level, [], null, false);

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty);

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            player.Id,
            new WorldPosition(4, 4),
            new UnitGrid<WorldUnit>.Builder()
                .Place(woundedSoldier, GridPosition.FrontCenter, false)
                .Build());

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(temple)
            .WithDefinition(soldierDefinition)
            .WithDefinition(cityDefinition)
            .WithCity(city)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());
        WorldState initialState = session.World.State;

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel management = new(gameModel, gameViewModel, ScreenMusic.Keep, null, owner => [new CityTabViewModel(gameModel, gameViewModel, owner, city.Id)]);
        CityTabViewModel screen = Assert.IsOfType<CityTabViewModel>(management.Tabs[0]);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(screen.HealCommand.CanExecute(null), Is.True);
            Assert.That(screen.ResurrectCommand.CanExecute(null), Is.True);
        }

        screen.HealCommand.Execute(null);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(screen.TargetedAction, Is.Null);
            Assert.That(screen.GarrisonGrid.Slots.Select(slot => slot.IsHighlighted), Is.EqualTo([false, false, false, false, false, false]));
            Assert.That(screen.Game.Alert.Current, Is.EqualTo(new Alert(AlertType.Info, IconMoniker.Heal, "No units can be healed", Details: null)));
        }

        UnitGridSlotViewModel slot = FindSlot(screen.GarrisonGrid, GridPosition.FrontCenter);
        await slot.SelectCommand.ExecuteAsync(null);

        Assert.That(session.World.State, Is.EqualTo(initialState));
    }
}
