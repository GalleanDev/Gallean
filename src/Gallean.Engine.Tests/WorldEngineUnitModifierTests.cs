using Gallean.Common;

namespace Gallean.Engine.Tests;

public sealed class WorldEngineUnitModifierTests
{
    [Test]
    public void LeadershipModifierTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^..."
        ]);

        UnitDefinition leaderDefinition = TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Leadership = 2
        };

        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 50, initiative: 40, damage: 5);

        UnitModifierDefinition leadershipModifier = TestHelpers.SimpleModifier("leadership-modifier") with
        {
            Leadership = new UnitStatModifier(1, 0)
        };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Undead, 1),
            WorldResourceSet.Empty);

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city1"),
            cityDefinition,
            player.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition), new GridPosition(0, 0), false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier2"), soldierDefinition), new GridPosition(0, 1), false)
                .Build());

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack1"),
            player.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition) with { Modifiers = [UnitModifier.Permanent(leadershipModifier.Id)] }, new GridPosition(0, 0), false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier3"), soldierDefinition), new GridPosition(0, 1), false)
                .Build(),
            movementPoints: 20);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(leadershipModifier)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCity(city)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldTransferUnitCommand(
                new WorldUnitGroup.Fortification(city.Id),
                new WorldUnitGroup.Stack(stack.Id),
                new WorldUnitId("soldier"),
                new GridPosition(0, 2)),
            [
                new WorldUnitTransferredEvent(new WorldUnitGroup.Fortification(city.Id), new WorldUnitGroup.Stack(stack.Id), new WorldUnitId("soldier"), new GridPosition(0, 2))
            ]);

        engine.CheckCommandError(
            new WorldTransferUnitCommand(
                new WorldUnitGroup.Fortification(city.Id),
                new WorldUnitGroup.Stack(stack.Id),
                new WorldUnitId("soldier2"),
                new GridPosition(1, 0)),
            WorldCommandError.LeadershipExceeded);
    }

    [Test]
    public void VisionRadiusModifierTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 4);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 5, leadership: 6) with
            {
                VisionRadius = 1
            };

        UnitModifierDefinition visionModifier = TestHelpers.SimpleModifier("vision-modifier") with
        {
            VisionRadius = new UnitStatModifier(2, 0)
        };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            FogOfWar = ImmutableBitmap.Create(map.Width, map.Height, false)
        };

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("s1"),
            player.Id,
            new WorldPosition(0, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition) with { Modifiers = [UnitModifier.Permanent(visionModifier.Id)] }, GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 5);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(visionModifier)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        Assert.That(TestHelpers.SerializeFogOfWar(engine.State.Players[player.Id].FogOfWar), Is.EqualTo([
            "....?",
            "....?",
            "....?",
            "....?"
        ]));
    }

    [Test]
    public void VisionRadiusCapTest()
    {
        WorldMap map = TestHelpers.SimpleMap(20, 2);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 5, leadership: 6) with
            {
                VisionRadius = 5
            };

        UnitModifierDefinition visionModifier = TestHelpers.SimpleModifier("vision-modifier") with
        {
            VisionRadius = new UnitStatModifier(5, 0)
        };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            FogOfWar = ImmutableBitmap.Create(map.Width, map.Height, false)
        };

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("s1"),
            player.Id,
            new WorldPosition(0, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition) with { Modifiers = [UnitModifier.Permanent(visionModifier.Id)] }, GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 5);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(visionModifier)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        Assert.That(TestHelpers.SerializeFogOfWar(engine.State.Players[player.Id].FogOfWar), Is.EqualTo([
            ".........???????????",
            ".........???????????"
        ]));
    }

    [Test]
    public void VisionRadiusUnderCapTest()
    {
        WorldMap map = TestHelpers.SimpleMap(20, 2);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 5, leadership: 6) with
            {
                VisionRadius = 5
            };

        UnitModifierDefinition visionModifier = TestHelpers.SimpleModifier("vision-modifier") with
        {
            VisionRadius = new UnitStatModifier(2, 0)
        };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            FogOfWar = ImmutableBitmap.Create(map.Width, map.Height, false)
        };

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("s1"),
            player.Id,
            new WorldPosition(0, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition) with { Modifiers = [UnitModifier.Permanent(visionModifier.Id)] }, GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 5);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(visionModifier)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        Assert.That(TestHelpers.SerializeFogOfWar(engine.State.Players[player.Id].FogOfWar), Is.EqualTo([
            "........????????????",
            "........????????????"
        ]));
    }

    [Test]
    public void NonLeaderVisionModifierTest()
    {
        WorldMap map = TestHelpers.SimpleMap(20, 2);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 5, leadership: 6) with
            {
                VisionRadius = 1
            };

        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 50, damage: 10);

        UnitModifierDefinition visionModifier = TestHelpers.SimpleModifier("vision-modifier") with
        {
            VisionRadius = new UnitStatModifier(3, 0)
        };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            FogOfWar = ImmutableBitmap.Create(map.Width, map.Height, false)
        };

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("s1"),
            player.Id,
            new WorldPosition(0, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition) with { Modifiers = [UnitModifier.Permanent(visionModifier.Id)] }, GridPosition.FrontTop, false)
                .Build(),
            movementPoints: 5);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(visionModifier)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        Assert.That(TestHelpers.SerializeFogOfWar(engine.State.Players[player.Id].FogOfWar), Is.EqualTo([
            "..??????????????????",
            "..??????????????????"
        ]));
    }

    [Test]
    public void LeaderWithoutVisionTest()
    {
        WorldMap map = TestHelpers.SimpleMap(20, 2);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 5, leadership: 6);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            FogOfWar = ImmutableBitmap.Create(map.Width, map.Height, false)
        };

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("s1"),
            player.Id,
            new WorldPosition(0, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 5);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        Assert.That(TestHelpers.SerializeFogOfWar(engine.State.Players[player.Id].FogOfWar), Is.EqualTo([
            "..??????????????????",
            "..??????????????????"
        ]));
    }

    [Test]
    public void NeutralDurationTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer neutral = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Neutral, map) with { IsAi = true };

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 3);

        UnitModifierDefinition expiringDefinition = TestHelpers.SimpleModifier("expiring") with
        {
            Damage = new UnitStatModifier(0, 30)
        };
        UnitModifierDefinition lastingDefinition = expiringDefinition with { Id = new UnitModifierDefinitionId("lasting") };

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Neutral"),
            neutral.Id,
            new WorldPosition(2, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(
                    TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition) with
                    {
                        Modifiers =
                        [
                            new UnitModifier(expiringDefinition.Id, RemainingTurns: 1),
                            new UnitModifier(lastingDefinition.Id, RemainingTurns: 3)
                        ]
                    },
                    GridPosition.FrontBottom,
                    false)
                .Build(),
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(expiringDefinition)
            .WithDefinition(lastingDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(neutral.LordId, neutral.Race, LordCategory.Warrior))
            .WithPlayer(neutral)
            .WithStack(stack)
            .Build();

        engine.CheckCommand(new WorldEndTurnCommand(), [
            new WorldTurnEndedEvent(player1.Id),
            new WorldTurnStartingEvent(neutral.Id),
            new WorldStackMovementRestoredEvent(stack.Id, 6),
            new WorldUnitModifierExpiredEvent(new WorldUnitGroup.Stack(stack.Id), new WorldUnitId("leader"), expiringDefinition.Id),
            new WorldUnitModifierDurationDecreasedEvent(new WorldUnitGroup.Stack(stack.Id), new WorldUnitId("leader"), lastingDefinition.Id, 2),
            new WorldTurnStartedEvent(neutral.Id),
            new WorldTurnResourcesGeneratedEvent(neutral.Id, WorldResourceSet.Empty)
        ]);
    }
}
