namespace Gallean.Engine.Tests;

public sealed class WorldEngineGetGarrisonInfoTests
{
    [Test]
    public void UnitsTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^^..",
            "^^^^.."
        ]);

        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 10) with
        {
            HealCost = new WorldResourceSet(1, 0, 0, 0, 0, 0),
            ReviveCost = new WorldResourceSet(50, 0, 0, 0, 0, 0)
        };

        WorldPlayerBuildingDefinition temple = new(
            Id: new WorldPlayerBuildingDefinitionId("temple"),
            NameTextId: new GlobalTextId("name"),
            DescriptionTextId: new GlobalTextId("description"),
            RequiredId: null,
            Category: WorldPlayerBuildingCategory.Heal,
            UnitBranch: null,
            Level: 1,
            Cost: WorldResourceSet.Empty,
            UnitIds: []);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0),
            Buildings = new WorldPlayerBuildings([temple.Id], [temple.Id]),
            Spellbook = new WorldPlayerSpellbook([], [])
        };

        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("cap"),
            player.Id,
            new WorldPosition(4, 4),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition) with { Hitpoints = 30 }, GridPosition.FrontBottom, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier2"), soldierDefinition) with { Hitpoints = 0 }, GridPosition.FrontCenter, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier3"), soldierDefinition), GridPosition.BackBottom, false)
                .Build());

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(soldierDefinition)
            .WithDefinition(temple)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCapital(capital)
            .Build();

        WorldGetGarrisonInfoCommand.Result result = engine.CheckCommand(
            new WorldGetGarrisonInfoCommand(capital.Id),
            []);

        Assert.That(result, Is.EqualTo(new WorldGetGarrisonInfoCommand.Result(
            [
                new WorldUnitActionInfo(
                    new WorldUnitId("soldier"),
                    HealError: null,
                    ResurrectError: WorldCommandError.UnitNotDead,
                    DisbandError: null,
                    LockTypeError: WorldCommandError.UnitTypeLockForbidden),
                new WorldUnitActionInfo(
                    new WorldUnitId("soldier2"),
                    HealError: WorldCommandError.UnitDead,
                    ResurrectError: null,
                    DisbandError: null,
                    LockTypeError: WorldCommandError.UnitTypeLockForbidden),
                new WorldUnitActionInfo(
                    new WorldUnitId("soldier3"),
                    HealError: WorldCommandError.UnitNotWounded,
                    ResurrectError: WorldCommandError.UnitNotDead,
                    DisbandError: null,
                    LockTypeError: WorldCommandError.UnitTypeLockForbidden)
            ],
            OccupiedCapacity: 3,
            TotalCapacity: 6)));
    }

    [Test]
    public void OtherPlayerCapitalTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^^..",
            "^^^^.."
        ]);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer otherPlayer = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Human, map);

        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("cap"),
            otherPlayer.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithDefinition(TestHelpers.SimpleLord(otherPlayer.LordId, otherPlayer.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithPlayer(otherPlayer)
            .WithCapital(capital)
            .Build();

        engine.CheckCommandError(new WorldGetGarrisonInfoCommand(capital.Id), WorldCommandError.FortificationNotFound);
    }

    [Test]
    public void CityCapacityTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 5);

        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 10);

        UnitDefinition largeDefinition = TestHelpers.SimpleUnitDefinition("large", maxHitpoints: 200, initiative: 50, damage: 20) with
        {
            IsLarge = true
        };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldCityDefinition level3 = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Undead, 3),
            new WorldResourceSet(150, 0, 0, 0, 0, 0));

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            level3,
            player.Id,
            new WorldPosition(4, 4),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("large"), largeDefinition), GridPosition.FrontCenter, true)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition), GridPosition.FrontBottom, false)
                .Build());

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(soldierDefinition)
            .WithDefinition(largeDefinition)
            .WithDefinition(level3)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCity(city)
            .Build();

        WorldGetGarrisonInfoCommand.Result result = engine.CheckCommand(
            new WorldGetGarrisonInfoCommand(city.Id),
            []);

        Assert.That(result, Is.EqualTo(new WorldGetGarrisonInfoCommand.Result(
            [
                new WorldUnitActionInfo(
                    new WorldUnitId("soldier"),
                    HealError: WorldCommandError.HealBuildingRequired,
                    ResurrectError: WorldCommandError.HealBuildingRequired,
                    DisbandError: null,
                    LockTypeError: WorldCommandError.UnitTypeLockForbidden),
                new WorldUnitActionInfo(
                    new WorldUnitId("large"),
                    HealError: WorldCommandError.HealBuildingRequired,
                    ResurrectError: WorldCommandError.HealBuildingRequired,
                    DisbandError: null,
                    LockTypeError: WorldCommandError.UnitTypeLockForbidden)
            ],
            OccupiedCapacity: 3,
            TotalCapacity: 3)));
    }
}
