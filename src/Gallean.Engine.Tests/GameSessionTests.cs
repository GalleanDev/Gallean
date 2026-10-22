using System.Collections.Immutable;
using Gallean.Common;
using Gallean.Tests.Common;

namespace Gallean.Engine.Tests;

public sealed class GameSessionTests
{
    [Test]
    public void HumanBattleTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        UnitDefinition attackerDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 100, initiative: 50, damage: 50),
            movementPoints: 6, leadership: 6);
        UnitDefinition defenderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 40, damage: 5),
            movementPoints: 6, leadership: 6);
        GlobalVariables globals = new() { BattleInitiativeVariance = 0, BattleDamageVariance = 0 };

        WorldPlayer ai = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with { IsAi = true };
        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack aiStack = TestHelpers.SimpleStack(
            new WorldStackId("AiStack"),
            ai.Id,
            new WorldPosition(1, 0),
            new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("attacker"), attackerDefinition), GridPosition.FrontBottom, false).Build(),
            movementPoints: 6);

        WorldStack humanStack = TestHelpers.SimpleStack(
            new WorldStackId("HumanStack"),
            human.Id,
            new WorldPosition(2, 0),
            new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("defender"), defenderDefinition), GridPosition.FrontBottom, false).Build(),
            movementPoints: 6);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithGlobals(globals)
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(TestHelpers.SimpleLord(ai.LordId, ai.Race, LordCategory.Warrior))
            .WithPlayer(ai)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithStack(aiStack)
            .WithStack(humanStack)
            .BuildSession();

        WorldState expectedWorld = session.World.State with
        {
            Stacks = session.World.State.Stacks.SetItem(
                aiStack.Id,
                aiStack with { Direction = WorldStackDirection.SouthEast, MovementPoints = 3 }),
            AiMemories = session.World.State.AiMemories.SetItem(
                ai.Id,
                new WorldAiMemory(new WorldAiTurnMemory(1, ai.Id, 1, 0, 0, []))
                    .WithStackMemory(aiStack.Id, new WorldAiStackMemory(
                        WorldStackOrder.Normal.Instance,
                        new WorldAiTarget.Stack(humanStack.Id))))
        };
        BattleState expectedBattle = new(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(TestHelpers.SimpleBattleUnit(new BattleUnitId(0), new WorldUnitId("attacker"), attackerDefinition.Id, BattleSide.Attacker, hitpoints: 100), GridPosition.FrontBottom, false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(TestHelpers.SimpleBattleUnit(new BattleUnitId(1), new WorldUnitId("defender"), defenderDefinition.Id, BattleSide.Defender, hitpoints: 10), GridPosition.FrontBottom, false)
                .Build(),
            CurrentUnitTurn: new BattleUnitTurn(new BattleUnitId(0), BattleUnitTurnType.Normal),
            TurnQueue: [new BattleUnitTurn(new BattleUnitId(1), BattleUnitTurnType.Normal)],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: null,
            Modifiers: BattleModifiers.None);

        session.ExecuteAiAction();

        Assert.That(session.World.State, Is.EqualTo(expectedWorld));
        BattleSession battle = Assert.NotNull(session.Battle);
        Assert.That(battle.State, Is.EqualTo(expectedBattle));
        Assert.That(battle.IsAttackerAi, Is.True);
        Assert.That(battle.IsDefenderAi, Is.False);
        Assert.That(battle.CanAutoResolve, Is.True);
        Assert.That(battle.Position, Is.EqualTo(new WorldPosition(2, 0)));

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => session.ExecuteAiAction());

        Assert.That(ex.Message, Is.EqualTo("Cannot execute an AI action while a battle is active."));
    }

    [Test]
    public void BattleResolutionEventsTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        UnitDefinition attackerDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 100, initiative: 50, damage: 50),
            movementPoints: 6, leadership: 6);
        UnitDefinition defenderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 40, damage: 5),
            movementPoints: 6, leadership: 6);
        GlobalVariables globals = new() { BattleInitiativeVariance = 0, BattleDamageVariance = 0 };

        WorldPlayer ai = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with { IsAi = true };
        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack aiStack = TestHelpers.SimpleStack(
            new WorldStackId("AiStack"),
            ai.Id,
            new WorldPosition(1, 0),
            new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("attacker"), attackerDefinition), GridPosition.FrontBottom, false).Build(),
            movementPoints: 6);

        WorldStack humanStack = TestHelpers.SimpleStack(
            new WorldStackId("HumanStack"),
            human.Id,
            new WorldPosition(2, 0),
            new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("defender"), defenderDefinition), GridPosition.FrontBottom, false).Build(),
            movementPoints: 6);

        (GameSession session, IEngineContext _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithGlobals(globals)
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(TestHelpers.SimpleLord(ai.LordId, ai.Race, LordCategory.Warrior))
            .WithPlayer(ai)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithStack(aiStack)
            .WithStack(humanStack)
            .BuildSession();

        session.ExecuteAiAction();

        List<WorldEvent> resolutionEvents = [];
        using IDisposable subscription = session.World.Events.Subscribe(step => resolutionEvents.Add(step.Event));

        BattleSession battle = Assert.NotNull(session.Battle);
        BattleCommandResult result = battle.DispatchCommand(new BattleAutoResolveCommand());
        Assert.That(result.Error, Is.Null);

        UnitGrid<WorldUnit> survivingAttackerGrid = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("attacker"), attackerDefinition) with { Experience = 1 }, GridPosition.FrontBottom, false)
            .Build();
        UnitGrid<WorldUnit> survivingDefenderGrid = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("defender"), defenderDefinition) with { Hitpoints = 0 }, GridPosition.FrontBottom, false)
            .Build();

        List<WorldEvent> expectedResolutionEvents =
        [
            new WorldStackBattleEndedEvent(aiStack.Id, humanStack.Id, BattleSide.Attacker, survivingAttackerGrid, survivingDefenderGrid),
            new WorldStackDestroyedEvent(humanStack.Id, new WorldUnitGroup.Stack(aiStack.Id), aiStack.OwnerId)
        ];

        Assert.That(session.Battle, Is.Null);
        Assert.That(resolutionEvents, Is.EqualTo(expectedResolutionEvents));
    }

    [Test]
    public void AiBattleTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 1);

        UnitDefinition attackerDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 100, initiative: 50, damage: 50),
            movementPoints: 6, leadership: 6);
        UnitDefinition defenderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 40, damage: 5),
            movementPoints: 6, leadership: 6);
        GlobalVariables globals = new() { BattleInitiativeVariance = 0, BattleDamageVariance = 0 };

        WorldPlayer aiAttacker = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with { IsAi = true };
        WorldPlayer aiDefender = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map) with { IsAi = true };
        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(3), WorldRace.Human, map);

        WorldStack attackerStack = TestHelpers.SimpleStack(
            new WorldStackId("Attacker"),
            aiAttacker.Id,
            new WorldPosition(0, 0),
            new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("attacker"), attackerDefinition), GridPosition.FrontBottom, false).Build(),
            movementPoints: 6);

        WorldStack defenderStack = TestHelpers.SimpleStack(
            new WorldStackId("Defender"),
            aiDefender.Id,
            new WorldPosition(1, 0),
            new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("defender"), defenderDefinition), GridPosition.FrontBottom, false).Build(),
            movementPoints: 6);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithGlobals(globals)
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(TestHelpers.SimpleLord(aiAttacker.LordId, aiAttacker.Race, LordCategory.Warrior))
            .WithPlayer(aiAttacker)
            .WithDefinition(TestHelpers.SimpleLord(aiDefender.LordId, aiDefender.Race, LordCategory.Warrior))
            .WithPlayer(aiDefender)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithStack(attackerStack)
            .WithStack(defenderStack)
            .BuildSession();

        UnitGrid<WorldUnit> expectedAttackerUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(
                TestHelpers.SimpleUnit(new WorldUnitId("attacker"), attackerDefinition) with { Experience = 1 },
                GridPosition.FrontBottom,
                false)
            .Build();
        WorldState expectedWorld = session.World.State with
        {
            Stacks = session.World.State.Stacks
                .SetItem(
                    attackerStack.Id,
                    attackerStack with
                    {
                        Direction = WorldStackDirection.SouthEast,
                        Units = expectedAttackerUnits,
                        MovementPoints = 3,
                        BattlesWon = 1
                    })
                .Remove(defenderStack.Id),
            Tombs = ImmutableSortedDictionary<WorldTombId, WorldTomb>.Empty.Add(
                new WorldTombId("Tomb0001"),
                new WorldTomb(
                    new WorldTombId("Tomb0001"),
                    new WorldPosition(1, 0),
                    aiDefender.Id, defenderDefinition.Id, LeaderName: null, aiAttacker.Id, attackerDefinition.Id, DestroyerLeaderName: null, Turn: 1)),
            ScenarioState = session.World.State.ScenarioState with
            {
                DestroyedStacks = [new ScenarioDestroyedStack(defenderStack.Id, attackerStack.OwnerId, SourceTemplateId: null)]
            },
            AiMemories = session.World.State.AiMemories.SetItem(
                aiAttacker.Id,
                new WorldAiMemory(new WorldAiTurnMemory(1, aiAttacker.Id, 1, 0, 0, []))
                    .WithStackMemory(attackerStack.Id, new WorldAiStackMemory(
                        WorldStackOrder.Normal.Instance,
                        new WorldAiTarget.Stack(defenderStack.Id))))
        };

        session.ExecuteAiAction();

        Assert.That(session.Battle, Is.Null);
        Assert.That(session.World.State, Is.EqualTo(expectedWorld));
    }

    [Test]
    public void NotAiTurnTest()
    {
        WorldMap map = TestHelpers.SimpleMap(1, 1);

        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .BuildSession();

        WorldState expectedWorld = session.World.State;

        WorldCommandException ex = Assert.Throws<WorldCommandException>(() => session.ExecuteAiAction());

        Assert.That(ex.Error, Is.EqualTo(WorldCommandError.NotAiTurn));
        Assert.That(session.Battle, Is.Null);
        Assert.That(session.World.State, Is.EqualTo(expectedWorld));
    }

    [Test]
    public void NoPlayersRemainingTest()
    {
        WorldMap map = TestHelpers.SimpleMap(1, 1);

        WorldPlayer ai = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with { IsAi = true };

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(ai.LordId, ai.Race, LordCategory.Warrior))
            .WithPlayer(ai)
            .BuildSession();

        WorldState expectedWorld = session.World.State;

        WorldCommandException ex = Assert.Throws<WorldCommandException>(() => session.ExecuteAiAction());

        Assert.That(ex.Error, Is.EqualTo(WorldCommandError.NoPlayersRemaining));
        Assert.That(session.Battle, Is.Null);
        Assert.That(session.World.State, Is.EqualTo(expectedWorld));
    }

    [Test]
    public void MemoryTest()
    {
        WorldMap map = TestHelpers.SimpleMap(1, 1);

        WorldPlayer ai1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Heretic, map) with { IsAi = true };
        WorldPlayer ai2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Undead, map) with { IsAi = true };
        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(3), WorldRace.Human, map);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(ai1.LordId, ai1.Race, LordCategory.Warrior))
            .WithPlayer(ai1)
            .WithDefinition(TestHelpers.SimpleLord(ai2.LordId, ai2.Race, LordCategory.Warrior))
            .WithPlayer(ai2)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .BuildSession();

        WorldAiMemory ai1Memory = new(new WorldAiTurnMemory(1, ai1.Id));
        WorldAiMemory ai2Memory = new(new WorldAiTurnMemory(1, ai2.Id));

        while (session.World.State.Players[session.World.State.TurnInfo.CurrentPlayerId].IsAi)
        {
            session.ExecuteAiAction();
        }

        Assert.That(session.World.State, Is.EqualTo(new WorldState(
            Map: map,
            TurnInfo: new WorldTurnInfo(2, human.Id),
            Difficulty: WorldDifficulty.Average,
            Players: ImmutableSortedDictionary.CreateRange([
                new KeyValuePair<WorldPlayerId, WorldPlayer>(ai1.Id, ai1),
                new KeyValuePair<WorldPlayerId, WorldPlayer>(ai2.Id, ai2 with { Resources = new WorldResourceSet(100, 25, 0, 0, 0, 0) }),
                new KeyValuePair<WorldPlayerId, WorldPlayer>(human.Id, human with { Resources = new WorldResourceSet(100, 0, 0, 25, 0, 0) })
            ]),
            Relations: ImmutableSortedDictionary<WorldRelationId, WorldRelation>.Empty,
            Stacks: ImmutableSortedDictionary<WorldStackId, WorldStack>.Empty,
            Fortifications: ImmutableSortedDictionary<WorldFortificationId, WorldFortification>.Empty,
            ResourceNodes: ImmutableSortedDictionary<WorldResourceNodeId, WorldResourceNode>.Empty,
            Rods: ImmutableSortedDictionary<WorldRodId, WorldRod>.Empty,
            Bags: ImmutableSortedDictionary<WorldBagId, WorldBag>.Empty,
            Tombs: ImmutableSortedDictionary<WorldTombId, WorldTomb>.Empty,
            Landmarks: ImmutableSortedDictionary<WorldLandmarkId, WorldLandmark>.Empty,
            Mountains: ImmutableSortedDictionary<WorldMountainId, WorldMountain>.Empty,
            Sites: ImmutableSortedDictionary<WorldSiteId, WorldSite>.Empty,
            Subraces: ImmutableSortedDictionary<WorldSubraceId, WorldSubrace>.Empty,
            ScenarioState: WorldScenarioState.Empty,
            AiMemories: ImmutableSortedDictionary.CreateRange([
                new KeyValuePair<WorldPlayerId, WorldAiMemory>(ai1.Id, ai1Memory),
                new KeyValuePair<WorldPlayerId, WorldAiMemory>(ai2.Id, ai2Memory)
            ]))));
    }

    [Test]
    public void KeyStackBattleTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        UnitDefinition attackerDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 100, initiative: 50, damage: 50),
            movementPoints: 6, leadership: 6);
        UnitDefinition defenderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 40, damage: 5),
            movementPoints: 6, leadership: 6);

        WorldPlayer ai = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with { IsAi = true };
        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack keyStack = TestHelpers.SimpleStack(
            new WorldStackId("S107KC0020"),
            ai.Id,
            new WorldPosition(1, 0),
            new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("attacker"), attackerDefinition), GridPosition.FrontBottom, false).Build(),
            movementPoints: 6);

        WorldStack humanStack = TestHelpers.SimpleStack(
            new WorldStackId("HumanStack"),
            human.Id,
            new WorldPosition(2, 0),
            new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("defender"), defenderDefinition), GridPosition.FrontBottom, false).Build(),
            movementPoints: 6);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(TestHelpers.SimpleLord(ai.LordId, ai.Race, LordCategory.Warrior))
            .WithPlayer(ai)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithStack(keyStack)
            .WithStack(humanStack)
            .BuildSession();

        session.ExecuteAiAction();

        BattleSession battle = Assert.NotNull(session.Battle);
        Assert.That(battle.CanAutoResolve, Is.False);
    }

    [Test]
    public void KeyTemplateBattleTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        UnitDefinition attackerDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 100, initiative: 50, damage: 50),
            movementPoints: 6, leadership: 6);
        UnitDefinition defenderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 40, damage: 5),
            movementPoints: 6, leadership: 6);

        WorldPlayer ai = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with { IsAi = true };
        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack keyStack = TestHelpers.SimpleStack(
                new WorldStackId("Key"),
                ai.Id,
                new WorldPosition(1, 0),
                new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("attacker"), attackerDefinition), GridPosition.FrontBottom, false).Build(),
                movementPoints: 6) with
            {
                SourceTemplateId = new ScenarioStackTemplateId("S117TM0000")
            };

        WorldStack humanStack = TestHelpers.SimpleStack(
            new WorldStackId("HumanStack"),
            human.Id,
            new WorldPosition(2, 0),
            new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("defender"), defenderDefinition), GridPosition.FrontBottom, false).Build(),
            movementPoints: 6);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(TestHelpers.SimpleLord(ai.LordId, ai.Race, LordCategory.Warrior))
            .WithPlayer(ai)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithStack(keyStack)
            .WithStack(humanStack)
            .BuildSession();

        session.ExecuteAiAction();

        BattleSession battle = Assert.NotNull(session.Battle);
        Assert.That(battle.CanAutoResolve, Is.False);
    }
}
