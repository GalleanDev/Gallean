using System.Collections.Immutable;

namespace Gallean.Engine.Tests;

public sealed class WorldEngineRemoveRodTests
{
    private static UnitDefinition RodLeaderDefinition()
    {
        return TestHelpers.SimpleUnitDefinition("rod-leader", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Rod,
            LeaderAbilities = LeaderAbility.Rod,
            Leadership = 1,
            MovementPoints = 20
        };
    }

    [Test]
    public void SuccessTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition rodLeaderDefinition = RodLeaderDefinition();
        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("rod-leader"),
            player1.Id,
            new WorldPosition(2, 2),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("rodLeader"), rodLeaderDefinition), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        WorldRod rod = new(new WorldRodId("Rod0001"), player2.Id, new WorldPosition(3, 2));

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(rodLeaderDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(stack)
            .WithRod(rod)
            .Build();

        engine.CheckCommand(
            new WorldRemoveRodCommand(stack.Id, new WorldPosition(3, 2)),
            [
                new WorldRodDestroyedEvent(rod.Id, player1.Id, player2.Id, new WorldPosition(3, 2)),
                new WorldStackMovementConsumedEvent(stack.Id, 0)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with { MovementPoints = 0 }),
                Rods = ImmutableSortedDictionary<WorldRodId, WorldRod>.Empty
            }));
    }

    [Test]
    public void StopsExpandingTerritoryTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition rodLeaderDefinition = RodLeaderDefinition();
        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("rod-leader"),
            player1.Id,
            new WorldPosition(2, 2),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("rodLeader"), rodLeaderDefinition), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        WorldRod rod = new(new WorldRodId("Rod0001"), player1.Id, new WorldPosition(3, 2));

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(rodLeaderDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithStack(stack)
            .WithRod(rod)
            .Build();

        engine.CheckCommand(
            new WorldRemoveRodCommand(stack.Id, new WorldPosition(3, 2)),
            [
                new WorldRodDestroyedEvent(rod.Id, player1.Id, player1.Id, new WorldPosition(3, 2)),
                new WorldStackMovementConsumedEvent(stack.Id, 0)
            ]);

        _ = engine.DispatchCommand(new WorldEndTurnCommand());

        Assert.That(TestHelpers.SerializeMap(engine.State.Map), Is.EqualTo([
            "......",
            "......",
            "......",
            "......",
            "......",
            "......"
        ]));
    }

    [Test]
    public void RequiresFeatTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 3);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 1);
        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("leader"),
            player1.Id,
            new WorldPosition(2, 2),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        WorldRod rod = new(new WorldRodId("Rod0001"), player2.Id, new WorldPosition(3, 2));

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(stack)
            .WithRod(rod)
            .Build();

        engine.CheckCommandError(
            new WorldRemoveRodCommand(stack.Id, new WorldPosition(3, 2)),
            WorldCommandError.RodLeaderRequired);
    }

    [Test]
    public void NoRodTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 3);

        UnitDefinition rodLeaderDefinition = RodLeaderDefinition();
        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("rod-leader"),
            player1.Id,
            new WorldPosition(2, 2),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("rodLeader"), rodLeaderDefinition), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(rodLeaderDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithStack(stack)
            .Build();

        engine.CheckCommandError(
            new WorldRemoveRodCommand(stack.Id, new WorldPosition(3, 2)),
            WorldCommandError.RodNotFound);
    }

    [Test]
    public void InsideFortificationTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition rodLeaderDefinition = RodLeaderDefinition();
        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Undead, 1),
            WorldResourceSet.Empty);
        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            player1.Id,
            new WorldPosition(3, 3),
            UnitGrid<WorldUnit>.Empty);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("rod-leader"),
            player1.Id,
            city.Entrance,
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("rodLeader"), rodLeaderDefinition), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        WorldRod rod = new(new WorldRodId("Rod0001"), player2.Id, new WorldPosition(4, 4));

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(rodLeaderDefinition)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithCity(city)
            .WithStack(stack)
            .WithRod(rod)
            .Build();

        engine.CheckCommandError(
            new WorldRemoveRodCommand(stack.Id, new WorldPosition(4, 4)),
            WorldCommandError.RodInvalidLocation);
    }

    [Test]
    public void TargetTooFarTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 3);

        UnitDefinition rodLeaderDefinition = RodLeaderDefinition();
        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("rod-leader"),
            player1.Id,
            new WorldPosition(0, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("rodLeader"), rodLeaderDefinition), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        WorldRod rod = new(new WorldRodId("Rod0001"), player2.Id, new WorldPosition(3, 0));

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(rodLeaderDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(stack)
            .WithRod(rod)
            .Build();

        engine.CheckCommandError(
            new WorldRemoveRodCommand(stack.Id, new WorldPosition(3, 0)),
            WorldCommandError.TargetTooFar);
    }

    [Test]
    public void InsufficientMovementTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 3);

        UnitDefinition rodLeaderDefinition = RodLeaderDefinition();
        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("rod-leader"),
            player1.Id,
            new WorldPosition(2, 2),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("rodLeader"), rodLeaderDefinition), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 0);

        WorldRod rod = new(new WorldRodId("Rod0001"), player2.Id, new WorldPosition(3, 2));

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(rodLeaderDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(stack)
            .WithRod(rod)
            .Build();

        engine.CheckCommandError(
            new WorldRemoveRodCommand(stack.Id, new WorldPosition(3, 2)),
            WorldCommandError.InsufficientMovement);
    }

    [Test]
    public void NotOwnStackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 3);

        UnitDefinition rodLeaderDefinition = RodLeaderDefinition();
        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("rod-leader"),
            player2.Id,
            new WorldPosition(2, 2),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("rodLeader"), rodLeaderDefinition), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        WorldRod rod = new(new WorldRodId("Rod0001"), player1.Id, new WorldPosition(3, 2));

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(rodLeaderDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(stack)
            .WithRod(rod)
            .Build();

        engine.CheckCommandError(
            new WorldRemoveRodCommand(stack.Id, new WorldPosition(3, 2)),
            WorldCommandError.StackNotFound);
    }

    [Test]
    public void WorsensRelationTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition rodLeaderDefinition = RodLeaderDefinition();
        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("rod-leader"),
            player1.Id,
            new WorldPosition(2, 2),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("rodLeader"), rodLeaderDefinition), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        WorldRod rod = new(new WorldRodId("Rod0001"), player2.Id, new WorldPosition(3, 2));

        WorldRelation relation = new(
            new WorldRelationId(WorldRace.Undead, WorldRace.Heretic),
            Current: 40,
            Previous: 40,
            Alliance: false,
            AllianceTurn: 0,
            PermanentWar: false,
            PermanentAlliance: false);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(rodLeaderDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(stack)
            .WithRod(rod)
            .WithRelation(relation)
            .Build();

        WorldState before = engine.State;

        engine.CheckCommand(
            new WorldRemoveRodCommand(stack.Id, new WorldPosition(3, 2)),
            [
                new WorldRodDestroyedEvent(rod.Id, player1.Id, player2.Id, new WorldPosition(3, 2)),
                new WorldRelationChangedEvent(player1.Id, player2.Id, 25),
                new WorldStackMovementConsumedEvent(stack.Id, 0)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            before with
            {
                Stacks = before.Stacks.SetItem(stack.Id, stack with { MovementPoints = 0 }),
                Rods = ImmutableSortedDictionary<WorldRodId, WorldRod>.Empty,
                Relations = before.Relations.SetItem(relation.Id, relation with { Current = 25 })
            }));
    }

    [Test]
    public void OwnRodKeepsRelationsTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition rodLeaderDefinition = RodLeaderDefinition();
        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("rod-leader"),
            player1.Id,
            new WorldPosition(2, 2),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("rodLeader"), rodLeaderDefinition), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        WorldRod rod = new(new WorldRodId("Rod0001"), player1.Id, new WorldPosition(3, 2));

        WorldRelation relation = new(
            new WorldRelationId(WorldRace.Undead, WorldRace.Heretic),
            Current: 40,
            Previous: 40,
            Alliance: false,
            AllianceTurn: 0,
            PermanentWar: false,
            PermanentAlliance: false);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(rodLeaderDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(stack)
            .WithRod(rod)
            .WithRelation(relation)
            .Build();

        WorldState before = engine.State;

        engine.CheckCommand(
            new WorldRemoveRodCommand(stack.Id, new WorldPosition(3, 2)),
            [
                new WorldRodDestroyedEvent(rod.Id, player1.Id, player1.Id, new WorldPosition(3, 2)),
                new WorldStackMovementConsumedEvent(stack.Id, 0)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            before with
            {
                Stacks = before.Stacks.SetItem(stack.Id, stack with { MovementPoints = 0 }),
                Rods = ImmutableSortedDictionary<WorldRodId, WorldRod>.Empty
            }));
    }

    [Test]
    public void PermanentWarFreezesRelationTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition rodLeaderDefinition = RodLeaderDefinition();
        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("rod-leader"),
            player1.Id,
            new WorldPosition(2, 2),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("rodLeader"), rodLeaderDefinition), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        WorldRod rod = new(new WorldRodId("Rod0001"), player2.Id, new WorldPosition(3, 2));

        WorldRelation relation = new(
            new WorldRelationId(WorldRace.Undead, WorldRace.Heretic),
            Current: 40,
            Previous: 40,
            Alliance: false,
            AllianceTurn: 0,
            PermanentWar: true,
            PermanentAlliance: false);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(rodLeaderDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(stack)
            .WithRod(rod)
            .WithRelation(relation)
            .Build();

        WorldState before = engine.State;

        engine.CheckCommand(
            new WorldRemoveRodCommand(stack.Id, new WorldPosition(3, 2)),
            [
                new WorldRodDestroyedEvent(rod.Id, player1.Id, player2.Id, new WorldPosition(3, 2)),
                new WorldStackMovementConsumedEvent(stack.Id, 0)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            before with
            {
                Stacks = before.Stacks.SetItem(stack.Id, stack with { MovementPoints = 0 }),
                Rods = ImmutableSortedDictionary<WorldRodId, WorldRod>.Empty
            }));
    }
}
