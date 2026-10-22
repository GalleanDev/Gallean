using Gallean.Common;

namespace Gallean.Engine.Tests;

public sealed class WorldEngineAiMapObjectTests
{
    [Test]
    public void DefendLocationRuinTest()
    {
        WorldMap map = TestHelpers.SimpleMap(10, 10);

        UnitDefinition mover = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("mover", maxHitpoints: 100, initiative: 40, damage: 50),
            movementPoints: 10, leadership: 6);
        UnitDefinition guard = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("guard", maxHitpoints: 5, initiative: 1, damage: 1),
            movementPoints: 0, leadership: 6);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with { IsAi = true };
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack moverStack = TestHelpers.SimpleStack(
                new WorldStackId("Mover"),
                player1.Id,
                new WorldPosition(4, 2),
                new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("mover"), mover), GridPosition.FrontBottom, false).Build(),
                movementPoints: 10) with
            {
                Order = new WorldStackOrder.DefendLocation(new ScenarioLocationId("location"), new WorldPosition(4, 2), 3)
            };

        WorldRuin ruin = new(
            Id: new WorldFortificationId("Ruin"),
            NameTextId: new ScenarioTextId("Ruin"),
            Footprint: FortificationCalculator.CreateFootprint(new WorldPosition(2, 2), FortificationCalculator.RuinFootprint),
            Variant: new WorldObjectVariant(0),
            Garrison: new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("guard"), guard), GridPosition.FrontBottom, false).Build(),
            Resources: new WorldResourceSet(200, 0, 0, 0, 0, 0),
            Item: null,
            LooterId: null,
            AiPriority: 3);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(mover)
            .WithDefinition(guard)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(moverStack)
            .WithRuin(ruin)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.Empty);
    }

    [Test]
    public void DefendLocationEntranceTest()
    {
        WorldMap map = TestHelpers.SimpleMap(10, 10);

        UnitDefinition mover = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("mover", maxHitpoints: 100, initiative: 40, damage: 50),
            movementPoints: 10, leadership: 6);
        UnitDefinition guard = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("guard", maxHitpoints: 5, initiative: 1, damage: 1),
            movementPoints: 0, leadership: 6);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with { IsAi = true };
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack moverStack = TestHelpers.SimpleStack(
                new WorldStackId("Mover"),
                player1.Id,
                new WorldPosition(8, 8),
                new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("mover"), mover), GridPosition.FrontBottom, false).Build(),
                movementPoints: 10) with
            {
                Order = new WorldStackOrder.DefendLocation(new ScenarioLocationId("location"), new WorldPosition(2, 2), 0)
            };

        WorldRuin ruin = new(
            Id: new WorldFortificationId("Ruin"),
            NameTextId: new ScenarioTextId("Ruin"),
            Footprint: FortificationCalculator.CreateFootprint(new WorldPosition(2, 2), FortificationCalculator.RuinFootprint),
            Variant: new WorldObjectVariant(0),
            Garrison: new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("guard"), guard), GridPosition.FrontBottom, false).Build(),
            Resources: new WorldResourceSet(200, 0, 0, 0, 0, 0),
            Item: null,
            LooterId: null,
            AiPriority: 3);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(mover)
            .WithDefinition(guard)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(moverStack)
            .WithRuin(ruin)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.Empty);
    }

    [Test]
    public void BerserkTest()
    {
        WorldMap map = TestHelpers.SimpleMap(10, 10);

        UnitDefinition mover = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("mover", maxHitpoints: 100, initiative: 40, damage: 50),
            movementPoints: 10, leadership: 6);
        UnitDefinition guard = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("guard", maxHitpoints: 5, initiative: 1, damage: 1),
            movementPoints: 0, leadership: 6);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with { IsAi = true };
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack moverStack = TestHelpers.SimpleStack(
                new WorldStackId("Mover"),
                player1.Id,
                new WorldPosition(4, 2),
                new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("mover"), mover), GridPosition.FrontBottom, false).Build(),
                movementPoints: 10) with
            {
                Order = WorldStackOrder.Berserk.Instance
            };

        WorldRuin ruin = new(
            Id: new WorldFortificationId("Ruin"),
            NameTextId: new ScenarioTextId("Ruin"),
            Footprint: FortificationCalculator.CreateFootprint(new WorldPosition(2, 2), FortificationCalculator.RuinFootprint),
            Variant: new WorldObjectVariant(0),
            Garrison: new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("guard"), guard), GridPosition.FrontBottom, false).Build(),
            Resources: new WorldResourceSet(200, 0, 0, 0, 0, 0),
            Item: null,
            LooterId: null,
            AiPriority: 3);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(mover)
            .WithDefinition(guard)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(moverStack)
            .WithRuin(ruin)
            .Build();

        WorldAiMemory memory = new(new WorldAiTurnMemory(1, player1.Id, 0, 0, 0, [moverStack.Id]));

        engine.CheckCommand(new WorldAiActionCommand(), [
            new WorldAiActionStartedEvent(WorldAiActionPlan.EndTurn(memory)),
            new WorldTurnEndedEvent(player1.Id),
            new WorldRoundEndedEvent(),
            new WorldRoundStartedEvent(),
            new WorldTurnStartingEvent(player2.Id),
            new WorldTurnStartedEvent(player2.Id),
            new WorldTurnResourcesGeneratedEvent(player2.Id, new WorldResourceSet(100, 0, 0, 0, 25, 0)),
            new WorldAiActionEndedEvent(player1.Id, memory)
        ]);
    }

    [Test]
    public void RoamTest()
    {
        WorldMap map = TestHelpers.SimpleMap(10, 10);

        UnitDefinition mover = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("mover", maxHitpoints: 100, initiative: 40, damage: 50),
            movementPoints: 10, leadership: 6);
        UnitDefinition guard = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("guard", maxHitpoints: 5, initiative: 1, damage: 1),
            movementPoints: 0, leadership: 6);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Neutral, map) with { IsAi = true };
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack moverStack = TestHelpers.SimpleStack(
                new WorldStackId("Mover"),
                player1.Id,
                new WorldPosition(4, 2),
                new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("mover"), mover), GridPosition.FrontBottom, false).Build(),
                movementPoints: 10) with
            {
                Order = WorldStackOrder.Roam.Instance
            };

        WorldRuin ruin = new(
            Id: new WorldFortificationId("Ruin"),
            NameTextId: new ScenarioTextId("Ruin"),
            Footprint: FortificationCalculator.CreateFootprint(new WorldPosition(2, 2), FortificationCalculator.RuinFootprint),
            Variant: new WorldObjectVariant(0),
            Garrison: new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("guard"), guard), GridPosition.FrontBottom, false).Build(),
            Resources: new WorldResourceSet(200, 0, 0, 0, 0, 0),
            Item: null,
            LooterId: null,
            AiPriority: 3);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(mover)
            .WithDefinition(guard)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(moverStack)
            .WithRuin(ruin)
            .Build();

        WorldAiMemory memory = new(new WorldAiTurnMemory(1, player1.Id, 0, 0, 0, [moverStack.Id]));

        engine.CheckCommand(new WorldAiActionCommand(), [
            new WorldAiActionStartedEvent(WorldAiActionPlan.EndTurn(memory)),
            new WorldTurnEndedEvent(player1.Id),
            new WorldRoundEndedEvent(),
            new WorldRoundStartedEvent(),
            new WorldTurnStartingEvent(player2.Id),
            new WorldTurnStartedEvent(player2.Id),
            new WorldTurnResourcesGeneratedEvent(player2.Id, new WorldResourceSet(100, 0, 0, 0, 25, 0)),
            new WorldAiActionEndedEvent(player1.Id, memory)
        ]);
    }

    [Test]
    public void SecureCityTest()
    {
        WorldMap map = TestHelpers.SimpleMap(10, 10);

        UnitDefinition mover = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("mover", maxHitpoints: 100, initiative: 40, damage: 50),
            movementPoints: 10, leadership: 6);
        UnitDefinition guard = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("guard", maxHitpoints: 5, initiative: 1, damage: 1),
            movementPoints: 0, leadership: 6);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with { IsAi = true };
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("Capital"),
            player1.Id,
            new WorldPosition(9, 6),
            UnitGrid<WorldUnit>.Empty);

        WorldStack moverStack = TestHelpers.SimpleStack(
                new WorldStackId("Mover"),
                player1.Id,
                new WorldPosition(3, 2),
                new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("mover"), mover), GridPosition.FrontBottom, false).Build(),
                movementPoints: 10) with
            {
                Order = new WorldStackOrder.SecureCity(capital.Id)
            };

        WorldRuin ruin = new(
            Id: new WorldFortificationId("Ruin"),
            NameTextId: new ScenarioTextId("Ruin"),
            Footprint: FortificationCalculator.CreateFootprint(new WorldPosition(2, 2), FortificationCalculator.RuinFootprint),
            Variant: new WorldObjectVariant(0),
            Garrison: new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("guard"), guard), GridPosition.FrontBottom, false).Build(),
            Resources: new WorldResourceSet(200, 0, 0, 0, 0, 0),
            Item: null,
            LooterId: null,
            AiPriority: 3);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(mover)
            .WithDefinition(guard)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithCapital(capital)
            .WithStack(moverStack)
            .WithRuin(ruin)
            .Build();

        WorldAiMemory memory = new(new WorldAiTurnMemory(1, player1.Id, 1, 0, 0, []));

        WorldAiActionPlan plan = new(
            new WorldAiActionScore(1, 1000),
            new WorldAiMoveStackCommand(moverStack.Id, new WorldPosition(5, 7)),
            memory);

        engine.CheckCommand(new WorldAiActionCommand(), [
            new WorldAiActionStartedEvent(plan),
            new WorldStackMovingEvent(moverStack.Id, new WorldPosition(3, 2), new WorldPosition(3, 3), WorldStackDirection.SouthWest, 8, [new WorldPosition(3, 4), new WorldPosition(3, 5), new WorldPosition(4, 6), new WorldPosition(5, 7)], null),
            new WorldStackMovedEvent(moverStack.Id, new WorldPosition(3, 2), new WorldPosition(3, 3), WorldStackDirection.SouthWest, 8, [new WorldPosition(3, 4), new WorldPosition(3, 5), new WorldPosition(4, 6), new WorldPosition(5, 7)], null),
            new WorldStackMovingEvent(moverStack.Id, new WorldPosition(3, 3), new WorldPosition(3, 4), WorldStackDirection.SouthWest, 6, [new WorldPosition(3, 5), new WorldPosition(4, 6), new WorldPosition(5, 7)], null),
            new WorldStackMovedEvent(moverStack.Id, new WorldPosition(3, 3), new WorldPosition(3, 4), WorldStackDirection.SouthWest, 6, [new WorldPosition(3, 5), new WorldPosition(4, 6), new WorldPosition(5, 7)], null),
            new WorldStackMovingEvent(moverStack.Id, new WorldPosition(3, 4), new WorldPosition(3, 5), WorldStackDirection.SouthWest, 4, [new WorldPosition(4, 6), new WorldPosition(5, 7)], null),
            new WorldStackMovedEvent(moverStack.Id, new WorldPosition(3, 4), new WorldPosition(3, 5), WorldStackDirection.SouthWest, 4, [new WorldPosition(4, 6), new WorldPosition(5, 7)], null),
            new WorldStackMovingEvent(moverStack.Id, new WorldPosition(3, 5), new WorldPosition(4, 6), WorldStackDirection.South, 2, [new WorldPosition(5, 7)], null),
            new WorldStackMovedEvent(moverStack.Id, new WorldPosition(3, 5), new WorldPosition(4, 6), WorldStackDirection.South, 2, [new WorldPosition(5, 7)], null),
            new WorldStackMovingEvent(moverStack.Id, new WorldPosition(4, 6), new WorldPosition(5, 7), WorldStackDirection.South, 0, [], null),
            new WorldStackMovedEvent(moverStack.Id, new WorldPosition(4, 6), new WorldPosition(5, 7), WorldStackDirection.South, 0, [], null),
            new WorldAiActionEndedEvent(player1.Id, memory)
        ]);
    }
}
