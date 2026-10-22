namespace Gallean.Engine.Tests;

public sealed class WorldEngineThiefCounterfeitOrdersTests
{
    [Test]
    public void SmallUnitsTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition first = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("first", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 10, leadership: 2);
        UnitDefinition second = TestHelpers.SimpleUnitDefinition("second", maxHitpoints: 100, initiative: 40, damage: 5);
        UnitDefinition third = TestHelpers.SimpleUnitDefinition("third", maxHitpoints: 100, initiative: 40, damage: 5);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack thiefStack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player1.Id,
            new WorldPosition(2, 2),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("thief"), thief), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        WorldStack targetStack = TestHelpers.SimpleStack(
            new WorldStackId("enemy"),
            player2.Id,
            new WorldPosition(3, 2),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("first"), first), GridPosition.FrontBottom, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("second"), second), GridPosition.FrontCenter, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("third"), third), GridPosition.BackBottom, false)
                .Build(),
            movementPoints: 10);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithRandom(new TestSequenceRandomProvider(0, 0, 0, 0, 0, 0, 0, 0, 0))
            .WithDefinition(thief)
            .WithDefinition(first)
            .WithDefinition(second)
            .WithDefinition(third)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.CounterfeitOrders, 90))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Guildmaster))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Guildmaster))
            .WithPlayer(player2)
            .WithStack(thiefStack)
            .WithStack(targetStack)
            .Build();

        UnitGrid<WorldUnit> shuffled = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("first"), first), GridPosition.BackCenter, false)
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("second"), second), GridPosition.FrontTop, false)
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("third"), third), GridPosition.BackTop, false)
            .Build();

        engine.CheckCommand(
            new WorldThiefActionCommand(thiefStack.Id, new WorldThiefCounterfeitOrdersAction(new WorldPosition(3, 2))),
            [
                new WorldThiefActionSucceededEvent(thiefStack.Id, WorldThiefActionType.CounterfeitOrders, player2.Id),
                new WorldUnitsShuffledEvent(new WorldUnitGroup.Stack(targetStack.Id), shuffled),
                new WorldStackMovementConsumedEvent(thiefStack.Id, 10)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks
                    .SetItem(targetStack.Id, targetStack with { Units = shuffled })
                    .SetItem(thiefStack.Id, thiefStack with { MovementPoints = 10 })
            }));
    }

    [Test]
    public void LargeUnitTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition giant = TestHelpers.SimpleUnitDefinition("giant", maxHitpoints: 200, initiative: 30, damage: 20) with
        {
            IsLarge = true
        };
        UnitDefinition first = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("first", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 10, leadership: 2);
        UnitDefinition second = TestHelpers.SimpleUnitDefinition("second", maxHitpoints: 100, initiative: 40, damage: 5);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack thiefStack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player1.Id,
            new WorldPosition(2, 2),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("thief"), thief), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        WorldStack targetStack = TestHelpers.SimpleStack(
            new WorldStackId("enemy"),
            player2.Id,
            new WorldPosition(3, 2),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("giant"), giant), GridPosition.FrontBottom, true)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("first"), first), GridPosition.FrontCenter, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("second"), second), GridPosition.FrontTop, false)
                .Build(),
            movementPoints: 10);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithRandom(new TestSequenceRandomProvider(0, 0, 0, 0, 0, 0, 0))
            .WithDefinition(thief)
            .WithDefinition(giant)
            .WithDefinition(first)
            .WithDefinition(second)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.CounterfeitOrders, 90))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Guildmaster))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Guildmaster))
            .WithPlayer(player2)
            .WithStack(thiefStack)
            .WithStack(targetStack)
            .Build();

        UnitGrid<WorldUnit> shuffled = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("giant"), giant), GridPosition.FrontCenter, true)
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("first"), first), GridPosition.BackTop, false)
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("second"), second), GridPosition.FrontBottom, false)
            .Build();

        engine.CheckCommand(
            new WorldThiefActionCommand(thiefStack.Id, new WorldThiefCounterfeitOrdersAction(new WorldPosition(3, 2))),
            [
                new WorldThiefActionSucceededEvent(thiefStack.Id, WorldThiefActionType.CounterfeitOrders, player2.Id),
                new WorldUnitsShuffledEvent(new WorldUnitGroup.Stack(targetStack.Id), shuffled),
                new WorldStackMovementConsumedEvent(thiefStack.Id, 10)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks
                    .SetItem(targetStack.Id, targetStack with { Units = shuffled })
                    .SetItem(thiefStack.Id, thiefStack with { MovementPoints = 10 })
            }));
    }

    [Test]
    public void FailedRollTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition warlord = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("warlord", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 10, leadership: 2);
        UnitDefinition soldier = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 50, initiative: 30, damage: 5);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack thiefStack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player1.Id,
            new WorldPosition(2, 2),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("thief"), thief), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        WorldStack targetStack = TestHelpers.SimpleStack(
            new WorldStackId("enemy"),
            player2.Id,
            new WorldPosition(3, 2),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("warlord"), warlord), GridPosition.FrontBottom, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldier), GridPosition.BackBottom, false)
                .Build(),
            movementPoints: 10);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithRandom(new TestSequenceRandomProvider(0, 0, 0, 0, 0, 0, 0, 84, 98))
            .WithDefinition(thief)
            .WithDefinition(warlord)
            .WithDefinition(soldier)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.CounterfeitOrders, 90))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Guildmaster))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Guildmaster))
            .WithPlayer(player2)
            .WithStack(thiefStack)
            .WithStack(targetStack)
            .Build();

        WorldState before = engine.State;

        engine.CheckCommand(
            new WorldThiefActionCommand(thiefStack.Id, new WorldThiefCounterfeitOrdersAction(new WorldPosition(3, 2))),
            [
                new WorldThiefActionFailedEvent(thiefStack.Id, WorldThiefActionType.CounterfeitOrders, player2.Id),
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
    public void NotAdjacentTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

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
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("thief"), thief), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        WorldStack targetStack = TestHelpers.SimpleStack(
            new WorldStackId("enemy"),
            player2.Id,
            new WorldPosition(5, 5),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 10);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.CounterfeitOrders, 90))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Guildmaster))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Guildmaster))
            .WithPlayer(player2)
            .WithStack(thiefStack)
            .WithStack(targetStack)
            .Build();

        engine.CheckCommandError(
            new WorldThiefActionCommand(thiefStack.Id, new WorldThiefCounterfeitOrdersAction(new WorldPosition(5, 5))),
            WorldCommandError.ThiefTargetInvalid);
    }

    [Test]
    public void OwnStackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 40, damage: 5),
            movementPoints: 6, leadership: 6);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldStack thiefStack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player1.Id,
            new WorldPosition(2, 2),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("thief"), thief), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        WorldStack allyStack = TestHelpers.SimpleStack(
            new WorldStackId("ally"),
            player1.Id,
            new WorldPosition(3, 2),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 10);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.CounterfeitOrders, 90))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Guildmaster))
            .WithPlayer(player1)
            .WithStack(thiefStack)
            .WithStack(allyStack)
            .Build();

        engine.CheckCommandError(
            new WorldThiefActionCommand(thiefStack.Id, new WorldThiefCounterfeitOrdersAction(new WorldPosition(3, 2))),
            WorldCommandError.ThiefTargetInvalid);
    }

    [Test]
    public void CityTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition first = TestHelpers.SimpleUnitDefinition("first", maxHitpoints: 100, initiative: 50, damage: 10);
        UnitDefinition second = TestHelpers.SimpleUnitDefinition("second", maxHitpoints: 100, initiative: 40, damage: 5);
        UnitDefinition third = TestHelpers.SimpleUnitDefinition("third", maxHitpoints: 100, initiative: 40, damage: 5);
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
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("first"), first), GridPosition.FrontBottom, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("second"), second), GridPosition.FrontCenter, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("third"), third), GridPosition.BackBottom, false)
                .Build());

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithRandom(new TestSequenceRandomProvider(0, 0, 0, 0, 0, 0, 0, 0, 0))
            .WithDefinition(thief)
            .WithDefinition(first)
            .WithDefinition(second)
            .WithDefinition(third)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.CounterfeitOrders, 90))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Guildmaster))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(thiefStack)
            .WithCity(city)
            .Build();

        UnitGrid<WorldUnit> shuffled = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("first"), first), GridPosition.BackCenter, false)
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("second"), second), GridPosition.FrontTop, false)
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("third"), third), GridPosition.BackTop, false)
            .Build();

        engine.CheckCommand(
            new WorldThiefActionCommand(thiefStack.Id, new WorldThiefCounterfeitOrdersAction(city.Entrance)),
            [
                new WorldThiefActionSucceededEvent(thiefStack.Id, WorldThiefActionType.CounterfeitOrders, player2.Id),
                new WorldUnitsShuffledEvent(new WorldUnitGroup.Fortification(city.Id), shuffled),
                new WorldStackMovementConsumedEvent(thiefStack.Id, 10)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Fortifications = engine.State.Fortifications.SetItem(city.Id, city with { Garrison = shuffled }),
                Stacks = engine.State.Stacks.SetItem(thiefStack.Id, thiefStack with { MovementPoints = 10 })
            }));
    }

    [Test]
    public void SingleUnitTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition warlord = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("warlord", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 10, leadership: 2);
        UnitDefinition soldier = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 5);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack thiefStack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player1.Id,
            new WorldPosition(2, 2),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("thief"), thief), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        WorldStack targetStack = TestHelpers.SimpleStack(
            new WorldStackId("enemy"),
            player2.Id,
            new WorldPosition(3, 2),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("warlord"), warlord), GridPosition.FrontBottom, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldier) with { Hitpoints = 0 }, GridPosition.BackBottom, false)
                .Build(),
            movementPoints: 10);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(warlord)
            .WithDefinition(soldier)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.CounterfeitOrders, 90))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Guildmaster))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(thiefStack)
            .WithStack(targetStack)
            .Build();

        engine.CheckCommandError(
            new WorldThiefActionCommand(thiefStack.Id, new WorldThiefCounterfeitOrdersAction(new WorldPosition(3, 2))),
            WorldCommandError.ThiefTargetInvalid);
    }
}
