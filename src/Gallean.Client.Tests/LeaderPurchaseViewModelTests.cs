using Gallean.Common;
using Gallean.Engine;
using Gallean.Engine.Tests;
using Gallean.Tests.Common;

namespace Gallean.Client.Tests;

public sealed class LeaderPurchaseViewModelTests
{
    private static IReadOnlyList<string> FormatOptions(IReadOnlyList<LeaderPurchaseItemViewModel> items)
    {
        List<string> rows = [];

        foreach (LeaderPurchaseItemViewModel item in items)
        {
            rows.Add($"{item.LeaderId.Value} | {item.Name} | {item.Description} | Cost: {FormatPrice(Assert.NotNull(item.Status.Price))} | {FormatAvailability(item.PurchaseCommand.CanExecute(null), item.Status.UnavailableReason)}");
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

    private static string FormatAvailability(bool isEnabled, string? unavailableReason)
    {
        if (isEnabled)
        {
            return "enabled";
        }
        else if (unavailableReason is null)
        {
            return "disabled";
        }
        else
        {
            return $"disabled: {unavailableReason}";
        }
    }

    [Test]
    public async Task PurchaseLeaderTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map) with
        {
            Resources = new WorldResourceSet(150, 0, 0, 0, 0, 0)
        };

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 20, leadership: 3) with
            {
                PurchaseCost = new WorldResourceSet(100, 0, 0, 0, 0, 0)
            };

        WorldRaceDefinition raceDefinition = new(
            Race: WorldRace.Human,
            NameTextId: new GlobalTextId("Human"),
            Income: WorldResourceSet.Empty,
            NativeTerrain: WorldMapTerrain.Human,
            IsPlayable: true,
            CapitalRegeneration: 35,
            Cities: [],
            Leaders: [leaderDefinition.Id],
            Units: [],
            MaleLeaderNames: ["Konrad"],
            FemaleLeaderNames: ["Zolian"]);

        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("cap"),
            player.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldSubrace subrace = new(
            Id: new WorldSubraceId("subrace"),
            PlayerId: player.Id,
            Subrace: WorldSubraceType.Human,
            BannerVariant: new WorldObjectVariant(1));

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithRace(raceDefinition)
            .WithCapital(capital)
            .WithSubrace(subrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel management = new(gameModel, gameViewModel, ScreenMusic.Keep, null, owner => [new CityTabViewModel(gameModel, gameViewModel, owner, capital.Id)]);
        CityTabViewModel city = Assert.IsOfType<CityTabViewModel>(management.Tabs[0]);

        Assert.That(city.CityImageKey, Is.EqualTo(new AssetSpriteKey.CapitalImage(WorldRace.Human)));
        Assert.That(city.HasStack, Is.False);

        city.LeaderBlankslate.HireCommand.Execute(null);

        LeaderPurchaseViewModel purchase = Assert.IsOfType<LeaderPurchaseViewModel>(gameViewModel.TopScreen);

        Assert.That(FormatOptions(purchase.Options), Is.EqualTo(["leader | name | description | Cost: 100 gold | enabled"]));

        await purchase.Options[0].PurchaseCommand.ExecuteAsync(null);

        WorldStack purchasedStack = TestHelpers.SimpleStack(
                new WorldStackId("Stack0001"),
                player.Id,
                capital.Entrance,
                TestHelpers.StackUnitGrid(
                    engine.Definitions,
                    [(new WorldUnit(new WorldUnitId("Unit0001"), leaderDefinition.Id, 100, 0, leaderDefinition.Level, [], Name: "Konrad", IsTypeLocked: false), GridPosition.FrontCenter)]),
                movementPoints: 20) with
            {
                SubraceId = subrace.Id
            };

        Assert.That(session.World.State.Stacks.Values, Is.EqualTo([purchasedStack]));
        Assert.That(session.World.State.Players[player.Id].Resources, Is.EqualTo(new WorldResourceSet(50, 0, 0, 0, 0, 0)));
        Assert.That(gameViewModel.TopScreen, Is.Null);
        Assert.That(city.HasStack, Is.True);
    }

    [Test]
    public void UnavailableReasonsTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayerBuildingDefinitionId requiredBuildingId = new("guild");
        WorldPlayerBuildingDefinition requiredBuilding = new(
            Id: requiredBuildingId,
            NameTextId: new GlobalTextId("guild"),
            DescriptionTextId: new GlobalTextId("description"),
            RequiredId: null,
            Category: WorldPlayerBuildingCategory.Guild,
            UnitBranch: null,
            Level: 1,
            Cost: WorldResourceSet.Empty,
            UnitIds: []);

