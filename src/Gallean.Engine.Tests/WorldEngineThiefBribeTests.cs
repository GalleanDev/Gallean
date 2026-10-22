namespace Gallean.Engine.Tests;

public sealed class WorldEngineThiefBribeTests
{
    private static UnitDefinition Guard(string id, int experienceKilled)
    {
        return TestHelpers.SimpleUnitDefinition(id, maxHitpoints: 100, initiative: 40, damage: 5) with
        {
            ExperienceKilled = experienceKilled
        };
    }

    private static WorldStack ThiefStack(WorldUnitId thiefId, WorldPlayerId ownerId, WorldPosition position, UnitDefinition thief)
    {
        return TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            ownerId,
            position,
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(thiefId, thief), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);
    }

    private static WorldCity City(WorldCityDefinition definition, WorldPlayerId ownerId, UnitGrid<WorldUnit> garrison)
    {
        return TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            definition,
            ownerId,
            new WorldPosition(3, 3),
            garrison);
    }

    private static UnitGrid<WorldUnit> Garrison(params (WorldUnitId Id, UnitDefinition Definition)[] units)
    {
        UnitGrid<WorldUnit>.Builder builder = new();
        GridPosition[] positions = [GridPosition.FrontBottom, GridPosition.FrontCenter, GridPosition.FrontTop];

        for (int index = 0; index < units.Length; index++)
        {
            builder.Place(TestHelpers.SimpleUnit(units[index].Id, units[index].Definition), positions[index], false);
        }

        return builder.Build();
    }

    [Test]
    public void CityTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6, 'H');

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition guard = Guard("guard", experienceKilled: 50);
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map) with
        {
            Resources = new WorldResourceSet(1000, 0, 0, 0, 0, 0)
        };
        WorldPlayer neutral = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Neutral, map);

        WorldStack thiefStack = ThiefStack(new WorldUnitId("thief"), player.Id, new WorldPosition(4, 3), thief);
        WorldCity city = City(cityDefinition, neutral.Id, Garrison((new WorldUnitId("guard"), guard), (new WorldUnitId("guard2"), guard)));

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithRandom(new TestSequenceRandomProvider(0, 0, 2))
            .WithDefinition(thief)
            .WithDefinition(guard)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Bribe, 100))
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Guildmaster))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(neutral.LordId, neutral.Race, LordCategory.Warrior))
            .WithPlayer(neutral)
            .WithStack(thiefStack)
            .WithCity(city)
            .Build();

        WorldState before = engine.State;

        engine.CheckCommand(
            new WorldThiefActionCommand(thiefStack.Id, new WorldThiefBribeAction(new WorldPosition(3, 3))),
            [
                new WorldThiefActionSucceededEvent(thiefStack.Id, WorldThiefActionType.Bribe, neutral.Id),
                new WorldCityBribedEvent(thiefStack.Id, city.Id, new WorldResourceSet(300, 0, 0, 0, 0, 0)),
                new WorldFortificationCapturingEvent(city.Id, neutral.Id, player.Id, thiefStack.Id),
                new WorldFortificationCapturedEvent(city.Id, neutral.Id, player.Id, thiefStack.Id),
                new WorldStackMovementConsumedEvent(thiefStack.Id, 10)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            before with
            {
                Players = before.Players.SetItem(player.Id, player with { Resources = new WorldResourceSet(700, 0, 0, 0, 0, 0) }),
                Stacks = before.Stacks.SetItem(thiefStack.Id, thiefStack with { MovementPoints = 10 }),
                Fortifications = before.Fortifications.SetItem(
                    city.Id,
                    city with { OwnerId = player.Id, Garrison = UnitGrid<WorldUnit>.Empty })
            }));
    }

    [Test]
    public void CityLevelTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6, 'H');

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition guard = Guard("guard", experienceKilled: 50);
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 4), WorldResourceSet.Empty);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map) with
        {
            Resources = new WorldResourceSet(1000, 0, 0, 0, 0, 0)
        };
        WorldPlayer neutral = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Neutral, map);

        WorldStack thiefStack = ThiefStack(new WorldUnitId("thief"), player.Id, new WorldPosition(4, 3), thief);
        WorldCity city = City(cityDefinition, neutral.Id, Garrison((new WorldUnitId("guard"), guard)));

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithRandom(new TestSequenceRandomProvider(0, 0, 2))
            .WithDefinition(thief)
            .WithDefinition(guard)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Bribe, 100))
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Guildmaster))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(neutral.LordId, neutral.Race, LordCategory.Warrior))
            .WithPlayer(neutral)
            .WithStack(thiefStack)
            .WithCity(city)
            .Build();

        WorldState before = engine.State;

        engine.CheckCommand(
            new WorldThiefActionCommand(thiefStack.Id, new WorldThiefBribeAction(new WorldPosition(3, 3))),
            [
                new WorldThiefActionSucceededEvent(thiefStack.Id, WorldThiefActionType.Bribe, neutral.Id),
                new WorldCityBribedEvent(thiefStack.Id, city.Id, new WorldResourceSet(600, 0, 0, 0, 0, 0)),
                new WorldFortificationCapturingEvent(city.Id, neutral.Id, player.Id, thiefStack.Id),
                new WorldFortificationCapturedEvent(city.Id, neutral.Id, player.Id, thiefStack.Id),
                new WorldStackMovementConsumedEvent(thiefStack.Id, 10)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            before with
            {
                Players = before.Players.SetItem(player.Id, player with { Resources = new WorldResourceSet(400, 0, 0, 0, 0, 0) }),
                Stacks = before.Stacks.SetItem(thiefStack.Id, thiefStack with { MovementPoints = 10 }),
                Fortifications = before.Fortifications.SetItem(
                    city.Id,
                    city with { OwnerId = player.Id, Garrison = UnitGrid<WorldUnit>.Empty })
            }));
    }

    [Test]
    public void OccupyingStackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6, 'H');

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition guard = Guard("guard", experienceKilled: 50);
        UnitDefinition defender = TestHelpers.SimpleLeaderDefinition(
            Guard("defender", experienceKilled: 200),
            movementPoints: 6, leadership: 6);
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map) with
        {
            Resources = new WorldResourceSet(1000, 0, 0, 0, 0, 0)
        };
        WorldPlayer neutral = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Neutral, map);

        WorldStack thiefStack = ThiefStack(new WorldUnitId("thief"), player.Id, new WorldPosition(4, 3), thief);
        WorldCity city = City(cityDefinition, neutral.Id, Garrison((new WorldUnitId("guard"), guard)));
        WorldStack occupant = TestHelpers.SimpleStack(
            new WorldStackId("occupant"),
            neutral.Id,
            city.Entrance,
            Garrison((new WorldUnitId("defender"), defender)),
            movementPoints: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithRandom(new TestSequenceRandomProvider(0, 0, 2))
            .WithDefinition(thief)
            .WithDefinition(guard)
            .WithDefinition(defender)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Bribe, 100))
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Guildmaster))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(neutral.LordId, neutral.Race, LordCategory.Warrior))
            .WithPlayer(neutral)
            .WithStack(thiefStack)
            .WithStack(occupant)
            .WithCity(city)
            .Build();

        WorldState before = engine.State;

        engine.CheckCommand(
            new WorldThiefActionCommand(thiefStack.Id, new WorldThiefBribeAction(new WorldPosition(2, 2))),
            [
                new WorldThiefActionSucceededEvent(thiefStack.Id, WorldThiefActionType.Bribe, neutral.Id),
                new WorldStackDestroyedEvent(occupant.Id, new WorldUnitGroup.Stack(thiefStack.Id), thiefStack.OwnerId),
                new WorldCityBribedEvent(thiefStack.Id, city.Id, new WorldResourceSet(750, 0, 0, 0, 0, 0)),
                new WorldFortificationCapturingEvent(city.Id, neutral.Id, player.Id, thiefStack.Id),
                new WorldFortificationCapturedEvent(city.Id, neutral.Id, player.Id, thiefStack.Id),
                new WorldStackMovementConsumedEvent(thiefStack.Id, 10)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            before with
            {
                Players = before.Players.SetItem(player.Id, player with { Resources = new WorldResourceSet(250, 0, 0, 0, 0, 0) }),
                Stacks = before.Stacks
                    .Remove(occupant.Id)
                    .SetItem(thiefStack.Id, thiefStack with { MovementPoints = 10 }),
                ScenarioState = before.ScenarioState with
                {
                    DestroyedStacks = before.ScenarioState.DestroyedStacks.Add(new ScenarioDestroyedStack(occupant.Id, thiefStack.OwnerId, SourceTemplateId: null))
                },
                Fortifications = before.Fortifications.SetItem(
                    city.Id,
                    city with { OwnerId = player.Id, Garrison = UnitGrid<WorldUnit>.Empty })
            }));
    }

    [Test]
    public void InsufficientGoldTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6, 'H');

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition guard = Guard("guard", experienceKilled: 50);
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map) with
        {
            Resources = new WorldResourceSet(149, 0, 0, 0, 0, 0)
        };
        WorldPlayer neutral = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Neutral, map);

        WorldStack thiefStack = ThiefStack(new WorldUnitId("thief"), player.Id, new WorldPosition(4, 3), thief);
        WorldCity city = City(cityDefinition, neutral.Id, Garrison((new WorldUnitId("guard"), guard)));

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(guard)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Bribe, 100))
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Guildmaster))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(neutral.LordId, neutral.Race, LordCategory.Warrior))
            .WithPlayer(neutral)
            .WithStack(thiefStack)
            .WithCity(city)
            .Build();

        engine.CheckCommandError(
            new WorldThiefActionCommand(thiefStack.Id, new WorldThiefBribeAction(new WorldPosition(3, 3))),
            WorldCommandError.InsufficientResources);
    }

    [Test]
    public void PlayerCityTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6, 'H');

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition guard = Guard("guard", experienceKilled: 50);
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map) with
        {
            Resources = new WorldResourceSet(1000, 0, 0, 0, 0, 0)
        };
        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack thiefStack = ThiefStack(new WorldUnitId("thief"), player.Id, new WorldPosition(4, 3), thief);
        WorldCity city = City(cityDefinition, enemy.Id, Garrison((new WorldUnitId("guard"), guard)));

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(guard)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Bribe, 100))
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Guildmaster))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(enemy.LordId, enemy.Race, LordCategory.Warrior))
            .WithPlayer(enemy)
            .WithStack(thiefStack)
            .WithCity(city)
            .Build();

        engine.CheckCommandError(
            new WorldThiefActionCommand(thiefStack.Id, new WorldThiefBribeAction(new WorldPosition(3, 3))),
            WorldCommandError.ThiefTargetInvalid);
    }

    [Test]
    public void EmptyCityTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6, 'H');

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map) with
        {
            Resources = new WorldResourceSet(1000, 0, 0, 0, 0, 0)
        };
        WorldPlayer neutral = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Neutral, map);

        WorldStack thiefStack = ThiefStack(new WorldUnitId("thief"), player.Id, new WorldPosition(4, 3), thief);
        WorldCity city = City(cityDefinition, neutral.Id, UnitGrid<WorldUnit>.Empty);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Bribe, 100))
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Guildmaster))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(neutral.LordId, neutral.Race, LordCategory.Warrior))
            .WithPlayer(neutral)
            .WithStack(thiefStack)
            .WithCity(city)
            .Build();

        engine.CheckCommandError(
            new WorldThiefActionCommand(thiefStack.Id, new WorldThiefBribeAction(new WorldPosition(3, 3))),
            WorldCommandError.ThiefTargetInvalid);
    }

    [Test]
    public void CapitalTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6, 'H');

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition guard = Guard("guard", experienceKilled: 50);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map) with
        {
            Resources = new WorldResourceSet(1000, 0, 0, 0, 0, 0)
        };
        WorldPlayer neutral = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Neutral, map);

        WorldStack thiefStack = ThiefStack(new WorldUnitId("thief"), player.Id, new WorldPosition(5, 4), thief);
        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("capital"),
            neutral.Id,
            new WorldPosition(4, 4),
            Garrison((new WorldUnitId("guard"), guard)));

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(guard)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Bribe, 100))
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Guildmaster))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(neutral.LordId, neutral.Race, LordCategory.Warrior))
            .WithPlayer(neutral)
            .WithStack(thiefStack)
            .WithCapital(capital)
            .Build();

        engine.CheckCommandError(
            new WorldThiefActionCommand(thiefStack.Id, new WorldThiefBribeAction(new WorldPosition(4, 4))),
            WorldCommandError.ThiefTargetInvalid);
    }
}
