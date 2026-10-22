namespace Gallean.Engine.Tests;

public sealed class WorldEngineDiplomacyThiefActionTests
{
    private static WorldStack ThiefStack(WorldUnitId thiefId, WorldPlayerId ownerId, UnitDefinition thief)
    {
        return TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            ownerId,
            new WorldPosition(4, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(thiefId, thief), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);
    }

    private static WorldRelation Relation(WorldRace race, WorldRace otherRace, int current)
    {
        return new WorldRelation(
            new WorldRelationId(race, otherRace),
            current,
            Previous: current,
            Alliance: false,
            AllianceTurn: 0,
            PermanentWar: false,
            PermanentAlliance: false);
    }

    [Test]
    public void PoisonStackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition warlord = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("warlord", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 10, leadership: 2);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack thiefStack = ThiefStack(new WorldUnitId("thief"), player1.Id, thief);

        WorldStack targetStack = TestHelpers.SimpleStack(
            new WorldStackId("enemy"),
            player2.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("warlord"), warlord), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 10);

        WorldThiefActionDefinition poisonStackAction = new(WorldThiefActionType.PoisonStack, 90, new GlobalTextId("poison-stack"), new GlobalTextId("poison-stack-description"));
        WorldRelation relation = Relation(WorldRace.Undead, WorldRace.Heretic, current: 40);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithRandom(new TestSequenceRandomProvider(0, 0))
            .WithDefinition(thief)
            .WithDefinition(warlord)
            .WithDefinition(poisonStackAction)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(thiefStack)
            .WithStack(targetStack)
            .WithRelation(relation)
            .Build();

        WorldState before = engine.State;

        UnitGrid<WorldUnit> poisonedUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("warlord"), warlord) with { Hitpoints = 80 }, GridPosition.FrontBottom, false)
            .Build();

        engine.CheckCommand(
            new WorldThiefActionCommand(thiefStack.Id, new WorldThiefPoisonAction(new WorldPosition(3, 3))),
            [
                new WorldThiefActionSucceededEvent(thiefStack.Id, WorldThiefActionType.PoisonStack, player2.Id),
                new WorldRelationChangedEvent(player1.Id, player2.Id, 35),
                new WorldStackPoisonedEvent(targetStack.Id, poisonedUnits),
                new WorldStackMovementConsumedEvent(thiefStack.Id, 10)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            before with
            {
                Stacks = before.Stacks
                    .SetItem(targetStack.Id, targetStack with { Units = poisonedUnits })
                    .SetItem(thiefStack.Id, thiefStack with { MovementPoints = 10 }),
                Relations = before.Relations.SetItem(relation.Id, relation with { Current = 35 })
            }));
    }

    [Test]
    public void RiotCityTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition soldier = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 5);
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack thiefStack = ThiefStack(new WorldUnitId("thief"), player1.Id, thief);

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            player2.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldier), GridPosition.FrontBottom, false)
                .Build());

        WorldThiefActionDefinition riotAction = new(WorldThiefActionType.RiotCity, 90, new GlobalTextId("riot"), new GlobalTextId("riot-description"));
        WorldRelation relation = Relation(WorldRace.Undead, WorldRace.Heretic, current: 40);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithRandom(new TestSequenceRandomProvider(2, 0, 0))
            .WithDefinition(thief)
            .WithDefinition(soldier)
            .WithDefinition(cityDefinition)
            .WithDefinition(riotAction)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Guildmaster))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(thiefStack)
            .WithCity(city)
            .WithRelation(relation)
            .Build();

        WorldState before = engine.State;

        engine.CheckCommand(
            new WorldThiefActionCommand(thiefStack.Id, new WorldThiefRiotAction(new WorldPosition(3, 3))),
            [
                new WorldThiefActionSucceededEvent(thiefStack.Id, WorldThiefActionType.RiotCity, player2.Id),
                new WorldRelationChangedEvent(player1.Id, player2.Id, 35),
                new WorldCityRiotStartedEvent(city.Id, 3),
                new WorldStackMovementConsumedEvent(thiefStack.Id, 10)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            before with
            {
                Fortifications = before.Fortifications.SetItem(city.Id, city with { RiotTurns = 3 }),
                Stacks = before.Stacks.SetItem(thiefStack.Id, thiefStack with { MovementPoints = 10 }),
                Relations = before.Relations.SetItem(relation.Id, relation with { Current = 35 })
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

        WorldStack thiefStack = ThiefStack(new WorldUnitId("thief"), player1.Id, thief);

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            neutral.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldier), GridPosition.FrontBottom, false)
                .Build());

        WorldThiefActionDefinition poisonCityAction = new(WorldThiefActionType.PoisonCity, 90, new GlobalTextId("poison-city"), new GlobalTextId("poison-city-description"));
        WorldRelation relation = Relation(WorldRace.Undead, WorldRace.Neutral, current: 40);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithRandom(new TestSequenceRandomProvider(0, 0))
            .WithDefinition(thief)
            .WithDefinition(soldier)
            .WithDefinition(cityDefinition)
            .WithDefinition(poisonCityAction)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(neutral.LordId, neutral.Race, LordCategory.Warrior))
            .WithPlayer(neutral)
            .WithStack(thiefStack)
            .WithCity(city)
            .WithRelation(relation)
            .Build();

        WorldState before = engine.State;

        UnitGrid<WorldUnit> poisonedGarrison = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldier) with { Hitpoints = 75 }, GridPosition.FrontBottom, false)
            .Build();

        engine.CheckCommand(
            new WorldThiefActionCommand(thiefStack.Id, new WorldThiefPoisonAction(new WorldPosition(3, 3))),
            [
                new WorldThiefActionSucceededEvent(thiefStack.Id, WorldThiefActionType.PoisonCity, neutral.Id),
                new WorldFortificationPoisonedEvent(city.Id, poisonedGarrison),
                new WorldStackMovementConsumedEvent(thiefStack.Id, 10)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            before with
            {
                Fortifications = before.Fortifications.SetItem(city.Id, city with { Garrison = poisonedGarrison }),
                Stacks = before.Stacks.SetItem(thiefStack.Id, thiefStack with { MovementPoints = 10 })
            }));
    }

    [Test]
    public void DuelTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition warlord = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("warlord", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 10, leadership: 2);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack thiefStack = ThiefStack(new WorldUnitId("thief"), player1.Id, thief);

        WorldStack targetStack = TestHelpers.SimpleStack(
            new WorldStackId("enemy"),
            player2.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("warlord"), warlord), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 10);

        WorldThiefActionDefinition duelAction = new(WorldThiefActionType.Duel, 100, new GlobalTextId("duel"), new GlobalTextId("duel-description"));
        WorldRelation relation = Relation(WorldRace.Undead, WorldRace.Heretic, current: 40);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithRandom(new TestSequenceRandomProvider(0, 0))
            .WithDefinition(thief)
            .WithDefinition(warlord)
            .WithDefinition(duelAction)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(thiefStack)
            .WithStack(targetStack)
            .WithRelation(relation)
            .Build();

        BattleState expectedBattleState = new(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(TestHelpers.SimpleBattleUnit(new BattleUnitId(0), new WorldUnitId("thief"), thief.Id, BattleSide.Attacker, hitpoints: 100), GridPosition.FrontCenter, false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(TestHelpers.SimpleBattleUnit(new BattleUnitId(1), new WorldUnitId("warlord"), warlord.Id, BattleSide.Defender, hitpoints: 100), GridPosition.FrontCenter, false)
                .Build(),
            CurrentUnitTurn: new BattleUnitTurn(new BattleUnitId(0), BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: null,
            Modifiers: BattleModifiers.None);

        WorldState before = engine.State;

        engine.CheckCommand(
            new WorldThiefActionCommand(thiefStack.Id, new WorldThiefDuelAction(new WorldPosition(3, 3))),
            [
                new WorldThiefActionSucceededEvent(thiefStack.Id, WorldThiefActionType.Duel, player2.Id),
                new WorldRelationChangedEvent(player1.Id, player2.Id, 35),
                new WorldStackBattleStartedEvent(thiefStack.Id, targetStack.Id, targetStack.Position, expectedBattleState, IsDuel: true),
                new WorldRelationChangedEvent(player1.Id, player2.Id, 28),
                new WorldStackMovementConsumedEvent(thiefStack.Id, 10)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            before with
            {
                Stacks = before.Stacks.SetItem(thiefStack.Id, thiefStack with { Direction = WorldStackDirection.NorthWest, MovementPoints = 10 }),
                Relations = before.Relations.SetItem(relation.Id, relation with { Current = 28 })
            }));
    }
}
