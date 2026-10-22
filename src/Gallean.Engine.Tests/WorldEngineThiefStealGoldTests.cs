namespace Gallean.Engine.Tests;

public sealed class WorldEngineThiefStealGoldTests
{
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

    private static WorldCity City(WorldUnitId soldierId, WorldCityDefinition definition, WorldPlayerId ownerId, UnitDefinition soldier)
    {
        return TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            definition,
            ownerId,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(soldierId, soldier), GridPosition.FrontBottom, false)
                .Build());
    }

    [Test]
    public void CityTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition soldier = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 5);
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0)
        };
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map) with
        {
            Resources = new WorldResourceSet(1000, 0, 0, 0, 0, 0)
        };

        WorldStack thiefStack = ThiefStack(new WorldUnitId("thief"), player1.Id, new WorldPosition(4, 3), thief);
        WorldCity city = City(new WorldUnitId("soldier"), cityDefinition, player2.Id, soldier);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithRandom(new TestSequenceRandomProvider(199, 0, 0))
            .WithDefinition(thief)
            .WithDefinition(soldier)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.StealGold, 80))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Guildmaster))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(thiefStack)
            .WithCity(city)
            .Build();

        WorldState before = engine.State;

        engine.CheckCommand(
            new WorldThiefActionCommand(thiefStack.Id, new WorldThiefStealGoldAction(new WorldPosition(3, 3))),
            [
                new WorldThiefActionSucceededEvent(thiefStack.Id, WorldThiefActionType.StealGold, player2.Id),
                new WorldResourcesStolenEvent(thiefStack.Id, player2.Id, new WorldResourceSet(200, 0, 0, 0, 0, 0)),
                new WorldStackMovementConsumedEvent(thiefStack.Id, 10)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            before with
            {
                Players = before.Players
                    .SetItem(player1.Id, player1 with { Resources = new WorldResourceSet(300, 0, 0, 0, 0, 0) })
                    .SetItem(player2.Id, player2 with { Resources = new WorldResourceSet(800, 0, 0, 0, 0, 0) }),
                Stacks = before.Stacks.SetItem(thiefStack.Id, thiefStack with { MovementPoints = 10 })
            }));
    }

    [Test]
    public void NoGoldTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition soldier = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 5);
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack thiefStack = ThiefStack(new WorldUnitId("thief"), player1.Id, new WorldPosition(4, 3), thief);
        WorldCity city = City(new WorldUnitId("soldier"), cityDefinition, player2.Id, soldier);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithRandom(new TestSequenceRandomProvider(0, 0, 0))
            .WithDefinition(thief)
            .WithDefinition(soldier)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.StealGold, 80))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Guildmaster))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(thiefStack)
            .WithCity(city)
            .Build();

        WorldState before = engine.State;

        engine.CheckCommand(
            new WorldThiefActionCommand(thiefStack.Id, new WorldThiefStealGoldAction(new WorldPosition(3, 3))),
            [
                new WorldThiefActionSucceededEvent(thiefStack.Id, WorldThiefActionType.StealGold, player2.Id),
                new WorldResourcesStolenEvent(thiefStack.Id, player2.Id, WorldResourceSet.Empty),
                new WorldStackMovementConsumedEvent(thiefStack.Id, 10)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            before with
            {
                Stacks = before.Stacks.SetItem(thiefStack.Id, thiefStack with { MovementPoints = 10 })
            }));
    }

    [Test]
    public void NeutralCityTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition soldier = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 5);
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer neutral = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Neutral, map);

        WorldStack thiefStack = ThiefStack(new WorldUnitId("thief"), player1.Id, new WorldPosition(4, 3), thief);
        WorldCity city = City(new WorldUnitId("soldier"), cityDefinition, neutral.Id, soldier);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(soldier)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.StealGold, 80))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Guildmaster))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(neutral.LordId, neutral.Race, LordCategory.Warrior))
            .WithPlayer(neutral)
            .WithStack(thiefStack)
            .WithCity(city)
            .Build();

        engine.CheckCommandError(
            new WorldThiefActionCommand(thiefStack.Id, new WorldThiefStealGoldAction(new WorldPosition(3, 3))),
            WorldCommandError.ThiefTargetInvalid);
    }

    [Test]
    public void FailedRollTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition soldier = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 5);
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map) with
        {
            Resources = new WorldResourceSet(1000, 0, 0, 0, 0, 0)
        };

        WorldStack thiefStack = ThiefStack(new WorldUnitId("thief"), player1.Id, new WorldPosition(4, 3), thief);
        WorldCity city = City(new WorldUnitId("soldier"), cityDefinition, player2.Id, soldier);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithRandom(new TestSequenceRandomProvider(199, 80, 90))
            .WithDefinition(thief)
            .WithDefinition(soldier)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.StealGold, 80))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Guildmaster))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(thiefStack)
            .WithCity(city)
            .Build();

        WorldState before = engine.State;

        engine.CheckCommand(
            new WorldThiefActionCommand(thiefStack.Id, new WorldThiefStealGoldAction(new WorldPosition(3, 3))),
            [
                new WorldThiefActionFailedEvent(thiefStack.Id, WorldThiefActionType.StealGold, player2.Id),
                new WorldStackDestroyedEvent(thiefStack.Id, null, null)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            before with
            {
                Stacks = before.Stacks.Remove(thiefStack.Id),
                ScenarioState = before.ScenarioState with
                {
                    DestroyedStacks = before.ScenarioState.DestroyedStacks.Add(new ScenarioDestroyedStack(thiefStack.Id, DestroyerPlayerId: null, SourceTemplateId: null))
                }
            }));
    }

    [Test]
    public void OwnCityTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition soldier = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 5);
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(1000, 0, 0, 0, 0, 0)
        };

        WorldStack thiefStack = ThiefStack(new WorldUnitId("thief"), player1.Id, new WorldPosition(4, 3), thief);
        WorldCity city = City(new WorldUnitId("soldier"), cityDefinition, player1.Id, soldier);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(soldier)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.StealGold, 80))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Guildmaster))
            .WithPlayer(player1)
            .WithStack(thiefStack)
            .WithCity(city)
            .Build();

        engine.CheckCommandError(
            new WorldThiefActionCommand(thiefStack.Id, new WorldThiefStealGoldAction(new WorldPosition(3, 3))),
            WorldCommandError.ThiefTargetInvalid);
    }

    [Test]
    public void NotAdjacentTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition soldier = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 5);
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map) with
        {
            Resources = new WorldResourceSet(1000, 0, 0, 0, 0, 0)
        };

        WorldStack thiefStack = ThiefStack(new WorldUnitId("thief"), player1.Id, new WorldPosition(5, 5), thief);
        WorldCity city = City(new WorldUnitId("soldier"), cityDefinition, player2.Id, soldier);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(soldier)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.StealGold, 80))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Guildmaster))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(thiefStack)
            .WithCity(city)
            .Build();

        engine.CheckCommandError(
            new WorldThiefActionCommand(thiefStack.Id, new WorldThiefStealGoldAction(new WorldPosition(3, 3))),
            WorldCommandError.ThiefTargetInvalid);
    }

    [Test]
    public void StackTargetTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition warlord = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("warlord", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 10, leadership: 2);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map) with
        {
            Resources = new WorldResourceSet(1000, 0, 0, 0, 0, 0)
        };

        WorldStack thiefStack = ThiefStack(new WorldUnitId("thief"), player1.Id, new WorldPosition(2, 2), thief);

        WorldStack targetStack = TestHelpers.SimpleStack(
            new WorldStackId("enemy"),
            player2.Id,
            new WorldPosition(3, 2),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("warlord"), warlord), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 10);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(warlord)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.StealGold, 80))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Guildmaster))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(thiefStack)
            .WithStack(targetStack)
            .Build();

        engine.CheckCommandError(
            new WorldThiefActionCommand(thiefStack.Id, new WorldThiefStealGoldAction(new WorldPosition(3, 2))),
            WorldCommandError.ThiefTargetInvalid);
    }

    [Test]
    public void WarriorLordTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition soldier = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 5);
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map) with
        {
            Resources = new WorldResourceSet(1000, 0, 0, 0, 0, 0)
        };

        WorldStack thiefStack = ThiefStack(new WorldUnitId("thief"), player1.Id, new WorldPosition(4, 3), thief);
        WorldCity city = City(new WorldUnitId("soldier"), cityDefinition, player2.Id, soldier);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(soldier)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.StealGold, 80))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(thiefStack)
            .WithCity(city)
            .Build();

        engine.CheckCommandError(
            new WorldThiefActionCommand(thiefStack.Id, new WorldThiefStealGoldAction(new WorldPosition(3, 3))),
            WorldCommandError.ThiefActionNotAllowed);
    }
}
