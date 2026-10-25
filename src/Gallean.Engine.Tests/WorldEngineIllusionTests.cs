namespace Gallean.Engine.Tests;

public sealed class WorldEngineIllusionTests
{
    [Test]
    public void DestroyTest()
    {
        WorldMap map = TestHelpers.SimpleMap(4, 2);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 11, leadership: 6);
        UnitDefinition illusion = TestHelpers.SimpleUnitDefinition("illusion", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Illusion,
            Leadership = 1,
            MovementPoints = 20,
            Lifetime = 3
        };
        WorldItem item = new(new WorldItemId("Item"), new ItemDefinitionId("item"), Charges: null);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            player1.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 11);

        WorldStack illusionStack = TestHelpers.SimpleStack(
                new WorldStackId("illusion"),
                player2.Id,
                new WorldPosition(3, 0),
                TestHelpers.SingleUnitGrid(new WorldUnitId("illusion"), illusion),
                movementPoints: 20) with
            {
                Inventory = new WorldInventory([item]),
                ExpirationTurn = 3
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(illusion)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(stack)
            .WithStack(illusionStack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        WorldState before = engine.State;

        engine.CheckCommand(
            new WorldMoveStackCommand(stack.Id, illusionStack.Position),
            [
                new WorldStackMovingEvent(stack.Id, new WorldPosition(0, 0), new WorldPosition(1, 0), WorldStackDirection.SouthEast, 9, [new WorldPosition(2, 0)], illusionStack.Position),
                new WorldStackMovedEvent(stack.Id, new WorldPosition(0, 0), new WorldPosition(1, 0), WorldStackDirection.SouthEast, 9, [new WorldPosition(2, 0)], illusionStack.Position),
                new WorldStackMovingEvent(stack.Id, new WorldPosition(1, 0), new WorldPosition(2, 0), WorldStackDirection.SouthEast, 7, [], illusionStack.Position),
                new WorldStackMovedEvent(stack.Id, new WorldPosition(1, 0), new WorldPosition(2, 0), WorldStackDirection.SouthEast, 7, [], illusionStack.Position),
                new WorldStackDestroyedEvent(illusionStack.Id, new WorldUnitGroup.Stack(stack.Id), player1.Id),
                new WorldStackMovementConsumedEvent(stack.Id, 1)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            before with
            {
                Stacks = before.Stacks
                    .Remove(illusionStack.Id)
                    .SetItem(stack.Id, stack with
                    {
                        Position = new WorldPosition(2, 0),
                        Direction = WorldStackDirection.SouthEast,
                        MovementPoints = 1,
                        Inventory = new WorldInventory([item])
                    }),
                ScenarioState = before.ScenarioState with
                {
                    DestroyedStacks = [new ScenarioDestroyedStack(illusionStack.Id, player1.Id, SourceTemplateId: null)]
                }
            }));
    }

    [Test]
    public void DestroyAdjacentTest()
    {
        WorldMap map = TestHelpers.SimpleMap(4, 2);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 7, leadership: 6);
        UnitDefinition illusion = TestHelpers.SimpleUnitDefinition("illusion", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Illusion,
            Leadership = 1,
            MovementPoints = 20,
            Lifetime = 3
        };

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            player1.Id,
            new WorldPosition(2, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 7);

        WorldStack illusionStack = TestHelpers.SimpleStack(
                new WorldStackId("illusion"),
                player2.Id,
                new WorldPosition(3, 0),
                TestHelpers.SingleUnitGrid(new WorldUnitId("illusion"), illusion),
                movementPoints: 20) with
            {
                ExpirationTurn = 3
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(illusion)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(stack)
            .WithStack(illusionStack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        WorldState before = engine.State;

        engine.CheckCommand(
            new WorldMoveStackCommand(stack.Id, illusionStack.Position),
            [
                new WorldStackDestroyedEvent(illusionStack.Id, new WorldUnitGroup.Stack(stack.Id), player1.Id),
                new WorldStackMovementConsumedEvent(stack.Id, 3)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            before with
            {
                Stacks = before.Stacks
                    .Remove(illusionStack.Id)
                    .SetItem(stack.Id, stack with { MovementPoints = 3 }),
                ScenarioState = before.ScenarioState with
                {
                    DestroyedStacks = [new ScenarioDestroyedStack(illusionStack.Id, player1.Id, SourceTemplateId: null)]
                }
            }));
    }

    [Test]
    public void GetPathTest()
    {
        WorldMap map = TestHelpers.SimpleMap(4, 2);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 11, leadership: 6);
        UnitDefinition illusion = TestHelpers.SimpleUnitDefinition("illusion", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Illusion,
            Leadership = 1,
            MovementPoints = 20,
            Lifetime = 3
        };

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            player1.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 11);

        WorldStack illusionStack = TestHelpers.SimpleStack(
                new WorldStackId("illusion"),
                player2.Id,
                new WorldPosition(3, 0),
                TestHelpers.SingleUnitGrid(new WorldUnitId("illusion"), illusion),
                movementPoints: 20) with
            {
                ExpirationTurn = 3
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(illusion)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(stack)
            .WithStack(illusionStack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        WorldGetPathCommand.Result result = engine.CheckCommand(
            new WorldGetPathCommand(stack.Id, illusionStack.Position),
            []);

        Assert.That(result, Is.EqualTo(new WorldGetPathCommand.Result(
            new WorldPath(
            [
                new WorldPathSegment(new WorldPosition(0, 0), new WorldPosition(1, 0), 2, Turn: 0),
                new WorldPathSegment(new WorldPosition(1, 0), new WorldPosition(2, 0), 2, Turn: 0)
            ]),
            new WorldPath(
            [
                new WorldPathSegment(new WorldPosition(2, 0), new WorldPosition(3, 0), 2, Turn: 0)
            ]),
            new WorldPathEncounter.Battle(new WorldPosition(3, 0)))));
    }

    [Test]
    public void GetPathAsThiefTest()
    {
        WorldMap map = TestHelpers.SimpleMap(4, 2);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition illusion = TestHelpers.SimpleUnitDefinition("illusion", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Illusion,
            Leadership = 1,
            MovementPoints = 20,
            Lifetime = 3
        };

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack thiefStack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player1.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("thief"), thief),
            movementPoints: 20);

        WorldStack illusionStack = TestHelpers.SimpleStack(
                new WorldStackId("illusion"),
                player2.Id,
                new WorldPosition(3, 0),
                TestHelpers.SingleUnitGrid(new WorldUnitId("illusion"), illusion),
                movementPoints: 20) with
            {
                ExpirationTurn = 3
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(illusion)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(thiefStack)
            .WithStack(illusionStack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        WorldGetPathCommand.Result result = engine.CheckCommand(
            new WorldGetPathCommand(thiefStack.Id, illusionStack.Position),
            []);

        Assert.That(result, Is.EqualTo(new WorldGetPathCommand.Result(
            new WorldPath(
            [
                new WorldPathSegment(new WorldPosition(0, 0), new WorldPosition(1, 0), 2, Turn: 0),
                new WorldPathSegment(new WorldPosition(1, 0), new WorldPosition(2, 0), 2, Turn: 0)
            ]),
            new WorldPath(
            [
                new WorldPathSegment(new WorldPosition(2, 0), new WorldPosition(3, 0), 2, Turn: 0)
            ]),
            new WorldPathEncounter.Battle(new WorldPosition(3, 0)))));
    }

    [Test]
    public void ThiefDestroyTest()
    {
        WorldMap map = TestHelpers.SimpleMap(4, 2);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition illusion = TestHelpers.SimpleUnitDefinition("illusion", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Illusion,
            Leadership = 1,
            MovementPoints = 20,
            Lifetime = 3
        };

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack thiefStack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player1.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("thief"), thief),
            movementPoints: 20);

        WorldStack illusionStack = TestHelpers.SimpleStack(
                new WorldStackId("illusion"),
                player2.Id,
                new WorldPosition(3, 0),
                TestHelpers.SingleUnitGrid(new WorldUnitId("illusion"), illusion),
                movementPoints: 20) with
            {
                ExpirationTurn = 3
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(illusion)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(thiefStack)
            .WithStack(illusionStack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        WorldState before = engine.State;

        engine.CheckCommand(
            new WorldMoveStackCommand(thiefStack.Id, illusionStack.Position),
            [
                new WorldStackMovingEvent(thiefStack.Id, new WorldPosition(0, 0), new WorldPosition(1, 0), WorldStackDirection.SouthEast, 18, [new WorldPosition(2, 0)], illusionStack.Position),
                new WorldStackMovedEvent(thiefStack.Id, new WorldPosition(0, 0), new WorldPosition(1, 0), WorldStackDirection.SouthEast, 18, [new WorldPosition(2, 0)], illusionStack.Position),
                new WorldStackMovingEvent(thiefStack.Id, new WorldPosition(1, 0), new WorldPosition(2, 0), WorldStackDirection.SouthEast, 16, [], illusionStack.Position),
                new WorldStackMovedEvent(thiefStack.Id, new WorldPosition(1, 0), new WorldPosition(2, 0), WorldStackDirection.SouthEast, 16, [], illusionStack.Position),
                new WorldStackDestroyedEvent(illusionStack.Id, new WorldUnitGroup.Stack(thiefStack.Id), player1.Id),
                new WorldStackMovementConsumedEvent(thiefStack.Id, 6)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            before with
            {
                Stacks = before.Stacks
                    .Remove(illusionStack.Id)
                    .SetItem(thiefStack.Id, thiefStack with
                    {
                        Position = new WorldPosition(2, 0),
                        Direction = WorldStackDirection.SouthEast,
                        MovementPoints = 6
                    }),
                ScenarioState = before.ScenarioState with
                {
                    DestroyedStacks = [new ScenarioDestroyedStack(illusionStack.Id, player1.Id, SourceTemplateId: null)]
                }
            }));
    }

    [Test]
    public void ThiefDestroyAdjacentTest()
    {
        WorldMap map = TestHelpers.SimpleMap(4, 2);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition illusion = TestHelpers.SimpleUnitDefinition("illusion", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Illusion,
            Leadership = 1,
            MovementPoints = 20,
            Lifetime = 3
        };

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack thiefStack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player1.Id,
            new WorldPosition(2, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("thief"), thief),
            movementPoints: 20);

        WorldStack illusionStack = TestHelpers.SimpleStack(
                new WorldStackId("illusion"),
                player2.Id,
                new WorldPosition(3, 0),
                TestHelpers.SingleUnitGrid(new WorldUnitId("illusion"), illusion),
                movementPoints: 20) with
            {
                ExpirationTurn = 3
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(illusion)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(thiefStack)
            .WithStack(illusionStack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        WorldState before = engine.State;

        engine.CheckCommand(
            new WorldMoveStackCommand(thiefStack.Id, illusionStack.Position),
            [
                new WorldStackDestroyedEvent(illusionStack.Id, new WorldUnitGroup.Stack(thiefStack.Id), player1.Id),
                new WorldStackMovementConsumedEvent(thiefStack.Id, 10)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            before with
            {
                Stacks = before.Stacks
                    .Remove(illusionStack.Id)
                    .SetItem(thiefStack.Id, thiefStack with { MovementPoints = 10 }),
                ScenarioState = before.ScenarioState with
                {
                    DestroyedStacks = [new ScenarioDestroyedStack(illusionStack.Id, player1.Id, SourceTemplateId: null)]
                }
            }));
    }

    [Test]
    public void ThiefPastTest()
    {
        WorldMap map = TestHelpers.SimpleMap(4, 2);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition illusion = TestHelpers.SimpleUnitDefinition("illusion", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Illusion,
            Leadership = 1,
            MovementPoints = 20,
            Lifetime = 3
        };

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack thiefStack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player1.Id,
            new WorldPosition(0, 1),
            TestHelpers.SingleUnitGrid(new WorldUnitId("thief"), thief),
            movementPoints: 20);

        WorldStack illusionStack = TestHelpers.SimpleStack(
                new WorldStackId("illusion"),
                player2.Id,
                new WorldPosition(2, 0),
                TestHelpers.SingleUnitGrid(new WorldUnitId("illusion"), illusion),
                movementPoints: 20) with
            {
                ExpirationTurn = 3
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(illusion)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(thiefStack)
            .WithStack(illusionStack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        WorldState before = engine.State;

        engine.CheckCommand(
            new WorldMoveStackCommand(thiefStack.Id, new WorldPosition(3, 1)),
            [
                new WorldStackMovingEvent(thiefStack.Id, new WorldPosition(0, 1), new WorldPosition(1, 1), WorldStackDirection.SouthEast, 18, [], new WorldPosition(2, 0)),
                new WorldStackMovedEvent(thiefStack.Id, new WorldPosition(0, 1), new WorldPosition(1, 1), WorldStackDirection.SouthEast, 18, [], new WorldPosition(2, 0)),
                new WorldStackDestroyedEvent(illusionStack.Id, new WorldUnitGroup.Stack(thiefStack.Id), player1.Id),
                new WorldStackMovementConsumedEvent(thiefStack.Id, 8)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            before with
            {
                Stacks = before.Stacks
                    .Remove(illusionStack.Id)
                    .SetItem(thiefStack.Id, thiefStack with
                    {
                        Position = new WorldPosition(1, 1),
                        Direction = WorldStackDirection.SouthEast,
                        MovementPoints = 8
                    }),
                ScenarioState = before.ScenarioState with
                {
                    DestroyedStacks = [new ScenarioDestroyedStack(illusionStack.Id, player1.Id, SourceTemplateId: null)]
                }
            }));
    }

    [Test]
    public void ThiefActionsTest()
    {
        WorldMap map = TestHelpers.SimpleMap(4, 2);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition illusion = TestHelpers.SimpleUnitDefinition("illusion", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Illusion,
            Leadership = 1,
            MovementPoints = 20,
            Lifetime = 3
        };

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack thiefStack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player1.Id,
            new WorldPosition(2, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("thief"), thief),
            movementPoints: 20);

        WorldStack illusionStack = TestHelpers.SimpleStack(
                new WorldStackId("illusion"),
                player2.Id,
                new WorldPosition(3, 0),
                TestHelpers.SingleUnitGrid(new WorldUnitId("illusion"), illusion),
                movementPoints: 20) with
            {
                ExpirationTurn = 3
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(illusion)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.PoisonStack, 90))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Spy, 100))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Duel, 100))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(thiefStack)
            .WithStack(illusionStack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        WorldGetThiefActionsCommand.Result result = engine.CheckCommand(
            new WorldGetThiefActionsCommand(thiefStack.Id, illusionStack.Position),
            []);

        Assert.That(result, Is.EqualTo(new WorldGetThiefActionsCommand.Result([])));
    }

    [Test]
    public void ThiefDuelTest()
    {
        WorldMap map = TestHelpers.SimpleMap(4, 2);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition illusion = TestHelpers.SimpleUnitDefinition("illusion", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Illusion,
            Leadership = 1,
            MovementPoints = 20,
            Lifetime = 3
        };

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack thiefStack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player1.Id,
            new WorldPosition(2, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("thief"), thief),
            movementPoints: 20);

        WorldStack illusionStack = TestHelpers.SimpleStack(
                new WorldStackId("illusion"),
                player2.Id,
                new WorldPosition(3, 0),
                TestHelpers.SingleUnitGrid(new WorldUnitId("illusion"), illusion),
                movementPoints: 20) with
            {
                ExpirationTurn = 3
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(illusion)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Duel, 100))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(thiefStack)
            .WithStack(illusionStack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(
            new WorldThiefActionCommand(thiefStack.Id, new WorldThiefDuelAction(illusionStack.Position)),
            WorldCommandError.ThiefTargetInvalid);
    }

    [Test]
    public void AttackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(4, 2);

        UnitDefinition illusion = TestHelpers.SimpleUnitDefinition("illusion", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Illusion,
            Leadership = 1,
            MovementPoints = 15,
            Lifetime = 3
        };
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack illusionStack = TestHelpers.SimpleStack(
                new WorldStackId("illusion"),
                player1.Id,
                new WorldPosition(2, 0),
                TestHelpers.SingleUnitGrid(new WorldUnitId("illusion"), illusion),
                movementPoints: 15) with
            {
                ExpirationTurn = 3
            };

        WorldStack enemyStack = TestHelpers.SimpleStack(
            new WorldStackId("enemy"),
            player2.Id,
            new WorldPosition(3, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(illusion)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(illusionStack)
            .WithStack(enemyStack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        WorldState before = engine.State;

        engine.CheckCommand(
            new WorldMoveStackCommand(illusionStack.Id, enemyStack.Position),
            [new WorldStackMovementConsumedEvent(illusionStack.Id, 7)]);

        Assert.That(engine.State, Is.EqualTo(
            before with
            {
                Stacks = before.Stacks.SetItem(illusionStack.Id, before.Stacks[illusionStack.Id] with { MovementPoints = 7 })
            }));
    }

    [Test]
    public void CaptureForbiddenTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^",
            "^^^^^",
            "^^^^^",
            "^^^.."
        ]);

        WorldPlayer attacker = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer defender = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);
        UnitDefinition illusion = TestHelpers.SimpleUnitDefinition("illusion", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Illusion,
            Leadership = 1,
            MovementPoints = 2,
            Lifetime = 3
        };
        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("stack"),
                attacker.Id,
                new WorldPosition(4, 3),
                TestHelpers.SingleUnitGrid(new WorldUnitId("illusion"), illusion),
                movementPoints: 2) with
            {
                ExpirationTurn = 3
            };
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty);
        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            defender.Id,
            new WorldPosition(3, 3),
            UnitGrid<WorldUnit>.Empty);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(illusion)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(attacker.LordId, attacker.Race, LordCategory.Warrior))
            .WithPlayer(attacker)
            .WithDefinition(TestHelpers.SimpleLord(defender.LordId, defender.Race, LordCategory.Warrior))
            .WithPlayer(defender)
            .WithStack(stack)
            .WithCity(city)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(
            new WorldMoveStackCommand(stack.Id, city.Entrance),
            WorldCommandError.FortificationCaptureForbidden);
    }

    [Test]
    public void LifetimeTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        UnitDefinition illusion = TestHelpers.SimpleUnitDefinition("illusion", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Illusion,
            Leadership = 1,
            MovementPoints = 20,
            Lifetime = 3
        };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("illusion"),
                player.Id,
                new WorldPosition(1, 1),
                TestHelpers.SingleUnitGrid(new WorldUnitId("illusion"), illusion),
                movementPoints: 20) with
            {
                ExpirationTurn = 1
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(illusion)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        WorldState before = engine.State;

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldStackMovementRestoredEvent(stack.Id, 20),
            new WorldStackDestroyedEvent(stack.Id, Destroyer: null, DestroyerPlayerId: null),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 25, 0, 0, 0, 0))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = before.Stacks.Remove(stack.Id),
                Tombs = before.Tombs
            }));
    }
}