        UnitDefinition expensiveLeader = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("expensive", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 20, leadership: 3) with
            {
                PurchaseCost = new WorldResourceSet(500, 0, 0, 0, 0, 0)
            };
        UnitDefinition gatedLeader = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("gated", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 20, leadership: 3) with
            {
                RequiredBuildingId = requiredBuildingId
            };
        UnitDefinition blockedLeader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("blocked", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 3);
        UnitDefinition occupyingLeader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("occupying", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 3);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0)
        };

        WorldRaceDefinition raceDefinition = new(
            Race: WorldRace.Human,
            NameTextId: new GlobalTextId("Human"),
            Income: WorldResourceSet.Empty,
            NativeTerrain: WorldMapTerrain.Human,
            IsPlayable: true,
            CapitalRegeneration: 35,
            Cities: [],
            Leaders: [expensiveLeader.Id, gatedLeader.Id, blockedLeader.Id],
            Units: [],
            MaleLeaderNames: [],
            FemaleLeaderNames: []);

        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("cap"),
            player.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldStack occupyingStack = TestHelpers.SimpleStack(
            new WorldStackId("occupying"),
            player.Id,
            capital.Entrance,
            TestHelpers.SingleUnitGrid(new WorldUnitId("occupyingLeader"), occupyingLeader),
            movementPoints: 0);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(requiredBuilding)
            .WithDefinition(expensiveLeader)
            .WithDefinition(gatedLeader)
            .WithDefinition(blockedLeader)
            .WithDefinition(occupyingLeader)
            .WithRace(raceDefinition)
            .WithCapital(capital)
            .WithStack(occupyingStack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        LeaderPurchaseViewModel purchase = new(gameModel, gameViewModel, capital.Id);

        Assert.That(
            FormatOptions(purchase.Options),
            Is.EqualTo([
                "expensive | name | description | Cost: 500 gold (unaffordable) | disabled",
                "gated | name | description | Cost: 50 gold | disabled: Requires guild",
                "blocked | name | description | Cost: 50 gold | disabled: City entrance is occupied"
            ]));
    }

    [Test]
    public async Task PurchaseLeaderAtCityTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map) with
        {
            Resources = new WorldResourceSet(150, 0, 0, 0, 0, 0)
        };

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 20, leadership: 3) with
            {
                PurchaseCost = new WorldResourceSet(100, 0, 0, 0, 0, 0)
            };

        WorldRaceDefinition raceDefinition = new(
            Race: WorldRace.Human,
            NameTextId: new GlobalTextId("Human"),
            Income: WorldResourceSet.Empty,
            NativeTerrain: WorldMapTerrain.Human,
            IsPlayable: true,
            CapitalRegeneration: 35,
            Cities: [],
            Leaders: [leaderDefinition.Id],
            Units: [],
            MaleLeaderNames: ["Konrad"],
            FemaleLeaderNames: ["Zolian"]);

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Human, 3),
            WorldResourceSet.Empty);

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("Milonia"),
            cityDefinition,
            player.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldSubrace subrace = new(
            Id: new WorldSubraceId("subrace"),
            PlayerId: player.Id,
            Subrace: WorldSubraceType.Human,
            BannerVariant: new WorldObjectVariant(1));

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(cityDefinition)
            .WithRace(raceDefinition)
            .WithCity(city)
            .WithSubrace(subrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel management = new(gameModel, gameViewModel, ScreenMusic.Keep, null, owner => [new CityTabViewModel(gameModel, gameViewModel, owner, city.Id)]);
        CityTabViewModel cityTab = Assert.IsOfType<CityTabViewModel>(management.Tabs[0]);

        Assert.That(cityTab.CityImageKey, Is.EqualTo(new AssetSpriteKey.CityImage(3)));
        Assert.That(cityTab.HasStack, Is.False);

        cityTab.LeaderBlankslate.HireCommand.Execute(null);

        LeaderPurchaseViewModel purchase = Assert.IsOfType<LeaderPurchaseViewModel>(gameViewModel.TopScreen);

        Assert.That(FormatOptions(purchase.Options), Is.EqualTo(["leader | name | description | Cost: 100 gold | enabled"]));

        await purchase.Options[0].PurchaseCommand.ExecuteAsync(null);

        WorldStack purchasedStack = TestHelpers.SimpleStack(
                new WorldStackId("Stack0001"),
                player.Id,
                city.Entrance,
                TestHelpers.StackUnitGrid(
                    engine.Definitions,
                    [(new WorldUnit(new WorldUnitId("Unit0001"), leaderDefinition.Id, 100, 0, leaderDefinition.Level, [], Name: "Konrad", IsTypeLocked: false), GridPosition.FrontCenter)]),
                movementPoints: 20) with
            {
                SubraceId = subrace.Id
            };

        Assert.That(session.World.State.Stacks.Values, Is.EqualTo([purchasedStack]));
        Assert.That(session.World.State.Players[player.Id].Resources, Is.EqualTo(new WorldResourceSet(50, 0, 0, 0, 0, 0)));
        Assert.That(gameViewModel.TopScreen, Is.Null);
        Assert.That(cityTab.HasStack, Is.True);
    }
}
