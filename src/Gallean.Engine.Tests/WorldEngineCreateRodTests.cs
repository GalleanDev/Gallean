using System.Collections.Immutable;

namespace Gallean.Engine.Tests;

public sealed class WorldEngineCreateRodTests
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
        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(150, 0, 0, 0, 0, 0)
        };

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

        engine.CheckCommand(
            new WorldCreateRodCommand(stack.Id, new WorldPosition(3, 2)),
            [
                new WorldRodCreatedEvent(new WorldRodId("Rod0001"), player1.Id, new WorldPosition(3, 2)),
                new WorldStackMovementConsumedEvent(stack.Id, 0)
            ]);

        WorldRod expectedRod = new(new WorldRodId("Rod0001"), player1.Id, new WorldPosition(3, 2));

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Players = engine.State.Players.SetItem(player1.Id, player1 with { Resources = WorldResourceSet.Empty }),
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with { MovementPoints = 0 }),
                Rods = ImmutableSortedDictionary<WorldRodId, WorldRod>.Empty.Add(expectedRod.Id, expectedRod),
                Map = TestHelpers.BuildMap([
                    "......",
                    "...U..",
                    "...U..",
                    "...U..",
                    "......",
                    "......"
                ])
            }));
    }

    [Test]
    public void ExpandsTerritoryTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition rodLeaderDefinition = RodLeaderDefinition();
        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(150, 0, 0, 0, 0, 0)
        };

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

        _ = engine.DispatchCommand(new WorldCreateRodCommand(stack.Id, new WorldPosition(3, 2)));
        _ = engine.DispatchCommand(new WorldEndTurnCommand());

        Assert.That(TestHelpers.SerializeMap(engine.State.Map), Is.EqualTo([
            "......",
            "...U..",
            "..UUU.",
            "...U..",
            "......",
            "......"
        ]));
    }

    [Test]
    public void BesideResourceNodeTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 5);

        UnitDefinition rodLeaderDefinition = RodLeaderDefinition();
        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(150, 0, 0, 0, 0, 0)
        };
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Human, map);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("rod-leader"),
            player1.Id,
            new WorldPosition(1, 1),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("rodLeader"), rodLeaderDefinition), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        WorldResourceNode node = new(new WorldResourceNodeId("node"), new WorldPosition(3, 2), WorldResourceType.Gold, AiPriority: 3);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(rodLeaderDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(stack)
            .WithResourceNode(node)
            .Build();

        _ = engine.DispatchCommand(new WorldCreateRodCommand(stack.Id, new WorldPosition(2, 2)));

        Assert.That(TestHelpers.SerializeMap(engine.State.Map), Is.EqualTo([
            ".......",
            "..UUU..",
            ".UUUU..",
            "..UUU..",
            "..U...."
        ]));
    }

    [Test]
    public void RequiresFeatTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 3);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 1);
        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(150, 0, 0, 0, 0, 0)
        };

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("rod-leader"),
            player1.Id,
            new WorldPosition(2, 2),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithStack(stack)
            .Build();

        engine.CheckCommandError(
            new WorldCreateRodCommand(stack.Id, new WorldPosition(3, 2)),
            WorldCommandError.RodLeaderRequired);
    }

    [Test]
    public void LeaderDeadTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 3);

        UnitDefinition rodLeaderDefinition = RodLeaderDefinition();
        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(150, 0, 0, 0, 0, 0)
        };

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("rod-leader"),
            player1.Id,
            new WorldPosition(2, 2),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("rodLeader"), rodLeaderDefinition) with { Hitpoints = 0 }, GridPosition.FrontBottom, false)
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
            new WorldCreateRodCommand(stack.Id, new WorldPosition(3, 2)),
            WorldCommandError.RodLeaderDead);
    }

    [Test]
    public void InsideFortificationTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition rodLeaderDefinition = RodLeaderDefinition();
        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(150, 0, 0, 0, 0, 0)
        };

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

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(rodLeaderDefinition)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithCity(city)
            .WithStack(stack)
            .Build();

        engine.CheckCommandError(
            new WorldCreateRodCommand(stack.Id, new WorldPosition(4, 4)),
            WorldCommandError.RodInvalidLocation);
    }

    [Test]
    public void TargetTooFarTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 3);

        UnitDefinition rodLeaderDefinition = RodLeaderDefinition();
        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(150, 0, 0, 0, 0, 0)
        };

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("rod-leader"),
            player1.Id,
            new WorldPosition(0, 0),
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
            new WorldCreateRodCommand(stack.Id, new WorldPosition(2, 1)),
            WorldCommandError.TargetTooFar);

        engine.CheckCommand(
            new WorldCreateRodCommand(stack.Id, new WorldPosition(1, 1)),
            [
                new WorldRodCreatedEvent(new WorldRodId("Rod0001"), player1.Id, new WorldPosition(1, 1)),
                new WorldStackMovementConsumedEvent(stack.Id, 0)
            ]);
    }

    [Test]
    public void TargetOccupiedTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 3);

        UnitDefinition rodLeaderDefinition = RodLeaderDefinition();
        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(150, 0, 0, 0, 0, 0)
        };

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
            new WorldCreateRodCommand(stack.Id, new WorldPosition(2, 2)),
            WorldCommandError.TargetOccupied);
    }

    [Test]
    public void OnRodTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 3);

        UnitDefinition rodLeaderDefinition = RodLeaderDefinition();
        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(150, 0, 0, 0, 0, 0)
        };

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("rod-leader"),
            player1.Id,
            new WorldPosition(2, 2),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("rodLeader"), rodLeaderDefinition), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        WorldRod existingRod = new(new WorldRodId("existing"), player1.Id, new WorldPosition(3, 2));

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(rodLeaderDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithStack(stack)
            .WithRod(existingRod)
            .Build();

        engine.CheckCommandError(
            new WorldCreateRodCommand(stack.Id, new WorldPosition(3, 2)),
            WorldCommandError.RodInvalidLocation);
    }

    [Test]
    public void NearbyRodTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 3);

        UnitDefinition rodLeaderDefinition = RodLeaderDefinition();
        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(150, 0, 0, 0, 0, 0)
        };

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("rod-leader"),
            player1.Id,
            new WorldPosition(3, 1),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("rodLeader"), rodLeaderDefinition), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        WorldRod existingRod = new(new WorldRodId("existing"), player1.Id, new WorldPosition(0, 0));

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(rodLeaderDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithStack(stack)
            .WithRod(existingRod)
            .Build();

        engine.CheckCommandError(
            new WorldCreateRodCommand(stack.Id, new WorldPosition(2, 2)),
            WorldCommandError.RodInvalidLocation);

        engine.CheckCommand(
            new WorldCreateRodCommand(stack.Id, new WorldPosition(3, 2)),
            [
                new WorldRodCreatedEvent(new WorldRodId("Rod0001"), player1.Id, new WorldPosition(3, 2)),
                new WorldStackMovementConsumedEvent(stack.Id, 0)
            ]);
    }

    [Test]
    public void AlliedLandTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "HR....",
            "......",
            "......"
        ]);

        UnitDefinition rodLeaderDefinition = RodLeaderDefinition();
        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(150, 0, 0, 0, 0, 0)
        };
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Human, map);
        WorldPlayer player3 = TestHelpers.SimplePlayer(new WorldPlayerId(3), WorldRace.Heretic, map);

        WorldRelation alliance = new(
            new WorldRelationId(WorldRace.Undead, WorldRace.Human),
            Current: 100,
            Previous: 100,
            Alliance: true,
            AllianceTurn: 1,
            PermanentWar: false,
            PermanentAlliance: false);
        WorldRelation peace = new(
            new WorldRelationId(WorldRace.Undead, WorldRace.Heretic),
            Current: 100,
            Previous: 100,
            Alliance: false,
            AllianceTurn: 0,
            PermanentWar: false,
            PermanentAlliance: false);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("rod-leader"),
            player1.Id,
            new WorldPosition(1, 1),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("rodLeader"), rodLeaderDefinition), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(rodLeaderDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithDefinition(TestHelpers.SimpleLord(player3.LordId, player3.Race, LordCategory.Warrior))
            .WithPlayer(player3)
            .WithRelation(alliance)
            .WithRelation(peace)
            .WithStack(stack)
            .Build();

        engine.CheckCommandError(
            new WorldCreateRodCommand(stack.Id, new WorldPosition(0, 0)),
            WorldCommandError.RodInvalidLocation);

        engine.CheckCommand(
            new WorldCreateRodCommand(stack.Id, new WorldPosition(1, 0)),
            [
                new WorldRodCreatedEvent(new WorldRodId("Rod0001"), player1.Id, new WorldPosition(1, 0)),
                new WorldStackMovementConsumedEvent(stack.Id, 0)
            ]);
    }

    [Test]
    public void OnWaterTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "......",
            "...~..",
            "......"
        ]);

        UnitDefinition rodLeaderDefinition = RodLeaderDefinition();
        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(150, 0, 0, 0, 0, 0)
        };

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("rod-leader"),
            player1.Id,
            new WorldPosition(2, 1),
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
            new WorldCreateRodCommand(stack.Id, new WorldPosition(3, 1)),
            WorldCommandError.TargetUnpassable);
    }

    [Test]
    public void InsufficientResourcesTest()
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
            new WorldCreateRodCommand(stack.Id, new WorldPosition(3, 2)),
            WorldCommandError.InsufficientResources);
    }

    [Test]
    public void InsufficientMovementTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 3);

        UnitDefinition rodLeaderDefinition = RodLeaderDefinition();
        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(150, 0, 0, 0, 0, 0)
        };

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("rod-leader"),
            player1.Id,
            new WorldPosition(2, 2),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("rodLeader"), rodLeaderDefinition), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(rodLeaderDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithStack(stack)
            .Build();

        engine.CheckCommandError(
            new WorldCreateRodCommand(stack.Id, new WorldPosition(3, 2)),
            WorldCommandError.InsufficientMovement);
    }

    [Test]
    public void NotOwnStackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 3);

        UnitDefinition rodLeaderDefinition = RodLeaderDefinition();
        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map) with
        {
            Resources = new WorldResourceSet(150, 0, 0, 0, 0, 0)
        };

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("rod-leader"),
            player2.Id,
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
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(stack)
            .Build();

        engine.CheckCommandError(
            new WorldCreateRodCommand(stack.Id, new WorldPosition(3, 2)),
            WorldCommandError.StackNotFound);
    }
}
