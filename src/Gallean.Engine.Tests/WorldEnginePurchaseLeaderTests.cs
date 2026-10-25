using System.Collections.Immutable;

namespace Gallean.Engine.Tests;

public sealed class WorldEnginePurchaseLeaderTests
{
    [Test]
    public void SuccessTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^...",
            "^^^...",
            "^^^..."
        ]);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 150, initiative: 50, damage: 10),
                movementPoints: 20, leadership: 5) with
            {
                Race = WorldRace.Undead,
                PurchaseCost = new WorldResourceSet(100, 0, 0, 0, 0, 0)
            };

        WorldRaceDefinition raceDefinition = new(
            Race: WorldRace.Undead,
            NameTextId: new GlobalTextId("Undead"),
            Income: WorldResourceSet.Empty,
            NativeTerrain: WorldMapTerrain.Undead,
            IsPlayable: true,
            CapitalRegeneration: 35,
            CapitalProtectionModifierId: null,
            Cities: [],
            Leaders: [leaderDefinition.Id],
            Units: [],
            MaleLeaderNames: ["Ardobert"],
            FemaleLeaderNames: ["Zolian"]);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(150, 0, 0, 0, 0, 0)
        };

        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("cap"),
            player.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldSubrace subrace = new(
            Id: new WorldSubraceId("Subrace1"),
            PlayerId: player.Id,
            Subrace: WorldSubraceType.Undead,
            BannerVariant: new WorldObjectVariant(1));

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithRace(raceDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCapital(capital)
            .WithSubrace(subrace)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        WorldStack purchasedStack = TestHelpers.SimpleStack(
                new WorldStackId("Stack0001"),
                player.Id,
                new WorldPosition(4, 4),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("Unit0001"), leaderDefinition) with { Name = "Ardobert" }, GridPosition.FrontCenter, leaderDefinition.IsLarge)
                    .Build(),
                movementPoints: 20) with
            {
                SubraceId = subrace.Id
            };

        engine.CheckCommand(
            new WorldPurchaseLeaderCommand(capital.Id, leaderDefinition.Id),
            [new WorldStackCreatedEvent(purchasedStack, new WorldResourceSet(100, 0, 0, 0, 0, 0))]
        );

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Players = engine.State.Players.SetItem(player.Id, player with
                {
                    Resources = new WorldResourceSet(50, 0, 0, 0, 0, 0)
                }),
                Stacks = engine.State.Stacks.SetItem(purchasedStack.Id, purchasedStack)
            }));
    }

    [Test]
    public void UniqueStackIdTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^...",
            "^^^...",
            "^^^..."
        ]);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 150, initiative: 50, damage: 10),
                movementPoints: 20, leadership: 5) with
            {
                Race = WorldRace.Undead,
                PurchaseCost = new WorldResourceSet(100, 0, 0, 0, 0, 0)
            };

        WorldRaceDefinition raceDefinition = new(
            Race: WorldRace.Undead,
            NameTextId: new GlobalTextId("Undead"),
            Income: WorldResourceSet.Empty,
            NativeTerrain: WorldMapTerrain.Undead,
            IsPlayable: true,
            CapitalRegeneration: 35,
            CapitalProtectionModifierId: null,
            Cities: [],
            Leaders: [leaderDefinition.Id],
            Units: [],
            MaleLeaderNames: ["Ardobert"],
            FemaleLeaderNames: ["Zolian"]);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0)
        };

        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("cap"),
            player.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldStack existingStack = TestHelpers.SimpleStack(
            new WorldStackId("Stack0001"),
            player.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("Unit0001"), leaderDefinition) with { Name = "Ardobert" }, GridPosition.FrontBottom, leaderDefinition.IsLarge)
                .Build(),
            movementPoints: 20);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithRace(raceDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCapital(capital)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(existingStack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        WorldStack purchasedStack = TestHelpers.SimpleStack(
            new WorldStackId("Stack0002"),
            player.Id,
            new WorldPosition(4, 4),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("Unit0002"), leaderDefinition) with { Name = "Ardobert" }, GridPosition.FrontCenter, leaderDefinition.IsLarge)
                .Build(),
            movementPoints: 20);

        engine.CheckCommand(
            new WorldPurchaseLeaderCommand(capital.Id, leaderDefinition.Id),
            [new WorldStackCreatedEvent(purchasedStack, new WorldResourceSet(100, 0, 0, 0, 0, 0))]
        );

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Players = engine.State.Players.SetItem(player.Id, player with
                {
                    Resources = new WorldResourceSet(0, 0, 0, 0, 0, 0)
                }),
                Stacks = engine.State.Stacks.SetItem(purchasedStack.Id, purchasedStack)
            }));
    }

    [Test]
    public void TargetOccupiedTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^...",
            "^^^...",
            "^^^..."
        ]);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 150, initiative: 50, damage: 10),
                movementPoints: 20, leadership: 5) with
            {
                Race = WorldRace.Undead
            };

        WorldRaceDefinition raceDefinition = new(
            Race: WorldRace.Undead,
            NameTextId: new GlobalTextId("Undead"),
            Income: WorldResourceSet.Empty,
            NativeTerrain: WorldMapTerrain.Undead,
            IsPlayable: true,
            CapitalRegeneration: 35,
            CapitalProtectionModifierId: null,
            Cities: [],
            Leaders: [leaderDefinition.Id],
            Units: [],
            MaleLeaderNames: ["Ardobert"],
            FemaleLeaderNames: ["Zolian"]);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0)
        };

        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("cap"),
            player.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldStack visitingStack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(4, 4),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition) with { Name = "Ardobert" }, GridPosition.FrontBottom, leaderDefinition.IsLarge)
                .Build(),
            movementPoints: 20);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithRace(raceDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCapital(capital)
            .WithStack(visitingStack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(
            new WorldPurchaseLeaderCommand(capital.Id, leaderDefinition.Id),
            WorldCommandError.TargetOccupied
        );
    }

    [Test]
    public void FortificationNotFoundTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 150, initiative: 50, damage: 10),
                movementPoints: 20, leadership: 5) with
            {
                Race = WorldRace.Undead
            };

        WorldRaceDefinition raceDefinition = new(
            Race: WorldRace.Undead,
            NameTextId: new GlobalTextId("Undead"),
            Income: WorldResourceSet.Empty,
            NativeTerrain: WorldMapTerrain.Undead,
            IsPlayable: true,
            CapitalRegeneration: 35,
            CapitalProtectionModifierId: null,
            Cities: [],
            Leaders: [leaderDefinition.Id],
            Units: [],
            MaleLeaderNames: ["Ardobert"],
            FemaleLeaderNames: ["Zolian"]);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0)
        };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithRace(raceDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(
            new WorldPurchaseLeaderCommand(new WorldFortificationId("missing"), leaderDefinition.Id),
            WorldCommandError.FortificationNotFound
        );
    }

    [Test]
    public void OtherPlayerCapitalTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 5);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 150, initiative: 50, damage: 10),
                movementPoints: 20, leadership: 5) with
            {
                Race = WorldRace.Undead
            };

        WorldRaceDefinition raceDefinition = new(
            Race: WorldRace.Undead,
            NameTextId: new GlobalTextId("Undead"),
            Income: WorldResourceSet.Empty,
            NativeTerrain: WorldMapTerrain.Undead,
            IsPlayable: true,
            CapitalRegeneration: 35,
            CapitalProtectionModifierId: null,
            Cities: [],
            Leaders: [leaderDefinition.Id],
            Units: [],
            MaleLeaderNames: ["Ardobert"],
            FemaleLeaderNames: ["Zolian"]);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0)
        };

        WorldPlayer other = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Undead, map);

        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("capital"),
            other.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithRace(raceDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(other.LordId, other.Race, LordCategory.Warrior))
            .WithPlayer(other)
            .WithCapital(capital)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(
            new WorldPurchaseLeaderCommand(capital.Id, leaderDefinition.Id),
            WorldCommandError.FortificationNotFound
        );
    }

    [Test]
    public void UnitNotAvailableTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^",
            "^^^^^",
            "^^^^^",
            "^^^^^",
            "^^^^."
        ]);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 150, initiative: 50, damage: 10),
                movementPoints: 20, leadership: 5) with
            {
                Race = WorldRace.Undead
            };

        WorldRaceDefinition raceDefinition = new(
            Race: WorldRace.Undead,
            NameTextId: new GlobalTextId("Undead"),
            Income: WorldResourceSet.Empty,
            NativeTerrain: WorldMapTerrain.Undead,
            IsPlayable: true,
            CapitalRegeneration: 35,
            CapitalProtectionModifierId: null,
            Cities: [],
            Leaders: [],
            Units: [leaderDefinition.Id],
            MaleLeaderNames: ["Ardobert"],
            FemaleLeaderNames: ["Zolian"]);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0)
        };

        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("cap"),
            player.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithRace(raceDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCapital(capital)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(
            new WorldPurchaseLeaderCommand(capital.Id, leaderDefinition.Id),
            WorldCommandError.UnitNotAvailable
        );
    }

    [Test]
    public void InsufficientResourcesTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^",
            "^^^^^",
            "^^^^^",
            "^^^^^",
            "^^^^."
        ]);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 150, initiative: 50, damage: 10),
                movementPoints: 20, leadership: 5) with
            {
                Race = WorldRace.Undead,
                PurchaseCost = new WorldResourceSet(150, 0, 0, 0, 0, 0)
            };

        WorldRaceDefinition raceDefinition = new(
            Race: WorldRace.Undead,
            NameTextId: new GlobalTextId("Undead"),
            Income: WorldResourceSet.Empty,
            NativeTerrain: WorldMapTerrain.Undead,
            IsPlayable: true,
            CapitalRegeneration: 35,
            CapitalProtectionModifierId: null,
            Cities: [],
            Leaders: [leaderDefinition.Id],
            Units: [],
            MaleLeaderNames: ["Ardobert"],
            FemaleLeaderNames: ["Zolian"]);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0)
        };

        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("cap"),
            player.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithRace(raceDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCapital(capital)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(
            new WorldPurchaseLeaderCommand(capital.Id, leaderDefinition.Id),
            WorldCommandError.InsufficientResources
        );
    }

    [Test]
    public void MissingPrerequisiteTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^",
            "^^^^^",
            "^^^^^",
            "^^^^^",
            "^^^^."
        ]);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 150, initiative: 50, damage: 10),
                movementPoints: 20, leadership: 5) with
            {
                Race = WorldRace.Undead,
                RequiredBuildingId = new WorldPlayerBuildingDefinitionId("building")
            };

        WorldRaceDefinition raceDefinition = new(
            Race: WorldRace.Undead,
            NameTextId: new GlobalTextId("Undead"),
            Income: WorldResourceSet.Empty,
            NativeTerrain: WorldMapTerrain.Undead,
            IsPlayable: true,
            CapitalRegeneration: 35,
            CapitalProtectionModifierId: null,
            Cities: [],
            Leaders: [leaderDefinition.Id],
            Units: [],
            MaleLeaderNames: ["Ardobert"],
            FemaleLeaderNames: ["Zolian"]);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0)
        };

        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("cap"),
            player.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithRace(raceDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCapital(capital)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(
            new WorldPurchaseLeaderCommand(capital.Id, leaderDefinition.Id),
            WorldCommandError.UnitMissingPrerequisite
        );
    }

    [Test]
    public void LeadershipExceededTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^",
            "^^^^^",
            "^^^^^",
            "^^^^^",
            "^^^^."
        ]);

        UnitDefinition leaderDefinition = TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 150, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Leader,
            Race = WorldRace.Undead,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Leadership = null,
            MovementPoints = 20
        };

        WorldRaceDefinition raceDefinition = new(
            Race: WorldRace.Undead,
            NameTextId: new GlobalTextId("Undead"),
            Income: WorldResourceSet.Empty,
            NativeTerrain: WorldMapTerrain.Undead,
            IsPlayable: true,
            CapitalRegeneration: 35,
            CapitalProtectionModifierId: null,
            Cities: [],
            Leaders: [leaderDefinition.Id],
            Units: [],
            MaleLeaderNames: ["Ardobert"],
            FemaleLeaderNames: ["Zolian"]);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0)
        };

        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("cap"),
            player.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithRace(raceDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCapital(capital)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(
            new WorldPurchaseLeaderCommand(capital.Id, leaderDefinition.Id),
            WorldCommandError.LeadershipExceeded
        );
    }

    [Test]
    public void CityEntranceTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 150, initiative: 50, damage: 10),
                movementPoints: 20, leadership: 5) with
            {
                Race = WorldRace.Undead,
                PurchaseCost = new WorldResourceSet(100, 0, 0, 0, 0, 0)
            };

        WorldRaceDefinition raceDefinition = new(
            Race: WorldRace.Undead,
            NameTextId: new GlobalTextId("Undead"),
            Income: WorldResourceSet.Empty,
            NativeTerrain: WorldMapTerrain.Undead,
            IsPlayable: true,
            CapitalRegeneration: 35,
            CapitalProtectionModifierId: null,
            Cities: [],
            Leaders: [leaderDefinition.Id],
            Units: [],
            MaleLeaderNames: ["Ardobert"],
            FemaleLeaderNames: ["Zolian"]);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(150, 0, 0, 0, 0, 0)
        };

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Undead, 2),
            new WorldResourceSet(150, 0, 0, 0, 0, 0));

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            player.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldSubrace subrace = new(
            Id: new WorldSubraceId("Subrace1"),
            PlayerId: player.Id,
            Subrace: WorldSubraceType.Undead,
            BannerVariant: new WorldObjectVariant(1));

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(cityDefinition)
            .WithRace(raceDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCity(city)
            .WithSubrace(subrace)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        WorldStack purchasedStack = TestHelpers.SimpleStack(
                new WorldStackId("Stack0001"),
                player.Id,
                new WorldPosition(4, 4),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("Unit0001"), leaderDefinition) with { Name = "Ardobert" }, GridPosition.FrontCenter, leaderDefinition.IsLarge)
                    .Build(),
                movementPoints: 20) with
            {
                SubraceId = subrace.Id
            };

        engine.CheckCommand(
            new WorldPurchaseLeaderCommand(city.Id, leaderDefinition.Id),
            [new WorldStackCreatedEvent(purchasedStack, new WorldResourceSet(100, 0, 0, 0, 0, 0))]
        );

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Players = engine.State.Players.SetItem(player.Id, player with
                {
                    Resources = new WorldResourceSet(50, 0, 0, 0, 0, 0)
                }),
                Stacks = engine.State.Stacks.SetItem(purchasedStack.Id, purchasedStack)
            }));
    }

    [Test]
    public void RiotingCityTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 150, initiative: 50, damage: 10),
                movementPoints: 20, leadership: 5) with
            {
                Race = WorldRace.Undead,
                PurchaseCost = new WorldResourceSet(100, 0, 0, 0, 0, 0)
            };

        WorldRaceDefinition raceDefinition = new(
            Race: WorldRace.Undead,
            NameTextId: new GlobalTextId("Undead"),
            Income: WorldResourceSet.Empty,
            NativeTerrain: WorldMapTerrain.Undead,
            IsPlayable: true,
            CapitalRegeneration: 35,
            CapitalProtectionModifierId: null,
            Cities: [],
            Leaders: [leaderDefinition.Id],
            Units: [],
            MaleLeaderNames: ["Ardobert"],
            FemaleLeaderNames: ["Zolian"]);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(150, 0, 0, 0, 0, 0)
        };

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Undead, 2),
            new WorldResourceSet(150, 0, 0, 0, 0, 0));

        WorldCity city = TestHelpers.SimpleCity(
                new WorldFortificationId("city"),
                cityDefinition,
                player.Id,
                new WorldPosition(4, 4),
                UnitGrid<WorldUnit>.Empty) with
            {
                RiotTurns = 3
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(cityDefinition)
            .WithRace(raceDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCity(city)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(
            new WorldPurchaseLeaderCommand(city.Id, leaderDefinition.Id),
            WorldCommandError.CityRioting
        );
    }

    [Test]
    public void DestroyedStackIdTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^...",
            "^^^...",
            "^^^..."
        ]);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 150, initiative: 50, damage: 10),
                movementPoints: 20, leadership: 5) with
            {
                Race = WorldRace.Undead,
                PurchaseCost = new WorldResourceSet(100, 0, 0, 0, 0, 0)
            };

        WorldRaceDefinition raceDefinition = new(
            Race: WorldRace.Undead,
            NameTextId: new GlobalTextId("Undead"),
            Income: WorldResourceSet.Empty,
            NativeTerrain: WorldMapTerrain.Undead,
            IsPlayable: true,
            CapitalRegeneration: 35,
            CapitalProtectionModifierId: null,
            Cities: [],
            Leaders: [leaderDefinition.Id],
            Units: [],
            MaleLeaderNames: ["Ardobert"],
            FemaleLeaderNames: ["Zolian"]);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0)
        };

        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("cap"),
            player.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldStack expiringStack = TestHelpers.SimpleStack(
                new WorldStackId("Stack0001"),
                player.Id,
                new WorldPosition(3, 3),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("Unit0001"), leaderDefinition) with { Name = "Ardobert" }, GridPosition.FrontBottom, leaderDefinition.IsLarge)
                    .Build(),
                movementPoints: 20) with
            {
                ExpirationTurn = 1
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithRace(raceDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCapital(capital)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(expiringStack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        WorldStack purchasedStack = TestHelpers.SimpleStack(
            new WorldStackId("Stack0002"),
            player.Id,
            new WorldPosition(4, 4),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("Unit0001"), leaderDefinition) with { Name = "Ardobert" }, GridPosition.FrontCenter, leaderDefinition.IsLarge)
                .Build(),
            movementPoints: 20);

        engine.CheckCommand(
            new WorldPurchaseLeaderCommand(capital.Id, leaderDefinition.Id),
            [new WorldStackCreatedEvent(purchasedStack, new WorldResourceSet(100, 0, 0, 0, 0, 0))]
        );

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Players = engine.State.Players.SetItem(player.Id, player with
                {
                    Resources = new WorldResourceSet(0, 0, 0, 0, 0, 0)
                }),
                Stacks = ImmutableSortedDictionary<WorldStackId, WorldStack>.Empty.Add(purchasedStack.Id, purchasedStack),
                ScenarioState = WorldScenarioState.Empty with
                {
                    DestroyedStacks = [new ScenarioDestroyedStack(expiringStack.Id, DestroyerPlayerId: null, SourceTemplateId: null)]
                }
            }));
    }

    [Test]
    public void GuildBuildingRequiredTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^",
            "^^^^^",
            "^^^^^",
            "^^^^^",
            "^^^^."
        ]);

        UnitDefinition thiefDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("thief", maxHitpoints: 150, initiative: 50, damage: 10),
                movementPoints: 20, leadership: 1) with
            {
                Race = WorldRace.Undead,
                Category = UnitCategory.Thief,
                LeaderCategory = UnitLeaderCategory.Thief
            };

        WorldPlayerBuildingDefinition healing = TestHelpers.SimpleBuildingDefinition(new WorldPlayerBuildingDefinitionId("healing"), WorldPlayerBuildingCategory.Heal);

        WorldRaceDefinition raceDefinition = new(
            Race: WorldRace.Undead,
            NameTextId: new GlobalTextId("Undead"),
            Income: WorldResourceSet.Empty,
            NativeTerrain: WorldMapTerrain.Undead,
            IsPlayable: true,
            CapitalRegeneration: 35,
            CapitalProtectionModifierId: null,
            Cities: [],
            Leaders: [thiefDefinition.Id],
            Units: [],
            MaleLeaderNames: ["Ardobert"],
            FemaleLeaderNames: ["Zolian"]);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0),
            Buildings = new WorldPlayerBuildings([healing.Id], [healing.Id])
        };

        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("cap"),
            player.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thiefDefinition)
            .WithDefinition(healing)
            .WithRace(raceDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCapital(capital)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(
            new WorldPurchaseLeaderCommand(capital.Id, thiefDefinition.Id),
            WorldCommandError.GuildBuildingRequired
        );
    }

    [Test]
    public void GuildTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^...",
            "^^^...",
            "^^^..."
        ]);

        UnitDefinition thiefDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("thief", maxHitpoints: 150, initiative: 50, damage: 10),
                movementPoints: 20, leadership: 1) with
            {
                Race = WorldRace.Undead,
                Category = UnitCategory.Thief,
                LeaderCategory = UnitLeaderCategory.Thief,
                PurchaseCost = new WorldResourceSet(100, 0, 0, 0, 0, 0)
            };

        WorldPlayerBuildingDefinition guild = TestHelpers.SimpleBuildingDefinition(new WorldPlayerBuildingDefinitionId("guild"), WorldPlayerBuildingCategory.Guild);

        WorldRaceDefinition raceDefinition = new(
            Race: WorldRace.Undead,
            NameTextId: new GlobalTextId("Undead"),
            Income: WorldResourceSet.Empty,
            NativeTerrain: WorldMapTerrain.Undead,
            IsPlayable: true,
            CapitalRegeneration: 35,
            CapitalProtectionModifierId: null,
            Cities: [],
            Leaders: [thiefDefinition.Id],
            Units: [],
            MaleLeaderNames: ["Ardobert"],
            FemaleLeaderNames: ["Zolian"]);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(150, 0, 0, 0, 0, 0),
            Buildings = new WorldPlayerBuildings([guild.Id], [guild.Id])
        };

        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("cap"),
            player.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldSubrace subrace = new(
            Id: new WorldSubraceId("Subrace1"),
            PlayerId: player.Id,
            Subrace: WorldSubraceType.Undead,
            BannerVariant: new WorldObjectVariant(1));

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thiefDefinition)
            .WithDefinition(guild)
            .WithRace(raceDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCapital(capital)
            .WithSubrace(subrace)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        WorldStack purchasedStack = TestHelpers.SimpleStack(
                new WorldStackId("Stack0001"),
                player.Id,
                new WorldPosition(4, 4),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("Unit0001"), thiefDefinition), GridPosition.FrontCenter, thiefDefinition.IsLarge)
                    .Build(),
                movementPoints: 20) with
            {
                SubraceId = subrace.Id
            };

        engine.CheckCommand(
            new WorldPurchaseLeaderCommand(capital.Id, thiefDefinition.Id),
            [new WorldStackCreatedEvent(purchasedStack, new WorldResourceSet(100, 0, 0, 0, 0, 0))]
        );
    }
}
