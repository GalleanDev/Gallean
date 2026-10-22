using System.Collections.Immutable;

namespace Gallean.Engine.Tests;

public sealed class WorldEngineAiThiefTests
{
    [Test]
    public void PurchaseThiefTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^^^",
            "^^^^^^^^",
            "^^^^^^^^",
            "^^^^^^^^",
            "^^^^...."
        ]);

        UnitDefinition thiefDefinition = TestHelpers.SimpleThiefDefinition() with
        {
            Race = WorldRace.Undead,
            PurchaseCost = new WorldResourceSet(100, 0, 0, 0, 0, 0)
        };

        UnitDefinition warDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("war", maxHitpoints: 100, initiative: 50, damage: 50),
                movementPoints: 6, leadership: 6) with
            {
                Race = WorldRace.Undead,
                PurchaseCost = new WorldResourceSet(100, 0, 0, 0, 0, 0)
            };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0)
        };

        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("capital"),
            player.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldRaceDefinition race = new(
            WorldRace.Undead,
            new GlobalTextId("Undead"),
            WorldResourceSet.Empty,
            WorldMapTerrain.Undead,
            true,
            35,
            [],
            [warDefinition.Id, thiefDefinition.Id],
            [],
            ["Nigosh"],
            ["Orissia"]);

        WorldStack mainStack = TestHelpers.SimpleStack(
            new WorldStackId("Main"),
            player.Id,
            new WorldPosition(7, 4),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("war"), warDefinition), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 0);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thiefDefinition)
            .WithDefinition(warDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCapital(capital)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithRace(race)
            .WithStack(mainStack)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        IReadOnlyList<TestWorldAiRunner.Trace> report = runner.PlayTurn();

        Assert.That(TestWorldAiTraceFormatter.Format(report), Is.EqualTo([
            "[P2 0] Leader thief -> capital",
            "[P3 1000] AutoFit Stack0001"
        ]));

        UnitGrid<WorldUnit> expectedUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("Unit0001"), thiefDefinition) with { Name = "Nigosh" }, GridPosition.BackCenter, false)
            .Build();

        Assert.That(session.World.State.Stacks[new WorldStackId("Stack0001")], Is.EqualTo(TestHelpers.SimpleStack(
            new WorldStackId("Stack0001"),
            player.Id,
            new WorldPosition(4, 4),
            expectedUnits,
            movementPoints: 20)));
        Assert.That(session.World.State.Players[player.Id].Resources, Is.EqualTo(WorldResourceSet.Empty));
    }

    [Test]
    public void SpyStackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 1);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 40, damage: 5),
            movementPoints: 6, leadership: 6);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack thiefStack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player1.Id,
            new WorldPosition(2, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("thief"), thief),
            movementPoints: 20);

        WorldStack targetStack = TestHelpers.SimpleStack(
            new WorldStackId("enemy"),
            player2.Id,
            new WorldPosition(3, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Spy, 100))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(thiefStack)
            .WithStack(targetStack)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo([
            "[P2 1000] Spy thief -> (3, 0)"
        ]));
        Assert.That(session.World.State.Players[player1.Id].SpiedPlayers, Is.EqualTo([player2.Id]));
        Assert.That(session.World.State.Stacks[thiefStack.Id], Is.EqualTo(thiefStack with { MovementPoints = 10 }));
    }

    [Test]
    public void ApproachTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 1);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 40, damage: 5),
            movementPoints: 6, leadership: 6);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack thiefStack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player1.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("thief"), thief),
            movementPoints: 20);

        WorldStack targetStack = TestHelpers.SimpleStack(
            new WorldStackId("enemy"),
            player2.Id,
            new WorldPosition(6, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Spy, 100))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(thiefStack)
            .WithStack(targetStack)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo([
            "[P2 600] Move thief -> (6, 0)",
            "[P2 1000] Spy thief -> (6, 0)"
        ]));
        Assert.That(session.World.State.Players[player1.Id].SpiedPlayers, Is.EqualTo([player2.Id]));
        Assert.That(session.World.State.Stacks[thiefStack.Id], Is.EqualTo(thiefStack with
        {
            Position = new WorldPosition(5, 0),
            Direction = WorldStackDirection.SouthEast,
            MovementPoints = 0
        }));
    }

    [Test]
    public void SpiedPlayerTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 1);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 40, damage: 5),
            movementPoints: 6, leadership: 6);

        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);
        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            SpiedPlayers = [player2.Id]
        };

        WorldStack thiefStack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player1.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("thief"), thief),
            movementPoints: 20);

        WorldStack targetStack = TestHelpers.SimpleStack(
            new WorldStackId("enemy"),
            player2.Id,
            new WorldPosition(6, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Spy, 100))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(thiefStack)
            .WithStack(targetStack)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo(Array.Empty<string>()));
        Assert.That(session.World.State.Stacks[thiefStack.Id], Is.EqualTo(thiefStack));
    }

    [Test]
    public void RiotCityTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition soldier = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 5);
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack thiefStack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player1.Id,
            new WorldPosition(4, 3),
            TestHelpers.SingleUnitGrid(new WorldUnitId("thief"), thief),
            movementPoints: 20);

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            player2.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldier), GridPosition.FrontBottom, false)
                .Build());

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(soldier)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.RiotCity, 100))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Spy, 100))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Guildmaster))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(thiefStack)
            .WithCity(city)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo([
            "[P2 1000] Riot thief -> (3, 3)"
        ]));
        Assert.That(session.World.State.Fortifications[city.Id], Is.EqualTo(city with { RiotTurns = 2 }));
        Assert.That(session.World.State.Players[player1.Id].SpiedPlayers, Is.EqualTo(ImmutableSortedSet<WorldPlayerId>.Empty));
        Assert.That(session.World.State.Stacks[thiefStack.Id], Is.EqualTo(thiefStack with { MovementPoints = 10 }));
    }

    [Test]
    public void SpiedPlayerRiotCityTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition soldier = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 5);
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty);

        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);
        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            SpiedPlayers = [player2.Id]
        };

        WorldStack thiefStack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player1.Id,
            new WorldPosition(4, 3),
            TestHelpers.SingleUnitGrid(new WorldUnitId("thief"), thief),
            movementPoints: 20);

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            player2.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldier), GridPosition.FrontBottom, false)
                .Build());

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(soldier)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.RiotCity, 100))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Spy, 100))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Guildmaster))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(thiefStack)
            .WithCity(city)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo([
            "[P2 1000] Riot thief -> (3, 3)"
        ]));
        Assert.That(session.World.State.Fortifications[city.Id], Is.EqualTo(city with { RiotTurns = 2 }));
        Assert.That(session.World.State.Players[player1.Id].SpiedPlayers, Is.EqualTo([player2.Id]));
        Assert.That(session.World.State.Stacks[thiefStack.Id], Is.EqualTo(thiefStack with { MovementPoints = 10 }));
    }
}
