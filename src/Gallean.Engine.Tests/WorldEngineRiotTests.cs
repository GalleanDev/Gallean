namespace Gallean.Engine.Tests;

public sealed class WorldEngineRiotTests
{
    [Test]
    public void GarrisonTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition soldier = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 5) with
        {
            Regeneration = 5
        };
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        WorldCity city = TestHelpers.SimpleCity(
                new WorldFortificationId("city"),
                cityDefinition,
                player1.Id,
                new WorldPosition(3, 3),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldier), GridPosition.FrontBottom, false)
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier2"), soldier) with { Hitpoints = 4 }, GridPosition.FrontCenter, false)
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier3"), soldier) with { Hitpoints = 0 }, GridPosition.FrontTop, false)
                    .Build()) with
            {
                RiotTurns = 1
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(soldier)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Mage))
            .WithPlayer(player1)
            .WithCity(city)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(),
        [
            new WorldTurnStartingEvent(player1.Id),
            new WorldCityRiotEndedEvent(city.Id),
            new WorldUnitDamagedEvent(new WorldUnitGroup.Fortification(city.Id), new WorldUnitId("soldier"), 80, SourcePlayerId: null),
            new WorldTurnStartedEvent(player1.Id),
            new WorldTurnResourcesGeneratedEvent(player1.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0))
        ]);

        UnitGrid<WorldUnit> riotedGarrison = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldier) with { Hitpoints = 80 }, GridPosition.FrontBottom, false)
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier2"), soldier) with { Hitpoints = 4 }, GridPosition.FrontCenter, false)
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier3"), soldier) with { Hitpoints = 0 }, GridPosition.FrontTop, false)
            .Build();

        Assert.That(engine.State.Fortifications[city.Id], Is.EqualTo(city with
        {
            Garrison = riotedGarrison,
            RiotTurns = 0
        }));
    }

    [Test]
    public void VisitingStackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 20, leadership: 6) with
            {
                Regeneration = 5
            };
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        WorldCity city = TestHelpers.SimpleCity(
                new WorldFortificationId("city"),
                cityDefinition,
                player1.Id,
                new WorldPosition(3, 3),
                UnitGrid<WorldUnit>.Empty) with
            {
                RiotTurns = 1
            };

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            player1.Id,
            city.Entrance,
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader) with { Hitpoints = 50 }, GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 5);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Mage))
            .WithPlayer(player1)
            .WithCity(city)
            .WithStack(stack)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(),
        [
            new WorldTurnStartingEvent(player1.Id),
            new WorldStackMovementRestoredEvent(stack.Id, 20),
            new WorldCityRiotEndedEvent(city.Id),
            new WorldStackMovementConsumedEvent(stack.Id, 0),
            new WorldUnitDamagedEvent(new WorldUnitGroup.Stack(stack.Id), new WorldUnitId("leader"), 40, SourcePlayerId: null),
            new WorldTurnStartedEvent(player1.Id),
            new WorldTurnResourcesGeneratedEvent(player1.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0))
        ]);

        UnitGrid<WorldUnit> riotedUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader) with { Hitpoints = 40 }, GridPosition.FrontBottom, false)
            .Build();

        Assert.That(engine.State.Stacks[stack.Id], Is.EqualTo(stack with
        {
            Units = riotedUnits,
            MovementPoints = 0
        }));
        Assert.That(engine.State.Fortifications[city.Id], Is.EqualTo(city with { RiotTurns = 0 }));
    }

    [Test]
    public void CaptureDoesNotRiotTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            ".....",
            ".....",
            ".....",
            "^^^.."
        ]);

        WorldPlayer attacker = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer defender = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 2, leadership: 6);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            attacker.Id,
            new WorldPosition(4, 3),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 2);

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty);
        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            defender.Id,
            new WorldPosition(3, 3),
            UnitGrid<WorldUnit>.Empty);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(attacker.LordId, attacker.Race, LordCategory.Warrior))
            .WithPlayer(attacker)
            .WithDefinition(TestHelpers.SimpleLord(defender.LordId, defender.Race, LordCategory.Warrior))
            .WithPlayer(defender)
            .WithStack(stack)
            .WithCity(city)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(new WorldMoveStackCommand(stack.Id, city.Entrance), [
            new WorldStackMovingEvent(stack.Id, new WorldPosition(4, 3), city.Entrance, WorldStackDirection.NorthWest, 2, [], null),
            new WorldFortificationEnteredEvent(new WorldStackId("stack"), new WorldFortificationId("city")),
            new WorldFortificationCapturingEvent(city.Id, defender.Id, attacker.Id, stack.Id),
            new WorldFortificationCapturedEvent(city.Id, defender.Id, attacker.Id, stack.Id),
            new WorldStackMovedEvent(stack.Id, new WorldPosition(4, 3), city.Entrance, WorldStackDirection.NorthWest, 2, [], null)
        ]);

        Assert.That(engine.State.Fortifications[city.Id], Is.EqualTo(city with { OwnerId = attacker.Id }));
    }

    [Test]
    public void CaptureEndsRiotTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            ".....",
            ".....",
            ".....",
            "^^^.."
        ]);

        WorldPlayer attacker = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer defender = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 2, leadership: 6);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            attacker.Id,
            new WorldPosition(4, 3),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 2);

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty);
        WorldCity city = TestHelpers.SimpleCity(
                new WorldFortificationId("city"),
                cityDefinition,
                defender.Id,
                new WorldPosition(3, 3),
                UnitGrid<WorldUnit>.Empty) with
            {
                RiotTurns = 3
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(attacker.LordId, attacker.Race, LordCategory.Warrior))
            .WithPlayer(attacker)
            .WithDefinition(TestHelpers.SimpleLord(defender.LordId, defender.Race, LordCategory.Warrior))
            .WithPlayer(defender)
            .WithStack(stack)
            .WithCity(city)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(new WorldMoveStackCommand(stack.Id, city.Entrance), [
            new WorldStackMovingEvent(stack.Id, new WorldPosition(4, 3), city.Entrance, WorldStackDirection.NorthWest, 2, [], null),
            new WorldFortificationEnteredEvent(new WorldStackId("stack"), new WorldFortificationId("city")),
            new WorldFortificationCapturingEvent(city.Id, defender.Id, attacker.Id, stack.Id),
            new WorldCityRiotEndedEvent(city.Id),
            new WorldFortificationCapturedEvent(city.Id, defender.Id, attacker.Id, stack.Id),
            new WorldStackMovedEvent(stack.Id, new WorldPosition(4, 3), city.Entrance, WorldStackDirection.NorthWest, 2, [], null)
        ]);

        Assert.That(engine.State.Fortifications[city.Id], Is.EqualTo(city with
        {
            OwnerId = attacker.Id,
            RiotTurns = 0
        }));
    }

    [Test]
    public void ThiefActionTest()
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
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("thief"), thief), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            player2.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldier), GridPosition.FrontBottom, false)
                .Build());

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithRandom(new TestSequenceRandomProvider(2, 0, 0))
            .WithDefinition(thief)
            .WithDefinition(soldier)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.RiotCity, 100))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Guildmaster))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(thiefStack)
            .WithCity(city)
            .Build();

        engine.CheckCommand(
            new WorldThiefActionCommand(thiefStack.Id, new WorldThiefRiotAction(city.Entrance)),
            [
                new WorldThiefActionSucceededEvent(thiefStack.Id, WorldThiefActionType.RiotCity, player2.Id),
                new WorldCityRiotStartedEvent(city.Id, 3),
                new WorldStackMovementConsumedEvent(thiefStack.Id, 10)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Fortifications = engine.State.Fortifications.SetItem(city.Id, city with { RiotTurns = 3 }),
                Stacks = engine.State.Stacks.SetItem(thiefStack.Id, thiefStack with { MovementPoints = 10 })
            }));
    }

    [Test]
    public void ThiefActionAgainstCapitalTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition soldier = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 5);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("capital"),
            player2.Id,
            new WorldPosition(4, 4),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldier), GridPosition.FrontBottom, false)
                .Build());

        WorldStack thiefStack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player1.Id,
            new WorldPosition(5, 4),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("thief"), thief), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(soldier)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.RiotCity, 100))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Guildmaster))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(thiefStack)
            .WithCapital(capital)
            .Build();

        engine.CheckCommandError(
            new WorldThiefActionCommand(thiefStack.Id, new WorldThiefRiotAction(capital.Entrance)),
            WorldCommandError.ThiefTargetInvalid);
    }

    [Test]
    public void CityActionsTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 6);
        UnitDefinition soldier = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 5);
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        WorldCity city = TestHelpers.SimpleCity(
                new WorldFortificationId("city"),
                cityDefinition,
                player1.Id,
                new WorldPosition(3, 3),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldier) with { Hitpoints = 50 }, GridPosition.FrontBottom, false)
                    .Build()) with
            {
                RiotTurns = 2
            };

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            player1.Id,
            city.Entrance,
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(soldier)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Mage))
            .WithPlayer(player1)
            .WithCity(city)
            .WithStack(stack)
            .Build();

        engine.CheckCommandError(
            new WorldUpgradeCityCommand(city.Id),
            WorldCommandError.CityRioting);

        engine.CheckCommandError(
            new WorldHealUnitCommand(new WorldUnitGroup.Fortification(city.Id), new WorldUnitId("soldier")),
            WorldCommandError.CityRioting);

        engine.CheckCommandError(
            new WorldTransferUnitCommand(
                new WorldUnitGroup.Fortification(city.Id),
                new WorldUnitGroup.Stack(stack.Id),
                new WorldUnitId("soldier"),
                GridPosition.BackBottom),
            WorldCommandError.CityRioting);

        engine.CheckCommandError(
            new WorldRepositionUnitCommand(
                new WorldUnitGroup.Fortification(city.Id),
                new WorldUnitId("soldier"),
                GridPosition.BackBottom),
            WorldCommandError.CityRioting);

        engine.CheckCommandError(
            new WorldRepositionUnitCommand(
                new WorldUnitGroup.Stack(stack.Id),
                new WorldUnitId("leader"),
                GridPosition.BackBottom),
            WorldCommandError.CityRioting);

        engine.CheckCommandError(
            new WorldDisbandUnitCommand(new WorldUnitGroup.Stack(stack.Id), new WorldUnitId("leader")),
            WorldCommandError.CityRioting);

        engine.CheckCommandError(
            new WorldDisbandUnitCommand(new WorldUnitGroup.Fortification(city.Id), new WorldUnitId("soldier")),
            WorldCommandError.CityRioting);

        engine.CheckCommandError(
            new WorldMoveStackCommand(stack.Id, new WorldPosition(1, 1)),
            WorldCommandError.CityRioting);
    }

    [Test]
    public void MoveIntoCityTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 6);
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        WorldCity city = TestHelpers.SimpleCity(
                new WorldFortificationId("city"),
                cityDefinition,
                player1.Id,
                new WorldPosition(3, 3),
                UnitGrid<WorldUnit>.Empty) with
            {
                RiotTurns = 2
            };

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            player1.Id,
            new WorldPosition(4, 3),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 20);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Mage))
            .WithPlayer(player1)
            .WithCity(city)
            .WithStack(stack)
            .Build();

        engine.CheckCommandError(
            new WorldMoveStackCommand(stack.Id, city.Entrance),
            WorldCommandError.CityRioting);
    }

    [Test]
    public void ThiefActionsWithheldTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack thiefStack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player1.Id,
            new WorldPosition(4, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("thief"), thief), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        WorldCity city = TestHelpers.SimpleCity(
                new WorldFortificationId("city"),
                cityDefinition,
                player2.Id,
                new WorldPosition(3, 3),
                UnitGrid<WorldUnit>.Empty) with
            {
                RiotTurns = 2
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.RiotCity, 100))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Guildmaster))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(thiefStack)
            .WithCity(city)
            .Build();

        WorldGetThiefActionsCommand.Result result = engine.CheckCommand(
            new WorldGetThiefActionsCommand(thiefStack.Id, city.Entrance),
            []);

        Assert.That(result, Is.EqualTo(new WorldGetThiefActionsCommand.Result([])));

        engine.CheckCommandError(
            new WorldThiefActionCommand(thiefStack.Id, new WorldThiefRiotAction(city.Entrance)),
            WorldCommandError.ThiefTargetInvalid);
    }

    [Test]
    public void RiotEndsTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition soldier = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 5) with
        {
            Regeneration = 5
        };
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        WorldCity city = TestHelpers.SimpleCity(
                new WorldFortificationId("city"),
                cityDefinition,
                player1.Id,
                new WorldPosition(3, 3),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldier), GridPosition.FrontBottom, false)
                    .Build()) with
            {
                RiotTurns = 2
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(soldier)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Mage))
            .WithPlayer(player1)
            .WithCity(city)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(),
        [
            new WorldTurnStartingEvent(new WorldPlayerId(1)),
            new WorldCityRiotTurnsDecreasedEvent(new WorldFortificationId("city"), 1),
            new WorldTurnStartedEvent(new WorldPlayerId(1)),
            new WorldTurnResourcesGeneratedEvent(new WorldPlayerId(1), new WorldResourceSet(100, 0, 0, 25, 0, 0))
        ]);

        engine.CheckCommand(new WorldEndTurnCommand(),
        [
            new WorldTurnEndedEvent(new WorldPlayerId(1)),
            new WorldRoundEndedEvent(),
            new WorldRoundStartedEvent(),
            new WorldTurnStartingEvent(new WorldPlayerId(1)),
            new WorldCityRiotEndedEvent(new WorldFortificationId("city")),
            new WorldUnitDamagedEvent(new WorldUnitGroup.Fortification(new WorldFortificationId("city")), new WorldUnitId("soldier"), 80, SourcePlayerId: null),
            new WorldTurnStartedEvent(new WorldPlayerId(1)),
            new WorldTurnResourcesGeneratedEvent(new WorldPlayerId(1), new WorldResourceSet(100, 0, 0, 25, 0, 0))
        ]);

        engine.CheckCommand(new WorldEndTurnCommand(),
        [
            new WorldTurnEndedEvent(new WorldPlayerId(1)),
            new WorldRoundEndedEvent(),
            new WorldRoundStartedEvent(),
            new WorldTurnStartingEvent(new WorldPlayerId(1)),
            new WorldTurnStartedEvent(new WorldPlayerId(1)),
            new WorldTurnResourcesGeneratedEvent(new WorldPlayerId(1), new WorldResourceSet(100, 0, 0, 25, 0, 0))
        ]);

        UnitGrid<WorldUnit> healedGarrison = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldier) with { Hitpoints = 95 }, GridPosition.FrontBottom, false)
            .Build();

        Assert.That(engine.State.Fortifications[city.Id], Is.EqualTo(city with
        {
            Garrison = healedGarrison,
            RiotTurns = 0
        }));
    }

    [Test]
    public void NeutralCityTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition soldier = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 5) with
        {
            Regeneration = 5
        };
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldPlayer neutral = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Neutral, map) with { IsAi = true };

        WorldCity city = TestHelpers.SimpleCity(
                new WorldFortificationId("city"),
                cityDefinition,
                neutral.Id,
                new WorldPosition(3, 3),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldier), GridPosition.FrontBottom, false)
                    .Build()) with
            {
                RiotTurns = 1
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(soldier)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Mage))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(neutral.LordId, neutral.Race, LordCategory.Mage))
            .WithPlayer(neutral)
            .WithCity(city)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(new WorldEndTurnCommand(),
        [
            new WorldTurnEndedEvent(player1.Id),
            new WorldTurnStartingEvent(neutral.Id),
            new WorldCityRiotEndedEvent(city.Id),
            new WorldUnitDamagedEvent(new WorldUnitGroup.Fortification(city.Id), new WorldUnitId("soldier"), 80, SourcePlayerId: null),
            new WorldTurnStartedEvent(neutral.Id),
            new WorldTurnResourcesGeneratedEvent(neutral.Id, WorldResourceSet.Empty)
        ]);

        UnitGrid<WorldUnit> riotedGarrison = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldier) with { Hitpoints = 80 }, GridPosition.FrontBottom, false)
            .Build();

        Assert.That(engine.State.Fortifications[city.Id], Is.EqualTo(city with
        {
            Garrison = riotedGarrison,
            RiotTurns = 0
        }));
    }

    [Test]
    public void RegenerationTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 20, leadership: 6) with
            {
                Regeneration = 5
            };
        UnitDefinition soldier = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 5) with
        {
            Regeneration = 5
        };
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        WorldCity city = TestHelpers.SimpleCity(
                new WorldFortificationId("city"),
                cityDefinition,
                player1.Id,
                new WorldPosition(3, 3),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldier) with { Hitpoints = 50 }, GridPosition.FrontBottom, false)
                    .Build()) with
            {
                RiotTurns = 2
            };

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            player1.Id,
            city.Entrance,
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader) with { Hitpoints = 50 }, GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 5);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(soldier)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Mage))
            .WithPlayer(player1)
            .WithCity(city)
            .WithStack(stack)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(),
        [
            new WorldTurnStartingEvent(player1.Id),
            new WorldStackMovementRestoredEvent(stack.Id, 20),
            new WorldCityRiotTurnsDecreasedEvent(city.Id, 1),
            new WorldTurnStartedEvent(player1.Id),
            new WorldTurnResourcesGeneratedEvent(player1.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0))
        ]);

        Assert.That(engine.State.Stacks[stack.Id], Is.EqualTo(stack with { MovementPoints = 20 }));
        Assert.That(engine.State.Fortifications[city.Id], Is.EqualTo(city with { RiotTurns = 1 }));
    }

    [Test]
    public void LethalDamageTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition soldier = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 5);
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        WorldCity city = TestHelpers.SimpleCity(
                new WorldFortificationId("city"),
                cityDefinition,
                player1.Id,
                new WorldPosition(3, 3),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldier) with { Hitpoints = 60 }, GridPosition.FrontBottom, false)
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier2"), soldier) with { Hitpoints = 1 }, GridPosition.FrontCenter, false)
                    .Build()) with
            {
                RiotTurns = 1
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithGlobals(new GlobalVariables { RiotDamagePercent = 100 })
            .WithDefinition(soldier)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Mage))
            .WithPlayer(player1)
            .WithCity(city)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(),
        [
            new WorldTurnStartingEvent(player1.Id),
            new WorldCityRiotEndedEvent(city.Id),
            new WorldUnitDamagedEvent(new WorldUnitGroup.Fortification(city.Id), new WorldUnitId("soldier"), 1, SourcePlayerId: null),
            new WorldTurnStartedEvent(player1.Id),
            new WorldTurnResourcesGeneratedEvent(player1.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0))
        ]);

        UnitGrid<WorldUnit> riotedGarrison = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldier) with { Hitpoints = 1 }, GridPosition.FrontBottom, false)
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier2"), soldier) with { Hitpoints = 1 }, GridPosition.FrontCenter, false)
            .Build();

        Assert.That(engine.State.Fortifications[city.Id], Is.EqualTo(city with
        {
            Garrison = riotedGarrison,
            RiotTurns = 0
        }));
    }

    [Test]
    public void StackExchangeTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 6);
        UnitDefinition soldier = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 5);
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        WorldCity city = TestHelpers.SimpleCity(
                new WorldFortificationId("city"),
                cityDefinition,
                player1.Id,
                new WorldPosition(3, 3),
                UnitGrid<WorldUnit>.Empty) with
            {
                RiotTurns = 2
            };

        WorldStack visitingStack = TestHelpers.SimpleStack(
                new WorldStackId("visiting"),
                player1.Id,
                city.Entrance,
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader), GridPosition.FrontBottom, false)
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldier), GridPosition.FrontCenter, false)
                    .Build(),
                movementPoints: 20) with
            {
                Inventory = new WorldInventory([new WorldItem(new WorldItemId("Relic"), new ItemDefinitionId("relic"), Charges: null)])
            };

        WorldStack outsideStack = TestHelpers.SimpleStack(
                new WorldStackId("outside"),
                player1.Id,
                new WorldPosition(4, 3),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader2"), leader), GridPosition.FrontBottom, false)
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier2"), soldier), GridPosition.FrontCenter, false)
                    .Build(),
                movementPoints: 20) with
            {
                Inventory = new WorldInventory([new WorldItem(new WorldItemId("Orb"), new ItemDefinitionId("orb"), Charges: null)])
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(soldier)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Mage))
            .WithPlayer(player1)
            .WithCity(city)
            .WithStack(visitingStack)
            .WithStack(outsideStack)
            .Build();

        engine.CheckCommandError(
            new WorldTransferUnitCommand(
                new WorldUnitGroup.Stack(visitingStack.Id),
                new WorldUnitGroup.Stack(outsideStack.Id),
                new WorldUnitId("soldier"),
                GridPosition.BackBottom),
            WorldCommandError.CityRioting);

        engine.CheckCommandError(
            new WorldTransferUnitCommand(
                new WorldUnitGroup.Stack(outsideStack.Id),
                new WorldUnitGroup.Stack(visitingStack.Id),
                new WorldUnitId("soldier2"),
                GridPosition.BackBottom),
            WorldCommandError.CityRioting);

        engine.CheckCommandError(
            new WorldTransferInventoryItemCommand(
                new WorldUnitGroup.Stack(visitingStack.Id),
                new WorldUnitGroup.Stack(outsideStack.Id),
                new WorldItemId("Relic")),
            WorldCommandError.CityRioting);

        engine.CheckCommandError(
            new WorldTransferInventoryItemCommand(
                new WorldUnitGroup.Stack(outsideStack.Id),
                new WorldUnitGroup.Stack(visitingStack.Id),
                new WorldItemId("Orb")),
            WorldCommandError.CityRioting);
    }
}
