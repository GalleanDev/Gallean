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
            null,
            [],
            [warDefinition.Id, thiefDefinition.Id],
            [],
            1,
            1);

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
            "[P2 1000] Leader thief -> capital",
            "[P3 1000] AutoFit Stack0001"
        ]));

        UnitGrid<WorldUnit> expectedUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("Unit0001"), thiefDefinition) with { Name = new LeaderUnitName(new LeaderNameId(WorldRace.Undead, true, 0)) }, GridPosition.BackCenter, false)
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

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            IsAi = true
        };
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
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.PoisonStack, 90))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Spy, 100))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.StealItem, 80))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Assassinate, 70))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.CounterfeitOrders, 90))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Duel, 100))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(thiefStack)
            .WithStack(targetStack)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo([
            "[P2 149] Spy thief -> (3, 0)",
            "[P2 104] Assassinate thief -> (3, 0)"
        ]));
        Assert.That(session.World.State.Players[player1.Id].SpiedPlayers, Is.EqualTo([player2.Id]));
        Assert.That(session.World.State.Stacks.Keys, Is.EqualTo([thiefStack.Id]));
        Assert.That(session.World.State.Stacks[thiefStack.Id], Is.EqualTo(thiefStack with { MovementPoints = 0 }));
    }

    [Test]
    public void ApproachTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 1);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 40, damage: 5),
            movementPoints: 6, leadership: 6);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            IsAi = true
        };
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
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.PoisonStack, 90))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Spy, 100))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.StealItem, 80))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Assassinate, 70))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.CounterfeitOrders, 90))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Duel, 100))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(thiefStack)
            .WithStack(targetStack)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo([
            "[P2 141] Move thief -> (5, 0)",
            "[P2 149] Spy thief -> (6, 0)"
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
            IsAi = true,
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
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.PoisonStack, 90))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Spy, 100))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.StealItem, 80))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Assassinate, 70))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.CounterfeitOrders, 90))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Duel, 100))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(thiefStack)
            .WithStack(targetStack)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo([
            "[P2 99] Move thief -> (5, 0)",
            "[P2 104] Assassinate thief -> (6, 0)"
        ]));
        Assert.That(session.World.State.Stacks.Keys, Is.EqualTo([thiefStack.Id]));
        Assert.That(session.World.State.Stacks[thiefStack.Id], Is.EqualTo(thiefStack with
        {
            Position = new WorldPosition(5, 0),
            Direction = WorldStackDirection.SouthEast,
            MovementPoints = 0
        }));
    }

    [Test]
    public void RiotCityTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition soldier = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 5);
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            IsAi = true
        };
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
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Spy, 100))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.PoisonCity, 90))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.StealGold, 80))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.StealSpell, 80))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Assassinate, 70))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.CounterfeitOrders, 90))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Bribe, 100))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.RiotCity, 100))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(thiefStack)
            .WithCity(city)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo([
            "[P2 125] Spy thief -> (3, 3)",
            "[P2 125] Riot thief -> (3, 3)"
        ]));
        Assert.That(session.World.State.Fortifications[city.Id], Is.EqualTo(city with { RiotTurns = 3 }));
        Assert.That(session.World.State.Players[player1.Id].SpiedPlayers, Is.EqualTo([player2.Id]));
        Assert.That(session.World.State.Stacks[thiefStack.Id], Is.EqualTo(thiefStack with { MovementPoints = 0 }));
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
            IsAi = true,
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
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Spy, 100))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.PoisonCity, 90))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.StealGold, 80))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.StealSpell, 80))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Assassinate, 70))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.CounterfeitOrders, 90))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Bribe, 100))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.RiotCity, 100))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(thiefStack)
            .WithCity(city)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo([
            "[P2 125] Riot thief -> (3, 3)"
        ]));
        Assert.That(session.World.State.Fortifications[city.Id], Is.EqualTo(city with { RiotTurns = 1 }));
        Assert.That(session.World.State.Players[player1.Id].SpiedPlayers, Is.EqualTo([player2.Id]));
        Assert.That(session.World.State.Stacks[thiefStack.Id], Is.EqualTo(thiefStack with { MovementPoints = 10 }));
    }

    [Test]
    public void DuelTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 1);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 30, initiative: 10, damage: 5),
            movementPoints: 6, leadership: 6) with
        {
            ExperienceKilled = 100
        };

        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);
        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            IsAi = true,
            SpiedPlayers = [player2.Id]
        };

        WorldStack thiefStack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player1.Id,
            new WorldPosition(2, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("thief"), thief),
            movementPoints: 10);

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
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.PoisonStack, 90))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Spy, 100))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.StealItem, 80))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Assassinate, 70))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.CounterfeitOrders, 90))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Duel, 100))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(thiefStack)
            .WithStack(targetStack)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo([
            "[P2 149] Duel thief -> (3, 0) (Attacker wins)"
        ]));
        Assert.That(session.World.State.Stacks.Keys, Is.EqualTo([thiefStack.Id]));
        Assert.That(session.World.State.Stacks[thiefStack.Id], Is.EqualTo(thiefStack with
        {
            Units = new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("thief"), thief) with { Hitpoints = 86, Experience = 100 }, GridPosition.FrontBottom, false)
                .Build(),
            Direction = WorldStackDirection.SouthEast,
            MovementPoints = 0,
            BattlesWon = 1
        }));
    }

    [Test]
    public void DuelUnwinnableTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 1);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 300, initiative: 60, damage: 50),
            movementPoints: 6, leadership: 6) with
        {
            ExperienceKilled = 100
        };

        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);
        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            IsAi = true,
            SpiedPlayers = [player2.Id]
        };

        WorldStack thiefStack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player1.Id,
            new WorldPosition(2, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("thief"), thief),
            movementPoints: 10);

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
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.PoisonStack, 90))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Spy, 100))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.StealItem, 80))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Assassinate, 70))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.CounterfeitOrders, 90))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Duel, 100))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(thiefStack)
            .WithStack(targetStack)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo([
            "[P2 104] Assassinate thief -> (3, 0)"
        ]));
        Assert.That(session.World.State.Stacks.Keys, Is.EqualTo([thiefStack.Id]));
        Assert.That(session.World.State.Stacks[thiefStack.Id], Is.EqualTo(thiefStack with { MovementPoints = 0 }));
    }

    [Test]
    public void StealItemTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 1);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 40, damage: 5),
            movementPoints: 6, leadership: 6);
        ItemDefinition cheapDefinition = TestHelpers.SimpleItemDefinition(new ItemDefinitionId("cheap"), ItemCategory.Valuable) with
        {
            Value = new WorldResourceSet(300, 0, 0, 0, 0, 0)
        };
        ItemDefinition dearDefinition = TestHelpers.SimpleItemDefinition(new ItemDefinitionId("dear"), ItemCategory.Valuable) with
        {
            Value = new WorldResourceSet(900, 0, 0, 0, 0, 0)
        };
        WorldItem cheapItem = new(new WorldItemId("Cheap"), cheapDefinition.Id, Charges: null);
        WorldItem dearItem = new(new WorldItemId("Dear"), dearDefinition.Id, Charges: null);

        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);
        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            IsAi = true,
            SpiedPlayers = [player2.Id]
        };

        WorldStack thiefStack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player1.Id,
            new WorldPosition(2, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("thief"), thief),
            movementPoints: 10);

        WorldStack targetStack = TestHelpers.SimpleStack(
                new WorldStackId("enemy"),
                player2.Id,
                new WorldPosition(3, 0),
                TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
                movementPoints: 6) with
            {
                Inventory = new WorldInventory([cheapItem, dearItem])
            };

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(leader)
            .WithDefinition(cheapDefinition)
            .WithDefinition(dearDefinition)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.PoisonStack, 90))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Spy, 100))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.StealItem, 80))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Assassinate, 70))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.CounterfeitOrders, 90))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Duel, 100))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(thiefStack)
            .WithStack(targetStack)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo([
            "[P2 214] Steal dear thief -> (3, 0)"
        ]));
        Assert.That(session.World.State.Stacks[targetStack.Id], Is.EqualTo(targetStack with { Inventory = new WorldInventory([cheapItem]) }));
        Assert.That(session.World.State.Stacks[thiefStack.Id], Is.EqualTo(thiefStack with
        {
            MovementPoints = 0,
            Inventory = new WorldInventory([dearItem])
        }));
    }

    [Test]
    public void StealGoldTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition soldier = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 5);
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty);

        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map) with
        {
            Resources = new WorldResourceSet(8000, 0, 0, 0, 0, 0)
        };
        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            IsAi = true,
            SpiedPlayers = [player2.Id]
        };

        WorldStack thiefStack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player1.Id,
            new WorldPosition(4, 3),
            TestHelpers.SingleUnitGrid(new WorldUnitId("thief"), thief),
            movementPoints: 10);

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            player2.Id,
            new WorldPosition(3, 3),
            TestHelpers.SingleUnitGrid(new WorldUnitId("soldier"), soldier));

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(soldier)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Spy, 100))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.PoisonCity, 90))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.StealGold, 80))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.StealSpell, 80))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Assassinate, 70))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.CounterfeitOrders, 90))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Bribe, 100))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.RiotCity, 40))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(thiefStack)
            .WithCity(city)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo([
            "[P2 175] Steal gold thief -> (3, 3)"
        ]));
        Assert.That(session.World.State.Players[player1.Id].Resources, Is.EqualTo(new WorldResourceSet(641, 0, 0, 0, 0, 0)));
        Assert.That(session.World.State.Players[player2.Id].Resources, Is.EqualTo(new WorldResourceSet(7459, 0, 0, 0, 25, 0)));
        Assert.That(session.World.State.Stacks[thiefStack.Id], Is.EqualTo(thiefStack with { MovementPoints = 0 }));
    }

    [Test]
    public void StealSpellTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition soldier = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 5);
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty);
        WorldSpellDefinition spell = TestHelpers.SimpleSpellDefinition(new WorldSpellDefinitionId("spell"), new WorldFogSpellEffect()) with
        {
            PurchaseCost = new WorldResourceSet(1000, 0, 0, 0, 0, 0)
        };

        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [spell.Id])
        };
        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            IsAi = true,
            SpiedPlayers = [player2.Id]
        };

        WorldStack thiefStack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player1.Id,
            new WorldPosition(4, 3),
            TestHelpers.SingleUnitGrid(new WorldUnitId("thief"), thief),
            movementPoints: 10);

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            player2.Id,
            new WorldPosition(3, 3),
            TestHelpers.SingleUnitGrid(new WorldUnitId("soldier"), soldier));

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(soldier)
            .WithDefinition(cityDefinition)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Spy, 100))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.PoisonCity, 90))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.StealGold, 80))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.StealSpell, 80))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Assassinate, 70))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.CounterfeitOrders, 90))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Bribe, 100))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.RiotCity, 40))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(thiefStack)
            .WithCity(city)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo([
            "[P2 175] Steal spell thief -> (3, 3)"
        ]));
        Assert.That(session.World.State.Players[player1.Id].Spellbook, Is.EqualTo(new WorldPlayerSpellbook([], [spell.Id])));
        Assert.That(session.World.State.Stacks[thiefStack.Id], Is.EqualTo(thiefStack with { MovementPoints = 0 }));
    }

    [Test]
    public void AssassinateTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 1);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 40, damage: 5),
            movementPoints: 6, leadership: 6);
        UnitDefinition soldier = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 50, initiative: 30, damage: 5);

        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);
        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            IsAi = true,
            SpiedPlayers = [player2.Id]
        };

        WorldStack thiefStack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player1.Id,
            new WorldPosition(2, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("thief"), thief),
            movementPoints: 10);

        WorldStack targetStack = TestHelpers.SimpleStack(
            new WorldStackId("enemy"),
            player2.Id,
            new WorldPosition(3, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader), GridPosition.FrontBottom, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldier), GridPosition.FrontCenter, false)
                .Build(),
            movementPoints: 6);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(leader)
            .WithDefinition(soldier)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.PoisonStack, 90))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Spy, 100))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.StealItem, 80))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Assassinate, 70))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.CounterfeitOrders, 90))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Duel, 100))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(thiefStack)
            .WithStack(targetStack)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo([
            "[P2 104] Assassinate thief -> (3, 0)"
        ]));
        Assert.That(session.World.State.Stacks[targetStack.Id], Is.EqualTo(targetStack with
        {
            Units = new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader), GridPosition.FrontBottom, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldier) with { Hitpoints = 0 }, GridPosition.FrontCenter, false)
                .Build()
        }));
        Assert.That(session.World.State.Stacks[thiefStack.Id], Is.EqualTo(thiefStack with { MovementPoints = 0 }));
    }

    [Test]
    public void BribeTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition guard = TestHelpers.SimpleUnitDefinition("guard", maxHitpoints: 100, initiative: 40, damage: 5) with
        {
            ExperienceKilled = 10
        };
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            IsAi = true,
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0)
        };
        WorldPlayer neutral = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Neutral, map);

        WorldStack thiefStack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player1.Id,
            new WorldPosition(4, 3),
            TestHelpers.SingleUnitGrid(new WorldUnitId("thief"), thief),
            movementPoints: 10);

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            neutral.Id,
            new WorldPosition(3, 3),
            TestHelpers.SingleUnitGrid(new WorldUnitId("guard"), guard));

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(guard)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Spy, 100))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.PoisonCity, 90))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.StealGold, 80))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.StealSpell, 80))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Assassinate, 70))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.CounterfeitOrders, 90))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Bribe, 100))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.RiotCity, 100))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(neutral.LordId, neutral.Race, LordCategory.Warrior))
            .WithPlayer(neutral)
            .WithStack(thiefStack)
            .WithCity(city)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo([
            "[P2 175] Bribe thief -> (3, 3)"
        ]));
        Assert.That(session.World.State.Fortifications[city.Id], Is.EqualTo(city with
        {
            OwnerId = player1.Id,
            Garrison = UnitGrid<WorldUnit>.Empty
        }));
        Assert.That(session.World.State.Players[player1.Id].Resources, Is.EqualTo(new WorldResourceSet(70, 0, 0, 0, 0, 0)));
    }

    [Test]
    public void TwoTargetsTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 3);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 40, damage: 5),
            movementPoints: 6, leadership: 6);
        ItemDefinition itemDefinition = TestHelpers.SimpleItemDefinition(new ItemDefinitionId("dear"), ItemCategory.Valuable) with
        {
            Value = new WorldResourceSet(900, 0, 0, 0, 0, 0)
        };
        WorldItem item = new(new WorldItemId("Dear"), itemDefinition.Id, Charges: null);

        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);
        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            IsAi = true,
            SpiedPlayers = [player2.Id]
        };

        WorldStack thiefStack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player1.Id,
            new WorldPosition(0, 1),
            TestHelpers.SingleUnitGrid(new WorldUnitId("thief"), thief),
            movementPoints: 20);

        WorldStack nearStack = TestHelpers.SimpleStack(
            new WorldStackId("near"),
            player2.Id,
            new WorldPosition(2, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("nearLeader"), leader),
            movementPoints: 6);

        WorldStack farStack = TestHelpers.SimpleStack(
                new WorldStackId("far"),
                player2.Id,
                new WorldPosition(6, 2),
                TestHelpers.SingleUnitGrid(new WorldUnitId("farLeader"), leader),
                movementPoints: 6) with
            {
                Inventory = new WorldInventory([item])
            };

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(leader)
            .WithDefinition(itemDefinition)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.PoisonStack, 90))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Spy, 100))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.StealItem, 80))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Assassinate, 70))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.CounterfeitOrders, 90))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Duel, 100))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(thiefStack)
            .WithStack(nearStack)
            .WithStack(farStack)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo([
            "[P2 203] Move thief -> (5, 1)",
            "[P2 214] Steal dear thief -> (6, 2)"
        ]));
        Assert.That(session.World.State.Stacks[nearStack.Id], Is.EqualTo(nearStack));
        Assert.That(session.World.State.Stacks[farStack.Id], Is.EqualTo(farStack with { Inventory = WorldInventory.Empty }));
        Assert.That(session.World.State.Stacks[thiefStack.Id], Is.EqualTo(thiefStack with
        {
            Position = new WorldPosition(5, 1),
            Direction = WorldStackDirection.SouthEast,
            MovementPoints = 0,
            Inventory = new WorldInventory([item])
        }));
    }

    [Test]
    public void PoisonTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 1);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 300, initiative: 60, damage: 50),
            movementPoints: 6, leadership: 6);

        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);
        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            IsAi = true,
            SpiedPlayers = [player2.Id]
        };

        WorldStack thiefStack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player1.Id,
            new WorldPosition(2, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("thief"), thief),
            movementPoints: 10);

        WorldStack targetStack = TestHelpers.SimpleStack(
            new WorldStackId("enemy"),
            player2.Id,
            new WorldPosition(3, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader) with { Hitpoints = 100 }, GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 6);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.PoisonStack, 90))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Spy, 100))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.StealItem, 80))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Assassinate, 40))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.CounterfeitOrders, 90))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Duel, 100))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(thiefStack)
            .WithStack(targetStack)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo([
            "[P2 14] Poison thief -> (3, 0)"
        ]));
        Assert.That(session.World.State.Stacks[targetStack.Id], Is.EqualTo(targetStack with
        {
            Units = new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader) with { Hitpoints = 140 }, GridPosition.FrontBottom, false)
                .Build()
        }));
        Assert.That(session.World.State.Stacks[thiefStack.Id], Is.EqualTo(thiefStack with { MovementPoints = 0 }));
    }

    [Test]
    public void CounterfeitOrdersTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 1);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 300, initiative: 60, damage: 50),
            movementPoints: 6, leadership: 6);
        UnitDefinition archer = TestHelpers.SimpleUnitDefinition("archer", maxHitpoints: 100, initiative: 40, damage: 20);

        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);
        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            IsAi = true,
            SpiedPlayers = [player2.Id]
        };

        WorldStack thiefStack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player1.Id,
            new WorldPosition(2, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("thief"), thief),
            movementPoints: 10);

        WorldStack targetStack = TestHelpers.SimpleStack(
            new WorldStackId("enemy"),
            player2.Id,
            new WorldPosition(3, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader), GridPosition.FrontBottom, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("archer"), archer), GridPosition.BackBottom, false)
                .Build(),
            movementPoints: 6);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(leader)
            .WithDefinition(archer)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.PoisonStack, 90))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Spy, 100))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.StealItem, 80))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Assassinate, 40))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.CounterfeitOrders, 90))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Duel, 100))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(thiefStack)
            .WithStack(targetStack)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo([
            "[P2 27] Counterfeit orders thief -> (3, 0)"
        ]));
        Assert.That(session.World.State.Stacks[targetStack.Id], Is.EqualTo(targetStack with
        {
            Units = new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("archer"), archer), GridPosition.FrontCenter, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader), GridPosition.FrontTop, false)
                .Build()
        }));
        Assert.That(session.World.State.Stacks[thiefStack.Id], Is.EqualTo(thiefStack with { MovementPoints = 0 }));
    }

    [Test]
    public void SpyRuinTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition guard = TestHelpers.SimpleUnitDefinition("guard", maxHitpoints: 100, initiative: 40, damage: 5);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            IsAi = true
        };
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack thiefStack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player1.Id,
            new WorldPosition(5, 4),
            TestHelpers.SingleUnitGrid(new WorldUnitId("thief"), thief),
            movementPoints: 10);

        WorldRuin ruin = new(
            new WorldFortificationId("ruin"),
            new ScenarioTextId("Ruin"),
            new WorldFootprint(new WorldPosition(2, 2), 3, 3),
            new WorldObjectVariant(0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("guard"), guard),
            new WorldResourceSet(300, 0, 0, 0, 0, 0),
            Item: null,
            LooterId: null,
            AiPriority: 3);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(guard)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.SpyRuin, 90))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(thiefStack)
            .WithRuin(ruin)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo([
            "[P2 23] Spy ruin thief -> (4, 4)"
        ]));
        Assert.That(session.World.State.Players[player1.Id].SpiedRuins, Is.EqualTo([ruin.Id]));
        Assert.That(session.World.State.Stacks[thiefStack.Id], Is.EqualTo(thiefStack with { MovementPoints = 0 }));
    }

    [Test]
    public void StealMerchantTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        ItemDefinition cheapDefinition = TestHelpers.SimpleItemDefinition(new ItemDefinitionId("cheap"), ItemCategory.Valuable) with
        {
            Value = new WorldResourceSet(300, 0, 0, 0, 0, 0)
        };
        ItemDefinition dearDefinition = TestHelpers.SimpleItemDefinition(new ItemDefinitionId("dear"), ItemCategory.Valuable) with
        {
            Value = new WorldResourceSet(900, 0, 0, 0, 0, 0)
        };

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            IsAi = true
        };
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack thiefStack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player1.Id,
            new WorldPosition(5, 4),
            TestHelpers.SingleUnitGrid(new WorldUnitId("thief"), thief),
            movementPoints: 10);

        WorldMerchantSite site = new(
            new WorldSiteId("site"),
            new WorldFootprint(new WorldPosition(2, 2), 3, 3),
            Variant: new WorldObjectVariant(0),
            TitleTextId: new ScenarioTextId("Title"),
            GreetingTextId: new ScenarioTextId("Greeting"),
            [new WorldMerchantItem(cheapDefinition.Id, 1), new WorldMerchantItem(dearDefinition.Id, 1)],
            VisitorIds: [],
            AiPriority: 3);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithRandom(new TestSeedRandomProvider(3))
            .WithDefinition(thief)
            .WithDefinition(cheapDefinition)
            .WithDefinition(dearDefinition)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.StealMerchant, 60))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(thiefStack)
            .WithSite(site)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo([
            "[P2 134] Steal dear thief -> (4, 4)"
        ]));
        Assert.That(session.World.State.Sites[site.Id], Is.EqualTo(site with { Items = [new WorldMerchantItem(cheapDefinition.Id, 1)] }));
        Assert.That(session.World.State.Stacks[thiefStack.Id], Is.EqualTo(thiefStack with
        {
            MovementPoints = 0,
            Inventory = new WorldInventory([new WorldItem(new WorldItemId("Item0001"), dearDefinition.Id, Charges: null)])
        }));
    }

    [Test]
    public void SiteEntranceTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        ItemDefinition cheapDefinition = TestHelpers.SimpleItemDefinition(new ItemDefinitionId("cheap"), ItemCategory.Valuable) with
        {
            Value = new WorldResourceSet(300, 0, 0, 0, 0, 0)
        };
        ItemDefinition dearDefinition = TestHelpers.SimpleItemDefinition(new ItemDefinitionId("dear"), ItemCategory.Valuable) with
        {
            Value = new WorldResourceSet(900, 0, 0, 0, 0, 0)
        };

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            IsAi = true
        };
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack thiefStack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player1.Id,
            new WorldPosition(4, 4),
            TestHelpers.SingleUnitGrid(new WorldUnitId("thief"), thief),
            movementPoints: 10);

        WorldMerchantSite site = new(
            new WorldSiteId("site"),
            new WorldFootprint(new WorldPosition(2, 2), 3, 3),
            Variant: new WorldObjectVariant(0),
            TitleTextId: new ScenarioTextId("Title"),
            GreetingTextId: new ScenarioTextId("Greeting"),
            [new WorldMerchantItem(cheapDefinition.Id, 1), new WorldMerchantItem(dearDefinition.Id, 1)],
            VisitorIds: [],
            AiPriority: 3);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(cheapDefinition)
            .WithDefinition(dearDefinition)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.StealMerchant, 60))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(thiefStack)
            .WithSite(site)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo(Array.Empty<string>()));
        Assert.That(session.World.State.Sites[site.Id], Is.EqualTo(site));
    }

    [Test]
    public void StealMageTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        WorldSpellDefinition cheapSpell = TestHelpers.SimpleSpellDefinition(new WorldSpellDefinitionId("cheap"), new WorldFogSpellEffect()) with
        {
            PurchaseCost = new WorldResourceSet(300, 0, 0, 0, 0, 0)
        };
        WorldSpellDefinition dearSpell = TestHelpers.SimpleSpellDefinition(new WorldSpellDefinitionId("dear"), new WorldFogSpellEffect()) with
        {
            PurchaseCost = new WorldResourceSet(900, 0, 0, 0, 0, 0)
        };

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            IsAi = true
        };
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack thiefStack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player1.Id,
            new WorldPosition(5, 4),
            TestHelpers.SingleUnitGrid(new WorldUnitId("thief"), thief),
            movementPoints: 10);

        WorldMageSite site = new(
            new WorldSiteId("site"),
            new WorldFootprint(new WorldPosition(2, 2), 3, 3),
            Variant: new WorldObjectVariant(0),
            TitleTextId: new ScenarioTextId("Title"),
            GreetingTextId: new ScenarioTextId("Greeting"),
            [cheapSpell.Id, dearSpell.Id],
            VisitorIds: [],
            AiPriority: 3);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithRandom(new TestSeedRandomProvider(3))
            .WithDefinition(thief)
            .WithDefinition(cheapSpell)
            .WithDefinition(dearSpell)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.StealMage, 60))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(thiefStack)
            .WithSite(site)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo([
            "[P2 134] Steal dear thief -> (4, 4)"
        ]));
        Assert.That(session.World.State.Players[player1.Id].Spellbook, Is.EqualTo(new WorldPlayerSpellbook([], [dearSpell.Id])));
        Assert.That(session.World.State.Stacks[thiefStack.Id], Is.EqualTo(thiefStack with { MovementPoints = 0 }));
    }

    [Test]
    public void StealSpecialItemTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 1);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 40, damage: 5),
            movementPoints: 6, leadership: 6);
        ItemDefinition gemDefinition = TestHelpers.SimpleItemDefinition(new ItemDefinitionId("gem"), ItemCategory.Valuable) with
        {
            Value = new WorldResourceSet(900, 0, 0, 0, 0, 0)
        };
        ItemDefinition specialDefinition = TestHelpers.SimpleItemDefinition(new ItemDefinitionId("special"), ItemCategory.Special) with
        {
            Value = new WorldResourceSet(5000, 0, 0, 0, 0, 0)
        };
        WorldItem gemItem = new(new WorldItemId("Gem"), gemDefinition.Id, Charges: null);
        WorldItem specialItem = new(new WorldItemId("Special"), specialDefinition.Id, Charges: null);

        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);
        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            IsAi = true,
            SpiedPlayers = [player2.Id]
        };

        WorldStack thiefStack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player1.Id,
            new WorldPosition(2, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("thief"), thief),
            movementPoints: 10);

        WorldStack targetStack = TestHelpers.SimpleStack(
                new WorldStackId("enemy"),
                player2.Id,
                new WorldPosition(3, 0),
                TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
                movementPoints: 6) with
            {
                Inventory = new WorldInventory([gemItem, specialItem])
            };

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(leader)
            .WithDefinition(gemDefinition)
            .WithDefinition(specialDefinition)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.PoisonStack, 90))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Spy, 100))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.StealItem, 80))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Assassinate, 70))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.CounterfeitOrders, 90))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Duel, 100))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(thiefStack)
            .WithStack(targetStack)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo([
            "[P2 214] Steal gem thief -> (3, 0)"
        ]));
        Assert.That(session.World.State.Stacks[targetStack.Id], Is.EqualTo(targetStack with { Inventory = new WorldInventory([specialItem]) }));
        Assert.That(session.World.State.Stacks[thiefStack.Id], Is.EqualTo(thiefStack with
        {
            MovementPoints = 0,
            Inventory = new WorldInventory([gemItem])
        }));
    }
}
