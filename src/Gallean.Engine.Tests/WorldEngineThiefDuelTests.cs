namespace Gallean.Engine.Tests;

public sealed class WorldEngineThiefDuelTests
{
    private static UnitDefinition Warlord()
    {
        return TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("warlord", maxHitpoints: 80, initiative: 40, damage: 20),
            movementPoints: 10, leadership: 2);
    }

    [Test]
    public void StartsLeaderOnlyBattleTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition warlord = Warlord();
        UnitDefinition soldier = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 60, initiative: 30, damage: 5);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldUnit thiefUnit = new(new WorldUnitId("thiefUnit"), thief.Id, 100, 7, 1, [], Name: "Sneak", IsTypeLocked: false);
        WorldUnit warlordUnit = new(new WorldUnitId("warlordUnit"), warlord.Id, 80, 11, 1, [], null, false);
        WorldUnit soldierUnit = new(new WorldUnitId("soldierUnit"), soldier.Id, 60, 3, 1, [], null, false);

        WorldStack thiefStack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player1.Id,
            new WorldPosition(2, 2),
            new UnitGrid<WorldUnit>.Builder()
                .Place(thiefUnit, GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        WorldStack targetStack = TestHelpers.SimpleStack(
            new WorldStackId("enemy"),
            player2.Id,
            new WorldPosition(3, 2),
            new UnitGrid<WorldUnit>.Builder()
                .Place(warlordUnit, GridPosition.BackBottom, false)
                .Place(soldierUnit, GridPosition.FrontTop, false)
                .Build(),
            movementPoints: 10);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithRandom(new TestSequenceRandomProvider(0, 0))
            .WithDefinition(thief)
            .WithDefinition(warlord)
            .WithDefinition(soldier)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Duel, 100))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(thiefStack)
            .WithStack(targetStack)
            .Build();

        BattleUnit attackerLeader = new(
            Id: new BattleUnitId(0),
            WorldUnitId: new WorldUnitId("thiefUnit"),
            DefinitionId: thief.Id,
            Side: BattleSide.Attacker,
            Hitpoints: 100,
            Experience: 7,
            ExperienceGained: 0,
            Level: 1,
            Modifiers: [],
            StatusEffects: [],
            SummonerId: null,
            EquipmentAttackIds: [],
            Name: "Sneak",
            IsTypeLocked: false);

        BattleUnit defenderLeader = new(
            Id: new BattleUnitId(1),
            WorldUnitId: new WorldUnitId("warlordUnit"),
            DefinitionId: warlord.Id,
            Side: BattleSide.Defender,
            Hitpoints: 80,
            Experience: 11,
            ExperienceGained: 0,
            Level: 1,
            Modifiers: [],
            StatusEffects: [],
            SummonerId: null,
            EquipmentAttackIds: [],
            Name: null,
            IsTypeLocked: false);

        BattleState expectedBattleState = new(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder().Place(attackerLeader, GridPosition.FrontCenter, false).Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder().Place(defenderLeader, GridPosition.FrontCenter, false).Build(),
            CurrentUnitTurn: new BattleUnitTurn(new BattleUnitId(0), BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: null,
            Modifiers: BattleModifiers.None);

        WorldState before = engine.State;

        engine.CheckCommand(
            new WorldThiefActionCommand(thiefStack.Id, new WorldThiefDuelAction(new WorldPosition(3, 2))),
            [
                new WorldThiefActionSucceededEvent(thiefStack.Id, WorldThiefActionType.Duel, player2.Id),
                new WorldStackBattleStartedEvent(thiefStack.Id, targetStack.Id, targetStack.Position, expectedBattleState, IsDuel: true),
                new WorldStackMovementConsumedEvent(thiefStack.Id, 10)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            before with
            {
                Stacks = before.Stacks.SetItem(thiefStack.Id, thiefStack with { Direction = WorldStackDirection.SouthEast, MovementPoints = 10 })
            }));
    }

    [Test]
    public void BystandersSurviveTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition warlord = Warlord();
        UnitDefinition soldier = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 60, initiative: 30, damage: 5);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldUnit thiefUnit = new(new WorldUnitId("thiefUnit"), thief.Id, 100, 7, 1, [], null, false);
        WorldUnit warlordUnit = new(new WorldUnitId("warlordUnit"), warlord.Id, 80, 11, 1, [], null, false);
        WorldUnit soldierUnit = new(new WorldUnitId("soldierUnit"), soldier.Id, 42, 3, 1, [], null, false);

        WorldStack thiefStack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player1.Id,
            new WorldPosition(2, 2),
            new UnitGrid<WorldUnit>.Builder()
                .Place(thiefUnit, GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        WorldStack targetStack = TestHelpers.SimpleStack(
            new WorldStackId("enemy"),
            player2.Id,
            new WorldPosition(3, 2),
            new UnitGrid<WorldUnit>.Builder()
                .Place(warlordUnit, GridPosition.BackBottom, false)
                .Place(soldierUnit, GridPosition.FrontTop, false)
                .Build(),
            movementPoints: 10);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithRandom(new TestSequenceRandomProvider(0, 0))
            .WithDefinition(thief)
            .WithDefinition(warlord)
            .WithDefinition(soldier)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Duel, 100))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(thiefStack)
            .WithStack(targetStack)
            .Build();

        BattleUnit attackerLeader = new(
            Id: new BattleUnitId(0),
            WorldUnitId: new WorldUnitId("thiefUnit"),
            DefinitionId: thief.Id,
            Side: BattleSide.Attacker,
            Hitpoints: 100,
            Experience: 7,
            ExperienceGained: 0,
            Level: 1,
            Modifiers: [],
            StatusEffects: [],
            SummonerId: null,
            EquipmentAttackIds: [],
            Name: null,
            IsTypeLocked: false);

        BattleUnit defenderLeader = new(
            Id: new BattleUnitId(1),
            WorldUnitId: new WorldUnitId("warlordUnit"),
            DefinitionId: warlord.Id,
            Side: BattleSide.Defender,
            Hitpoints: 80,
            Experience: 11,
            ExperienceGained: 0,
            Level: 1,
            Modifiers: [],
            StatusEffects: [],
            SummonerId: null,
            EquipmentAttackIds: [],
            Name: null,
            IsTypeLocked: false);

        BattleState initialBattleState = new(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder().Place(attackerLeader, GridPosition.FrontCenter, false).Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder().Place(defenderLeader, GridPosition.FrontCenter, false).Build(),
            CurrentUnitTurn: new BattleUnitTurn(new BattleUnitId(0), BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: null,
            Modifiers: BattleModifiers.None);

        engine.CheckCommand(
            new WorldThiefActionCommand(thiefStack.Id, new WorldThiefDuelAction(new WorldPosition(3, 2))),
            [
                new WorldThiefActionSucceededEvent(thiefStack.Id, WorldThiefActionType.Duel, player2.Id),
                new WorldStackBattleStartedEvent(thiefStack.Id, targetStack.Id, targetStack.Position, initialBattleState, IsDuel: true),
                new WorldStackMovementConsumedEvent(thiefStack.Id, 10)
            ]);

        BattleState finalState = initialBattleState with
        {
            AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                .Place(attackerLeader with { Hitpoints = 25, ExperienceGained = 30 }, GridPosition.FrontCenter, false)
                .Build(),
            DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                .Place(defenderLeader with { Hitpoints = 0 }, GridPosition.FrontCenter, false)
                .Build(),
            Outcome = new BattleOutcome(BattleSide.Attacker)
        };

        WorldState before = engine.State;

        engine.CheckCommand(
            new WorldResolveStackBattleCommand(thiefStack.Id, targetStack.Id, finalState, IsDuel: true),
            [
                new WorldStackBattleEndedEvent(
                    thiefStack.Id,
                    targetStack.Id,
                    BattleSide.Attacker,
                    new UnitGrid<WorldUnit>.Builder()
                        .Place(TestHelpers.SimpleUnit(new WorldUnitId("thiefUnit"), thief) with { Hitpoints = 25, Experience = 37 }, GridPosition.FrontBottom, false)
                        .Build(),
                    new UnitGrid<WorldUnit>.Builder()
                        .Place(TestHelpers.SimpleUnit(new WorldUnitId("warlordUnit"), warlord) with { Hitpoints = 0, Experience = 11 }, GridPosition.BackBottom, false)
                        .Place(soldierUnit, GridPosition.FrontTop, false)
                        .Build()),
                new WorldStackRelocatedEvent(targetStack.Id, new WorldPosition(3, 2), new WorldPosition(4, 2), WorldStackDirection.SouthEast)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            before with
            {
                Stacks = before.Stacks
                    .SetItem(thiefStack.Id, thiefStack with
                    {
                        Direction = WorldStackDirection.SouthEast,
                        MovementPoints = 10,
                        Units = new UnitGrid<WorldUnit>.Builder()
                            .Place(TestHelpers.SimpleUnit(new WorldUnitId("thiefUnit"), thief) with { Hitpoints = 25, Experience = 37 }, GridPosition.FrontBottom, false)
                            .Build(),
                        BattlesWon = 1
                    })
                    .SetItem(targetStack.Id, targetStack with
                    {
                        Units = new UnitGrid<WorldUnit>.Builder()
                            .Place(TestHelpers.SimpleUnit(new WorldUnitId("warlordUnit"), warlord) with { Hitpoints = 0, Experience = 11 }, GridPosition.BackBottom, false)
                            .Place(soldierUnit, GridPosition.FrontTop, false)
                            .Build(),
                        Position = new WorldPosition(4, 2),
                        Direction = WorldStackDirection.SouthEast
                    })
            }));
    }

    [Test]
    public void ThiefKilledTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition warlord = Warlord();

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldUnit thiefUnit = new(new WorldUnitId("thiefUnit"), thief.Id, 100, 7, 1, [], null, false);
        WorldUnit warlordUnit = new(new WorldUnitId("warlordUnit"), warlord.Id, 80, 11, 1, [], null, false);

        WorldStack thiefStack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player1.Id,
            new WorldPosition(2, 2),
            new UnitGrid<WorldUnit>.Builder()
                .Place(thiefUnit, GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        WorldStack targetStack = TestHelpers.SimpleStack(
            new WorldStackId("enemy"),
            player2.Id,
            new WorldPosition(3, 2),
            new UnitGrid<WorldUnit>.Builder()
                .Place(warlordUnit, GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 10);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithRandom(new TestSequenceRandomProvider(0, 0))
            .WithDefinition(thief)
            .WithDefinition(warlord)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Duel, 100))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(thiefStack)
            .WithStack(targetStack)
            .Build();

        BattleUnit attackerLeader = new(
            Id: new BattleUnitId(0),
            WorldUnitId: new WorldUnitId("thiefUnit"),
            DefinitionId: thief.Id,
            Side: BattleSide.Attacker,
            Hitpoints: 100,
            Experience: 7,
            ExperienceGained: 0,
            Level: 1,
            Modifiers: [],
            StatusEffects: [],
            SummonerId: null,
            EquipmentAttackIds: [],
            Name: null,
            IsTypeLocked: false);

        BattleUnit defenderLeader = new(
            Id: new BattleUnitId(1),
            WorldUnitId: new WorldUnitId("warlordUnit"),
            DefinitionId: warlord.Id,
            Side: BattleSide.Defender,
            Hitpoints: 80,
            Experience: 11,
            ExperienceGained: 0,
            Level: 1,
            Modifiers: [],
            StatusEffects: [],
            SummonerId: null,
            EquipmentAttackIds: [],
            Name: null,
            IsTypeLocked: false);

        BattleState initialBattleState = new(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder().Place(attackerLeader, GridPosition.FrontCenter, false).Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder().Place(defenderLeader, GridPosition.FrontCenter, false).Build(),
            CurrentUnitTurn: new BattleUnitTurn(new BattleUnitId(0), BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: null,
            Modifiers: BattleModifiers.None);

        engine.CheckCommand(
            new WorldThiefActionCommand(thiefStack.Id, new WorldThiefDuelAction(new WorldPosition(3, 2))),
            [
                new WorldThiefActionSucceededEvent(thiefStack.Id, WorldThiefActionType.Duel, player2.Id),
                new WorldStackBattleStartedEvent(thiefStack.Id, targetStack.Id, targetStack.Position, initialBattleState, IsDuel: true),
                new WorldStackMovementConsumedEvent(thiefStack.Id, 10)
            ]);

        BattleState finalState = initialBattleState with
        {
            AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                .Place(attackerLeader with { Hitpoints = 0 }, GridPosition.FrontCenter, false)
                .Build(),
            DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                .Place(defenderLeader with { Hitpoints = 55, ExperienceGained = 20 }, GridPosition.FrontCenter, false)
                .Build(),
            Outcome = new BattleOutcome(BattleSide.Defender)
        };

        WorldState before = engine.State;

        engine.CheckCommand(
            new WorldResolveStackBattleCommand(thiefStack.Id, targetStack.Id, finalState, IsDuel: true),
            [
                new WorldStackBattleEndedEvent(
                    thiefStack.Id,
                    targetStack.Id,
                    BattleSide.Defender,
                    new UnitGrid<WorldUnit>.Builder()
                        .Place(TestHelpers.SimpleUnit(new WorldUnitId("thiefUnit"), thief) with { Hitpoints = 0, Experience = 7 }, GridPosition.FrontBottom, false)
                        .Build(),
                    new UnitGrid<WorldUnit>.Builder()
                        .Place(TestHelpers.SimpleUnit(new WorldUnitId("warlordUnit"), warlord) with { Hitpoints = 55, Experience = 31 }, GridPosition.FrontBottom, false)
                        .Build()),
                new WorldStackDestroyedEvent(thiefStack.Id, new WorldUnitGroup.Stack(targetStack.Id), targetStack.OwnerId)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            before with
            {
                Stacks = before.Stacks
                    .Remove(thiefStack.Id)
                    .SetItem(targetStack.Id, targetStack with
                    {
                        Units = new UnitGrid<WorldUnit>.Builder()
                            .Place(TestHelpers.SimpleUnit(new WorldUnitId("warlordUnit"), warlord) with { Hitpoints = 55, Experience = 31 }, GridPosition.FrontBottom, false)
                            .Build(),
                        BattlesWon = 1
                    }),
                ScenarioState = before.ScenarioState with
                {
                    DestroyedStacks = [new ScenarioDestroyedStack(thiefStack.Id, targetStack.OwnerId, SourceTemplateId: null)]
                }
            }));
    }

    [Test]
    public void DeadTargetLeaderTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition warlord = Warlord();
        UnitDefinition soldier = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 60, initiative: 30, damage: 5);

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
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("warlord"), warlord) with { Hitpoints = 0 }, GridPosition.FrontBottom, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldier), GridPosition.FrontCenter, false)
                .Build(),
            movementPoints: 10);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithRandom(new TestSequenceRandomProvider(0, 0))
            .WithDefinition(thief)
            .WithDefinition(warlord)
            .WithDefinition(soldier)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Duel, 100))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(thiefStack)
            .WithStack(targetStack)
            .Build();

        WorldState before = engine.State;

        engine.CheckCommandError(
            new WorldThiefActionCommand(thiefStack.Id, new WorldThiefDuelAction(new WorldPosition(3, 2))),
            WorldCommandError.ThiefTargetInvalid);

        Assert.That(engine.State, Is.EqualTo(before));
    }
}
