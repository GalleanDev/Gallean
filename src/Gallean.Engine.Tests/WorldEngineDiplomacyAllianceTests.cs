namespace Gallean.Engine.Tests;

public sealed class WorldEngineDiplomacyAllianceTests
{
    private static WorldRelation BuildRelation(bool allied, int current)
    {
        return new WorldRelation(
            new WorldRelationId(WorldRace.Undead, WorldRace.Heretic),
            current,
            current,
            allied,
            AllianceTurn: 0,
            PermanentWar: false,
            PermanentAlliance: false);
    }

    [Test]
    public void AlliedStackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer otherPlayer = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        WorldStack otherStack = TestHelpers.SimpleStack(
            new WorldStackId("Stack2"),
            otherPlayer.Id,
            new WorldPosition(1, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader2"), leader),
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(otherPlayer.LordId, otherPlayer.Race, LordCategory.Warrior))
            .WithPlayer(otherPlayer)
            .WithRelation(BuildRelation(allied: true, current: 100))
            .WithStack(stack)
            .WithStack(otherStack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(
            new WorldMoveStackCommand(stack.Id, new WorldPosition(1, 0)),
            WorldCommandError.TargetOccupied);
    }

    [Test]
    public void AlliedStackPassedTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 2);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer otherPlayer = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        WorldStack otherStack = TestHelpers.SimpleStack(
            new WorldStackId("Stack2"),
            otherPlayer.Id,
            new WorldPosition(1, 1),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader2"), leader),
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(otherPlayer.LordId, otherPlayer.Race, LordCategory.Warrior))
            .WithPlayer(otherPlayer)
            .WithRelation(BuildRelation(allied: true, current: 100))
            .WithStack(stack)
            .WithStack(otherStack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldMoveStackCommand(stack.Id, new WorldPosition(2, 0)),
            [
                new WorldStackMovingEvent(
                    StackId: stack.Id,
                    SourcePosition: new WorldPosition(0, 0),
                    TargetPosition: new WorldPosition(1, 0),
                    NewDirection: WorldStackDirection.SouthEast,
                    NewMovementPoints: 4,
                    RemainingPath: [new WorldPosition(2, 0)], null),
                new WorldStackMovedEvent(
                    StackId: stack.Id,
                    SourcePosition: new WorldPosition(0, 0),
                    TargetPosition: new WorldPosition(1, 0),
                    NewDirection: WorldStackDirection.SouthEast,
                    NewMovementPoints: 4,
                    RemainingPath: [new WorldPosition(2, 0)], null),
                new WorldStackMovingEvent(
                    StackId: stack.Id,
                    SourcePosition: new WorldPosition(1, 0),
                    TargetPosition: new WorldPosition(2, 0),
                    NewDirection: WorldStackDirection.SouthEast,
                    NewMovementPoints: 2,
                    RemainingPath: [], null),
                new WorldStackMovedEvent(
                    StackId: stack.Id,
                    SourcePosition: new WorldPosition(1, 0),
                    TargetPosition: new WorldPosition(2, 0),
                    NewDirection: WorldStackDirection.SouthEast,
                    NewMovementPoints: 2,
                    RemainingPath: [], null)
            ]);
    }

    [Test]
    public void NeutralRelationStackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer otherPlayer = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        WorldStack otherStack = TestHelpers.SimpleStack(
            new WorldStackId("Stack2"),
            otherPlayer.Id,
            new WorldPosition(1, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader2"), leader),
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(otherPlayer.LordId, otherPlayer.Race, LordCategory.Warrior))
            .WithPlayer(otherPlayer)
            .WithRelation(BuildRelation(allied: false, current: 49))
            .WithStack(stack)
            .WithStack(otherStack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        BattleUnit attackerUnit = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), new WorldUnitId("leader"), leader.Id, BattleSide.Attacker, hitpoints: 100);

        BattleUnit defenderUnit = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), new WorldUnitId("leader2"), leader.Id, BattleSide.Defender, hitpoints: 100);

        BattleState expectedBattleState = new(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder().Place(attackerUnit, GridPosition.FrontBottom, false).Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder().Place(defenderUnit, GridPosition.FrontBottom, false).Build(),
            CurrentUnitTurn: new BattleUnitTurn(new BattleUnitId(0), BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: null,
            Modifiers: BattleModifiers.None);

        engine.CheckCommand(
            new WorldMoveStackCommand(stack.Id, new WorldPosition(1, 0)),
            [
                new WorldStackMovementConsumedEvent(stack.Id, 3),
                new WorldStackBattleStartedEvent(stack.Id, otherStack.Id, otherStack.Position, expectedBattleState, IsDuel: false),
                new WorldRelationChangedEvent(player.Id, otherPlayer.Id, 42)
            ]);
    }

    [Test]
    public void AlliedCityTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer otherPlayer = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Heretic, 1),
            WorldResourceSet.Empty);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(5, 4),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("City1"),
            cityDefinition,
            otherPlayer.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(otherPlayer.LordId, otherPlayer.Race, LordCategory.Warrior))
            .WithPlayer(otherPlayer)
            .WithRelation(BuildRelation(allied: true, current: 100))
            .WithStack(stack)
            .WithCity(city)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldMoveStackCommand(stack.Id, city.Entrance),
            [
                new WorldStackMovingEvent(
                    StackId: stack.Id,
                    SourcePosition: new WorldPosition(5, 4),
                    TargetPosition: city.Entrance,
                    NewDirection: WorldStackDirection.NorthWest,
                    NewMovementPoints: 6,
                    RemainingPath: [], null),
                new WorldFortificationEnteredEvent(new WorldStackId("Stack1"), new WorldFortificationId("City1")),
                new WorldStackMovedEvent(
                    StackId: stack.Id,
                    SourcePosition: new WorldPosition(5, 4),
                    TargetPosition: city.Entrance,
                    NewDirection: WorldStackDirection.NorthWest,
                    NewMovementPoints: 6,
                    RemainingPath: [], null)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Position = city.Entrance,
                    Direction = WorldStackDirection.NorthWest,
                    MovementPoints = 6
                }),
                Fortifications = engine.State.Fortifications.SetItem(city.Id, city)
            }));
    }

    [Test]
    public void AlliedGarrisonedCityTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer otherPlayer = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        UnitDefinition guard = TestHelpers.SimpleUnitDefinition("guard", maxHitpoints: 100, initiative: 50, damage: 10);
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Heretic, 1),
            WorldResourceSet.Empty);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(6, 3),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("City1"),
            cityDefinition,
            otherPlayer.Id,
            new WorldPosition(3, 3),
            TestHelpers.SingleUnitGrid(new WorldUnitId("guard"), guard));

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(guard)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(otherPlayer.LordId, otherPlayer.Race, LordCategory.Warrior))
            .WithPlayer(otherPlayer)
            .WithRelation(BuildRelation(allied: true, current: 100))
            .WithStack(stack)
            .WithCity(city)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldMoveStackCommand(stack.Id, city.Entrance),
            [
                new WorldStackMovingEvent(
                    StackId: stack.Id,
                    SourcePosition: new WorldPosition(6, 3),
                    TargetPosition: new WorldPosition(5, 2),
                    NewDirection: WorldStackDirection.North,
                    NewMovementPoints: 4,
                    RemainingPath: [new WorldPosition(4, 2), new WorldPosition(3, 3)], null),
                new WorldStackMovedEvent(
                    StackId: stack.Id,
                    SourcePosition: new WorldPosition(6, 3),
                    TargetPosition: new WorldPosition(5, 2),
                    NewDirection: WorldStackDirection.North,
                    NewMovementPoints: 4,
                    RemainingPath: [new WorldPosition(4, 2), new WorldPosition(3, 3)], null),
                new WorldStackMovingEvent(
                    StackId: stack.Id,
                    SourcePosition: new WorldPosition(5, 2),
                    TargetPosition: new WorldPosition(4, 2),
                    NewDirection: WorldStackDirection.NorthWest,
                    NewMovementPoints: 2,
                    RemainingPath: [new WorldPosition(3, 3)], null),
                new WorldStackMovedEvent(
                    StackId: stack.Id,
                    SourcePosition: new WorldPosition(5, 2),
                    TargetPosition: new WorldPosition(4, 2),
                    NewDirection: WorldStackDirection.NorthWest,
                    NewMovementPoints: 2,
                    RemainingPath: [new WorldPosition(3, 3)], null),
                new WorldStackMovingEvent(
                    StackId: stack.Id,
                    SourcePosition: new WorldPosition(4, 2),
                    TargetPosition: new WorldPosition(3, 3),
                    NewDirection: WorldStackDirection.West,
                    NewMovementPoints: 2,
                    RemainingPath: [], null),
                new WorldFortificationEnteredEvent(new WorldStackId("Stack1"), new WorldFortificationId("City1")),
                new WorldStackMovedEvent(
                    StackId: stack.Id,
                    SourcePosition: new WorldPosition(4, 2),
                    TargetPosition: new WorldPosition(3, 3),
                    NewDirection: WorldStackDirection.West,
                    NewMovementPoints: 2,
                    RemainingPath: [], null)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Position = city.Entrance,
                    Direction = WorldStackDirection.West,
                    MovementPoints = 2
                }),
                Fortifications = engine.State.Fortifications.SetItem(city.Id, city)
            }));
    }
}
