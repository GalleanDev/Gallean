using Gallean.Common;

namespace Gallean.Engine.Tests;

public sealed class WorldEngineAiEnterCityTests
{
    [Test]
    public void EnterCityTest()
    {
        WorldMap map = TestHelpers.SimpleMap(10, 4);

        UnitDefinition main = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("main", maxHitpoints: 100, initiative: 50, damage: 50),
            movementPoints: 20, leadership: 6);

        WorldPlayer neutral = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Neutral, map) with { IsAi = true };
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Neutral, 1), WorldResourceSet.Empty);
        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            neutral.Id,
            new WorldPosition(3, 3),
            UnitGrid<WorldUnit>.Empty);
        WorldStack roamStack = TestHelpers.SimpleStack(
                new WorldStackId("Roam"),
                neutral.Id,
                new WorldPosition(8, 3),
                TestHelpers.SingleUnitGrid(new WorldUnitId("main"), main),
                movementPoints: 20) with
            {
                Order = WorldStackOrder.Roam.Instance
            };

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(main)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(neutral.LordId, neutral.Race, LordCategory.Warrior))
            .WithPlayer(neutral)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCity(city)
            .WithStack(roamStack)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo(["[P2 980] Move Roam -> (3, 3)"]));
        Assert.That(session.World.State.Stacks[roamStack.Id], Is.EqualTo(roamStack with
        {
            Position = city.Entrance,
            Direction = WorldStackDirection.West,
            MovementPoints = 12
        }));
    }

    [Test]
    public void StayInsideTest()
    {
        WorldMap map = TestHelpers.SimpleMap(10, 4);

        UnitDefinition main = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("main", maxHitpoints: 100, initiative: 50, damage: 50),
            movementPoints: 20, leadership: 6);
        UnitDefinition weak = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("weak", maxHitpoints: 5, initiative: 40, damage: 1),
            movementPoints: 6, leadership: 6);

        WorldPlayer neutral = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Neutral, map) with { IsAi = true };
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Neutral, 1), WorldResourceSet.Empty);
        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            neutral.Id,
            new WorldPosition(3, 3),
            UnitGrid<WorldUnit>.Empty);
        WorldStack roamStack = TestHelpers.SimpleStack(
                new WorldStackId("Roam"),
                neutral.Id,
                city.Entrance,
                TestHelpers.SingleUnitGrid(new WorldUnitId("main"), main),
                movementPoints: 20) with
            {
                Order = WorldStackOrder.Roam.Instance
            };
        WorldStack weakStack = TestHelpers.SimpleStack(
            new WorldStackId("Weak"),
            player.Id,
            new WorldPosition(4, 3),
            TestHelpers.SingleUnitGrid(new WorldUnitId("weak"), weak),
            movementPoints: 0);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(main)
            .WithDefinition(weak)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(neutral.LordId, neutral.Race, LordCategory.Warrior))
            .WithPlayer(neutral)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCity(city)
            .WithStack(roamStack)
            .WithStack(weakStack)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.Empty);
        Assert.That(session.World.State.Stacks[roamStack.Id], Is.EqualTo(roamStack));
    }

    [Test]
    public void NearCityOverAttackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(10, 4);

        UnitDefinition main = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("main", maxHitpoints: 100, initiative: 50, damage: 50),
            movementPoints: 20, leadership: 6);
        UnitDefinition weak = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("weak", maxHitpoints: 5, initiative: 40, damage: 1),
            movementPoints: 6, leadership: 6);

        WorldPlayer neutral = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Neutral, map) with { IsAi = true };
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Neutral, 1), WorldResourceSet.Empty);
        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            neutral.Id,
            new WorldPosition(3, 3),
            UnitGrid<WorldUnit>.Empty);
        WorldStack roamStack = TestHelpers.SimpleStack(
                new WorldStackId("Roam"),
                neutral.Id,
                new WorldPosition(6, 3),
                TestHelpers.SingleUnitGrid(new WorldUnitId("main"), main),
                movementPoints: 20) with
            {
                Order = WorldStackOrder.Roam.Instance
            };
        WorldStack weakStack = TestHelpers.SimpleStack(
            new WorldStackId("Weak"),
            player.Id,
            new WorldPosition(7, 3),
            TestHelpers.SingleUnitGrid(new WorldUnitId("weak"), weak),
            movementPoints: 0);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(main)
            .WithDefinition(weak)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(neutral.LordId, neutral.Race, LordCategory.Warrior))
            .WithPlayer(neutral)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCity(city)
            .WithStack(roamStack)
            .WithStack(weakStack)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo(["[P2 988] Move Roam -> (3, 3)"]));
        Assert.That(session.World.State.Stacks[roamStack.Id], Is.EqualTo(roamStack with
        {
            Position = city.Entrance,
            Direction = WorldStackDirection.West,
            MovementPoints = 16
        }));
        Assert.That(session.World.State.Stacks[weakStack.Id], Is.EqualTo(weakStack with
        {
            MovementPoints = 6
        }));
    }

    [Test]
    public void AttackOverFarCityTest()
    {
        WorldMap map = TestHelpers.SimpleMap(30, 4);

        UnitDefinition main = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("main", maxHitpoints: 100, initiative: 50, damage: 50),
            movementPoints: 20, leadership: 6);
        UnitDefinition weak = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("weak", maxHitpoints: 5, initiative: 40, damage: 1),
            movementPoints: 6, leadership: 6);

        WorldPlayer neutral = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Neutral, map) with { IsAi = true };
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Neutral, 1), WorldResourceSet.Empty);
        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            neutral.Id,
            new WorldPosition(3, 3),
            UnitGrid<WorldUnit>.Empty);
        WorldStack roamStack = TestHelpers.SimpleStack(
                new WorldStackId("Roam"),
                neutral.Id,
                new WorldPosition(27, 3),
                TestHelpers.SingleUnitGrid(new WorldUnitId("main"), main),
                movementPoints: 20) with
            {
                Order = WorldStackOrder.Roam.Instance
            };
        WorldStack weakStack = TestHelpers.SimpleStack(
            new WorldStackId("Weak"),
            player.Id,
            new WorldPosition(28, 3),
            TestHelpers.SingleUnitGrid(new WorldUnitId("weak"), weak),
            movementPoints: 0);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(main)
            .WithDefinition(weak)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(neutral.LordId, neutral.Race, LordCategory.Warrior))
            .WithPlayer(neutral)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCity(city)
            .WithStack(roamStack)
            .WithStack(weakStack)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo([
            "[P2 910] Move Roam -> (28, 3) (Attacker wins)",
            "[P2 904] Move Roam -> (22, 2)"
        ]));
        Assert.That(session.World.State.Stacks.Keys, Is.EqualTo([roamStack.Id]));
    }

    [Test]
    public void CityFilterTest()
    {
        WorldMap map = TestHelpers.SimpleMap(28, 6);

        UnitDefinition main = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("main", maxHitpoints: 100, initiative: 50, damage: 50),
            movementPoints: 20, leadership: 6);

        WorldPlayer neutral = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Neutral, map) with { IsAi = true };
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Neutral, 1), WorldResourceSet.Empty);
        WorldCity otherSubraceCity = TestHelpers.SimpleCity(
                new WorldFortificationId("OtherSubrace"),
                cityDefinition,
                neutral.Id,
                new WorldPosition(8, 3),
                UnitGrid<WorldUnit>.Empty) with
            {
                SubraceId = new WorldSubraceId("other")
            };
        WorldCity occupiedCity = TestHelpers.SimpleCity(
            new WorldFortificationId("Occupied"),
            cityDefinition,
            neutral.Id,
            new WorldPosition(13, 3),
            UnitGrid<WorldUnit>.Empty);
        WorldCity riotingCity = TestHelpers.SimpleCity(
                new WorldFortificationId("Rioting"),
                cityDefinition,
                neutral.Id,
                new WorldPosition(17, 3),
                UnitGrid<WorldUnit>.Empty) with
            {
                RiotTurns = 2
            };
        WorldCity freeCity = TestHelpers.SimpleCity(
            new WorldFortificationId("Free"),
            cityDefinition,
            neutral.Id,
            new WorldPosition(26, 3),
            UnitGrid<WorldUnit>.Empty);
        WorldStack roamStack = TestHelpers.SimpleStack(
                new WorldStackId("Roam"),
                neutral.Id,
                new WorldPosition(12, 5),
                TestHelpers.SingleUnitGrid(new WorldUnitId("main"), main),
                movementPoints: 20) with
            {
                Order = WorldStackOrder.Roam.Instance
            };
        WorldStack standStack = TestHelpers.SimpleStack(
                new WorldStackId("Stand"),
                neutral.Id,
                occupiedCity.Entrance,
                TestHelpers.SingleUnitGrid(new WorldUnitId("main2"), main),
                movementPoints: 20) with
            {
                Order = WorldStackOrder.Stand.Instance
            };

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(main)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(neutral.LordId, neutral.Race, LordCategory.Warrior))
            .WithPlayer(neutral)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCity(otherSubraceCity)
            .WithCity(occupiedCity)
            .WithCity(riotingCity)
            .WithCity(freeCity)
            .WithStack(roamStack)
            .WithStack(standStack)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo(["[P2 944] Move Roam -> (22, 4)"]));
    }

    [Test]
    public void SummonLeaderTest()
    {
        WorldMap map = TestHelpers.SimpleMap(10, 4);

        UnitDefinition summon = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("summon", maxHitpoints: 100, initiative: 50, damage: 50),
                movementPoints: 20, leadership: 6) with
            {
                Category = UnitCategory.Summon
            };

        WorldPlayer neutral = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Neutral, map) with { IsAi = true };
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Neutral, 1), WorldResourceSet.Empty);
        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            neutral.Id,
            new WorldPosition(3, 3),
            UnitGrid<WorldUnit>.Empty);
        WorldStack roamStack = TestHelpers.SimpleStack(
                new WorldStackId("Roam"),
                neutral.Id,
                new WorldPosition(8, 3),
                TestHelpers.SingleUnitGrid(new WorldUnitId("summon"), summon),
                movementPoints: 20) with
            {
                Order = WorldStackOrder.Roam.Instance
            };

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(summon)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(neutral.LordId, neutral.Race, LordCategory.Warrior))
            .WithPlayer(neutral)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCity(city)
            .WithStack(roamStack)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.Empty);
        Assert.That(session.World.State.Stacks[roamStack.Id], Is.EqualTo(roamStack));
    }

    [Test]
    public void FarAttackUnderCityTest()
    {
        WorldMap map = TestHelpers.SimpleMap(30, 4);

        UnitDefinition main = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("main", maxHitpoints: 100, initiative: 50, damage: 50),
            movementPoints: 20, leadership: 6);
        UnitDefinition weak = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("weak", maxHitpoints: 5, initiative: 40, damage: 1),
            movementPoints: 6, leadership: 6);

        WorldPlayer neutral = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Neutral, map) with { IsAi = true };
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Neutral, 1), WorldResourceSet.Empty);
        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            neutral.Id,
            new WorldPosition(3, 3),
            UnitGrid<WorldUnit>.Empty);
        WorldStack roamStack = TestHelpers.SimpleStack(
                new WorldStackId("Roam"),
                neutral.Id,
                new WorldPosition(20, 3),
                TestHelpers.SingleUnitGrid(new WorldUnitId("main"), main),
                movementPoints: 20) with
            {
                Order = WorldStackOrder.Roam.Instance
            };
        WorldStack weakStack = TestHelpers.SimpleStack(
            new WorldStackId("Weak"),
            player.Id,
            new WorldPosition(29, 3),
            TestHelpers.SingleUnitGrid(new WorldUnitId("weak"), weak),
            movementPoints: 0);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(main)
            .WithDefinition(weak)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(neutral.LordId, neutral.Race, LordCategory.Warrior))
            .WithPlayer(neutral)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCity(city)
            .WithStack(roamStack)
            .WithStack(weakStack)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo(["[P2 932] Move Roam -> (10, 2)"]));
        Assert.That(session.World.State.Stacks.Keys, Is.EquivalentTo([roamStack.Id, weakStack.Id]));
    }
}
