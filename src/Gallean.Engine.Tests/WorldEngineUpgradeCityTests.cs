namespace Gallean.Engine.Tests;

public sealed class WorldEngineUpgradeCityTests
{
    [Test]
    public void UpgradeCityTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 5);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(400, 0, 0, 0, 0, 0)
        };

        WorldCityDefinition level1 = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Undead, 1),
            new WorldResourceSet(150, 0, 0, 0, 0, 0));

        WorldCityDefinition level2 = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Undead, 2),
            new WorldResourceSet(250, 0, 0, 0, 0, 0));

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            level1,
            player.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(level1)
            .WithDefinition(level2)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCity(city)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(new WorldUpgradeCityCommand(city.Id), [
            new WorldCityUpgradedEvent(city.Id, level2.Id, new WorldResourceSet(150, 0, 0, 0, 0, 0))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Players = engine.State.Players.SetItem(player.Id, player with
                {
                    Resources = new WorldResourceSet(350, 25, 0, 0, 0, 0)
                }),
                Fortifications = engine.State.Fortifications.SetItem(city.Id, city with
                {
                    DefinitionId = level2.Id,
                    GrowthTurn = 1
                })
            }));
    }

    [Test]
    public void UpgradeCityLevelPriceTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 5);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(1000, 0, 0, 0, 0, 0)
        };

        WorldCityDefinition level3 = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Undead, 3),
            new WorldResourceSet(500, 0, 0, 0, 0, 0));

        WorldCityDefinition level4 = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Undead, 4),
            new WorldResourceSet(750, 0, 0, 0, 0, 0));

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            level3,
            player.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(level3)
            .WithDefinition(level4)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCity(city)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(new WorldUpgradeCityCommand(city.Id), [
            new WorldCityUpgradedEvent(city.Id, level4.Id, new WorldResourceSet(500, 0, 0, 0, 0, 0))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Players = engine.State.Players.SetItem(player.Id, player with
                {
                    Resources = new WorldResourceSet(600, 25, 0, 0, 0, 0)
                }),
                Fortifications = engine.State.Fortifications.SetItem(city.Id, city with
                {
                    DefinitionId = level4.Id,
                    GrowthTurn = 1
                })
            }));
    }

    [Test]
    public void UpgradeCityWithoutGoldTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 5);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldCityDefinition level1 = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Undead, 1),
            new WorldResourceSet(150, 0, 0, 0, 0, 0));

        WorldCityDefinition level2 = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Undead, 2),
            new WorldResourceSet(250, 0, 0, 0, 0, 0));

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            level1,
            player.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(level1)
            .WithDefinition(level2)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCity(city)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(new WorldUpgradeCityCommand(city.Id), WorldCommandError.InsufficientResources);
    }

    [Test]
    public void UpgradeCityAtScenarioMaxLevelTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 5);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(1000, 0, 0, 0, 0, 0)
        };

        WorldCityDefinition level2 = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Undead, 2),
            new WorldResourceSet(250, 0, 0, 0, 0, 0));

        WorldCityDefinition level3 = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Undead, 3),
            new WorldResourceSet(500, 0, 0, 0, 0, 0));

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            level2,
            player.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithMaxCityLevel(2)
            .WithDefinition(level2)
            .WithDefinition(level3)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCity(city)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(new WorldUpgradeCityCommand(city.Id), WorldCommandError.CityFullyUpgraded);
    }

    [Test]
    public void UpgradeCityAtTopLevelTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 5);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(1000, 0, 0, 0, 0, 0)
        };

        WorldCityDefinition level2 = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Undead, 2),
            new WorldResourceSet(250, 0, 0, 0, 0, 0));

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            level2,
            player.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(level2)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCity(city)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(new WorldUpgradeCityCommand(city.Id), WorldCommandError.CityFullyUpgraded);
    }

    [Test]
    public void UpgradeCityOfAnotherPlayerTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 5);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(1000, 0, 0, 0, 0, 0)
        };

        WorldPlayer other = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldCityDefinition level1 = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Undead, 1),
            new WorldResourceSet(150, 0, 0, 0, 0, 0));

        WorldCityDefinition level2 = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Undead, 2),
            new WorldResourceSet(250, 0, 0, 0, 0, 0));

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            level1,
            other.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(level1)
            .WithDefinition(level2)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(other.LordId, other.Race, LordCategory.Warrior))
            .WithPlayer(other)
            .WithCity(city)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(new WorldUpgradeCityCommand(city.Id), WorldCommandError.FortificationNotFound);
    }

    [Test]
    public void UpgradeCapitalTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 5);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(1000, 0, 0, 0, 0, 0)
        };

        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("capital"),
            player.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCapital(capital)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(new WorldUpgradeCityCommand(capital.Id), WorldCommandError.FortificationNotFound);
    }

    [Test]
    public void GuildmasterGrowthDiscountTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 5);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(400, 0, 0, 0, 0, 0)
        };

        WorldCityDefinition level1 = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Undead, 1),
            new WorldResourceSet(150, 0, 0, 0, 0, 0));

        WorldCityDefinition level2 = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Undead, 2),
            new WorldResourceSet(250, 0, 0, 0, 0, 0));

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            level1,
            player.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(level1)
            .WithDefinition(level2)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Guildmaster))
            .WithPlayer(player)
            .WithCity(city)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(new WorldUpgradeCityCommand(city.Id), [
            new WorldCityUpgradedEvent(city.Id, level2.Id, new WorldResourceSet(75, 0, 0, 0, 0, 0))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Players = engine.State.Players.SetItem(player.Id, player with
                {
                    Resources = new WorldResourceSet(425, 25, 0, 0, 0, 0)
                }),
                Fortifications = engine.State.Fortifications.SetItem(city.Id, city with
                {
                    DefinitionId = level2.Id,
                    GrowthTurn = 1
                })
            }));
    }

    [Test]
    public void GuildmasterAffordsWhatOtherLordsCannotTest()
    {
        WorldMap map = TestHelpers.SimpleMap(10, 10);

        WorldPlayer warrior = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer guildmaster = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Undead, map);

        WorldCityDefinition level1 = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Undead, 1),
            new WorldResourceSet(150, 0, 0, 0, 0, 0));

        WorldCityDefinition level2 = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Undead, 2),
            new WorldResourceSet(250, 0, 0, 0, 0, 0));

        WorldCity warriorCity = TestHelpers.SimpleCity(
            new WorldFortificationId("warrior-city"),
            level1,
            warrior.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldCity guildmasterCity = TestHelpers.SimpleCity(
            new WorldFortificationId("guildmaster-city"),
            level1,
            guildmaster.Id,
            new WorldPosition(9, 9),
            UnitGrid<WorldUnit>.Empty);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(level1)
            .WithDefinition(level2)
            .WithDefinition(TestHelpers.SimpleLord(warrior.LordId, warrior.Race, LordCategory.Warrior))
            .WithPlayer(warrior)
            .WithDefinition(TestHelpers.SimpleLord(guildmaster.LordId, guildmaster.Race, LordCategory.Guildmaster))
            .WithPlayer(guildmaster)
            .WithCity(warriorCity)
            .WithCity(guildmasterCity)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(new WorldUpgradeCityCommand(warriorCity.Id), WorldCommandError.InsufficientResources);

        _ = engine.DispatchCommand(new WorldEndTurnCommand());

        engine.CheckCommand(new WorldUpgradeCityCommand(guildmasterCity.Id), [
            new WorldCityUpgradedEvent(guildmasterCity.Id, level2.Id, new WorldResourceSet(75, 0, 0, 0, 0, 0))
        ]);
    }

    [Test]
    public void GrowthInfoTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 5);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(400, 0, 0, 0, 0, 0)
        };

        WorldCityDefinition level1 = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Undead, 1),
            new WorldResourceSet(150, 0, 0, 0, 0, 0));

        WorldCityDefinition level2 = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Undead, 2),
            new WorldResourceSet(250, 0, 0, 0, 0, 0));

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            level1,
            player.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(level1)
            .WithDefinition(level2)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Guildmaster))
            .WithPlayer(player)
            .WithCity(city)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        WorldGetCityInfoCommand.Result result = engine.CheckCommand(
            new WorldGetCityInfoCommand(city.Id),
            []);

        Assert.That(result, Is.EqualTo(
            new WorldGetCityInfoCommand.Result(
                new WorldGetCityInfoCommand.Growth(new WorldResourceSet(75, 0, 0, 0, 0, 0), Error: null),
                WorldCommandError.HealBuildingRequired)));
    }

    [Test]
    public void GrowthInfoUnaffordableTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 5);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(10, 0, 0, 0, 0, 0)
        };

        WorldCityDefinition level1 = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Undead, 1),
            new WorldResourceSet(150, 0, 0, 0, 0, 0));

        WorldCityDefinition level2 = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Undead, 2),
            new WorldResourceSet(250, 0, 0, 0, 0, 0));

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            level1,
            player.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(level1)
            .WithDefinition(level2)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCity(city)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        WorldGetCityInfoCommand.Result result = engine.CheckCommand(
            new WorldGetCityInfoCommand(city.Id),
            []);

        Assert.That(result, Is.EqualTo(new WorldGetCityInfoCommand.Result(
            new WorldGetCityInfoCommand.Growth(
                new WorldResourceSet(150, 0, 0, 0, 0, 0),
                WorldCommandError.InsufficientResources),
            WorldCommandError.HealBuildingRequired)));
    }

    [Test]
    public void GrowthInfoAtCapitalTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 5);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("capital"),
            player.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCapital(capital)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        WorldGetCityInfoCommand.Result result = engine.CheckCommand(
            new WorldGetCityInfoCommand(capital.Id),
            []);

        Assert.That(result, Is.EqualTo(new WorldGetCityInfoCommand.Result(Growth: null, WorldCommandError.HealBuildingRequired)));
    }

    [Test]
    public void GrowthInfoOfAnotherPlayerTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 5);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(1000, 0, 0, 0, 0, 0)
        };

        WorldPlayer other = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldCityDefinition level1 = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Undead, 1),
            new WorldResourceSet(150, 0, 0, 0, 0, 0));

        WorldCityDefinition level2 = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Undead, 2),
            new WorldResourceSet(250, 0, 0, 0, 0, 0));

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            level1,
            other.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(level1)
            .WithDefinition(level2)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(other.LordId, other.Race, LordCategory.Warrior))
            .WithPlayer(other)
            .WithCity(city)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        WorldGetCityInfoCommand.Result result = engine.CheckCommand(
            new WorldGetCityInfoCommand(city.Id),
            []);

        Assert.That(result, Is.EqualTo(new WorldGetCityInfoCommand.Result(Growth: null, WorldCommandError.GarrisonNotFound)));
    }

    [Test]
    public void UpgradeCityTwiceInOneTurnTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 5);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(1000, 0, 0, 0, 0, 0)
        };

        WorldCityDefinition level1 = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Undead, 1),
            new WorldResourceSet(150, 0, 0, 0, 0, 0));

        WorldCityDefinition level2 = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Undead, 2),
            new WorldResourceSet(250, 0, 0, 0, 0, 0));

        WorldCityDefinition level3 = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Undead, 3),
            new WorldResourceSet(500, 0, 0, 0, 0, 0));

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            level1,
            player.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(level1)
            .WithDefinition(level2)
            .WithDefinition(level3)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCity(city)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());
        _ = engine.DispatchCommand(new WorldUpgradeCityCommand(city.Id));

        engine.CheckCommandError(new WorldUpgradeCityCommand(city.Id), WorldCommandError.CityAlreadyGrownThisTurn);

        _ = engine.DispatchCommand(new WorldEndTurnCommand());

        engine.CheckCommand(new WorldUpgradeCityCommand(city.Id), [
            new WorldCityUpgradedEvent(city.Id, level3.Id, new WorldResourceSet(250, 0, 0, 0, 0, 0))
        ]);
    }

    [Test]
    public void UpgradeCityGrownTodayTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 5);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(1000, 0, 0, 0, 0, 0)
        };

        WorldCityDefinition level1 = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Undead, 1),
            new WorldResourceSet(150, 0, 0, 0, 0, 0));

        WorldCityDefinition level2 = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Undead, 2),
            new WorldResourceSet(250, 0, 0, 0, 0, 0));

        WorldCity city = TestHelpers.SimpleCity(
                new WorldFortificationId("city"),
                level1,
                player.Id,
                new WorldPosition(4, 4),
                UnitGrid<WorldUnit>.Empty) with
            {
                GrowthTurn = 1
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(level1)
            .WithDefinition(level2)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCity(city)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(new WorldUpgradeCityCommand(city.Id), WorldCommandError.CityAlreadyGrownThisTurn);
    }
}
