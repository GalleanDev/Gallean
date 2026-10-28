using Gallean.Common;
using Gallean.Engine;
using Gallean.Engine.Tests;

namespace Gallean.Client.Tests;

public sealed class WorldFortificationInfoViewModelTests
{
    private static IReadOnlyList<string> FormatLines(IReadOnlyList<TextSpans> lines)
    {
        List<string> rows = [];

        foreach (TextSpans line in lines)
        {
            rows.Add(line.Text);
        }

        return rows;
    }

    [Test]
    public void CapitalTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 5);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        WorldCapital capital = new(
            Id: new WorldFortificationId("capital"),
            NameTextId: new ScenarioTextId("Trent"),
            OwnerId: player.Id,
            Footprint: new WorldFootprint(new WorldPosition(0, 0), 5, 5),
            Garrison: UnitGrid<WorldUnit>.Empty,
            Inventory: WorldInventory.Empty,
            AiPriority: 1,
            CapturedById: null);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCapital(capital)
            .BuildSession();

        WorldFortificationInfoViewModel info = WorldFortificationInfoViewModel.CreateCapital(
            session.World.State,
            player.Id,
            capital,
            null,
            engine.Definitions,
            TestGameTextProvider.Instance,
            engine.UnitStats);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(info.Title, Is.EqualTo("Trent"));
            Assert.That(FormatLines(info.Lines), Is.EqualTo(["City level: Capital", "Owner: Empire", "Hit point regeneration per turn: 35%"]));
            Assert.That(info.VisitingStackTitle, Is.Null);
            Assert.That(info.VisitingStack.Units, Is.Empty);
            Assert.That(info.Note, Is.Null);
            Assert.That(info.Defenses.Units, Is.Empty);
        }
    }

    [Test]
    public void CityTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 4);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Dwarf, map);

        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 35, initiative: 50, damage: 10) with
        {
            NameTextId = new GlobalTextId("Squire")
        };

        WorldUnit soldier = new(new WorldUnitId("soldier"), soldierDefinition.Id, 20, 0, soldierDefinition.Level, [], null, false);

        WorldCity city = new(
            Id: new WorldFortificationId("city"),
            NameTextId: new ScenarioTextId("Hemwyk Court"),
            DefinitionId: new WorldCityDefinitionId(WorldRace.Dwarf, 2),
            OwnerId: player.Id,
            SubraceId: TestHelpers.NeutralSubrace.Id,
            Footprint: new WorldFootprint(new WorldPosition(0, 0), 4, 4),
            Garrison: new UnitGrid<WorldUnit>.Builder().Place(soldier, GridPosition.BackCenter, isLarge: false).Build(),
            Inventory: WorldInventory.Empty,
            AiPriority: 1,
            RiotTurns: 0,
            GrowthTurn: 0);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(soldierDefinition)
            .WithDefinition(TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Dwarf, 2), WorldResourceSet.Empty))
            .WithCity(city)
            .BuildSession();

        WorldFortificationInfoViewModel info = WorldFortificationInfoViewModel.CreateCity(
            session.World.State,
            player.Id,
            city,
            null,
            engine.Definitions,
            TestGameTextProvider.Instance,
            engine.UnitStats);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(info.Title, Is.EqualTo("Hemwyk Court"));
            Assert.That(FormatLines(info.Lines), Is.EqualTo(["City level: 2", "Owner: Mountain Clans", "Hit point regeneration per turn: 15%"]));
            Assert.That(info.VisitingStackTitle, Is.Null);
            Assert.That(info.VisitingStack.Units, Is.Empty);
            Assert.That(info.Note, Is.Null);
            Assert.That(info.Defenses.Units, Is.EqualTo(new List<WorldUnitGroupInfoUnitViewModel>
            {
                new(
                    new AssetSpriteKey.UnitFace(soldierDefinition.Id),
                    "Squire",
                    20,
                    35,
                    IsAlive: true,
                    Row: 1,
                    Column: 1,
                    ColumnSpan: 1,
                    Statuses: [])
            }));
        }
    }

    [Test]
    public void RiotCityTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 5);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Dwarf, map);

        WorldCity city = new(
            Id: new WorldFortificationId("city"),
            NameTextId: new ScenarioTextId("Hemwyk Court"),
            DefinitionId: new WorldCityDefinitionId(WorldRace.Dwarf, 2),
            OwnerId: player.Id,
            SubraceId: TestHelpers.NeutralSubrace.Id,
            Footprint: new WorldFootprint(new WorldPosition(0, 0), 4, 4),
            Garrison: UnitGrid<WorldUnit>.Empty,
            Inventory: WorldInventory.Empty,
            AiPriority: 1,
            RiotTurns: 3,
            GrowthTurn: 0);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Dwarf, 2), WorldResourceSet.Empty))
            .WithCity(city)
            .BuildSession();

        WorldFortificationInfoViewModel info = WorldFortificationInfoViewModel.CreateCity(
            session.World.State,
            player.Id,
            city,
            null,
            engine.Definitions,
            TestGameTextProvider.Instance,
            engine.UnitStats);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(info.Title, Is.EqualTo("Hemwyk Court (RIOT)"));
            Assert.That(FormatLines(info.Lines), Is.EqualTo(["City level: 2", "Owner: Mountain Clans", "Hit point regeneration per turn: 15%"]));
            Assert.That(info.VisitingStackTitle, Is.Null);
            Assert.That(info.VisitingStack.Units, Is.Empty);
            Assert.That(info.Note, Is.Null);
            Assert.That(info.Defenses.Units, Is.Empty);
        }
    }

    [Test]
    public void UnspiedEnemyCityTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 4);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Dwarf, map);

        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 35, initiative: 50, damage: 10) with
        {
            NameTextId = new GlobalTextId("Squire")
        };

        WorldCity city = new(
            Id: new WorldFortificationId("city"),
            NameTextId: new ScenarioTextId("Hemwyk Court"),
            DefinitionId: new WorldCityDefinitionId(WorldRace.Dwarf, 1),
            OwnerId: enemy.Id,
            SubraceId: TestHelpers.NeutralSubrace.Id,
            Footprint: new WorldFootprint(new WorldPosition(0, 0), 4, 4),
            Garrison: TestHelpers.SingleUnitGrid(new WorldUnitId("soldier"), soldierDefinition),
            Inventory: WorldInventory.Empty,
            AiPriority: 1,
            RiotTurns: 0,
            GrowthTurn: 0);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(enemy.LordId, enemy.Race, LordCategory.Warrior))
            .WithPlayer(enemy)
            .WithDefinition(soldierDefinition)
            .WithDefinition(TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Dwarf, 1), WorldResourceSet.Empty))
            .WithCity(city)
            .BuildSession();

        WorldFortificationInfoViewModel info = WorldFortificationInfoViewModel.CreateCity(
            session.World.State,
            player.Id,
            city,
            null,
            engine.Definitions,
            TestGameTextProvider.Instance,
            engine.UnitStats);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(info.Title, Is.EqualTo("Hemwyk Court"));
            Assert.That(FormatLines(info.Lines), Is.EqualTo(["City level: 1", "Owner: Mountain Clans", "Hit point regeneration per turn: 10%"]));
            Assert.That(info.VisitingStackTitle, Is.Null);
            Assert.That(info.VisitingStack.Units, Is.Empty);
            Assert.That(info.Note, Is.EqualTo("A thief must spy on this city to gain information about it."));
            Assert.That(info.Defenses.Units, Is.Empty);
        }
    }

    [Test]
    public void SpiedEnemyCityTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 4);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map) with
        {
            SpiedPlayers = [new WorldPlayerId(2)]
        };
        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Dwarf, map);

        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 35, initiative: 50, damage: 10) with
        {
            NameTextId = new GlobalTextId("Squire")
        };

        WorldCity city = new(
            Id: new WorldFortificationId("city"),
            NameTextId: new ScenarioTextId("Hemwyk Court"),
            DefinitionId: new WorldCityDefinitionId(WorldRace.Dwarf, 1),
            OwnerId: enemy.Id,
            SubraceId: TestHelpers.NeutralSubrace.Id,
            Footprint: new WorldFootprint(new WorldPosition(0, 0), 4, 4),
            Garrison: TestHelpers.SingleUnitGrid(new WorldUnitId("soldier"), soldierDefinition),
            Inventory: WorldInventory.Empty,
            AiPriority: 1,
            RiotTurns: 0,
            GrowthTurn: 0);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(enemy.LordId, enemy.Race, LordCategory.Warrior))
            .WithPlayer(enemy)
            .WithDefinition(soldierDefinition)
            .WithDefinition(TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Dwarf, 1), WorldResourceSet.Empty))
            .WithCity(city)
            .BuildSession();

        WorldFortificationInfoViewModel info = WorldFortificationInfoViewModel.CreateCity(
            session.World.State,
            player.Id,
            city,
            null,
            engine.Definitions,
            TestGameTextProvider.Instance,
            engine.UnitStats);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(info.Title, Is.EqualTo("Hemwyk Court"));
            Assert.That(FormatLines(info.Lines), Is.EqualTo(["City level: 1", "Owner: Mountain Clans", "Hit point regeneration per turn: 10%"]));
            Assert.That(info.VisitingStackTitle, Is.Null);
            Assert.That(info.VisitingStack.Units, Is.Empty);
            Assert.That(info.Note, Is.Null);
            Assert.That(info.Defenses.Units, Is.EqualTo(new List<WorldUnitGroupInfoUnitViewModel>
            {
                new(
                    new AssetSpriteKey.UnitFace(soldierDefinition.Id),
                    "Squire",
                    35,
                    35,
                    IsAlive: true,
                    Row: 2,
                    Column: 0,
                    ColumnSpan: 1,
                    Statuses: [])
            }));
        }
    }
}
