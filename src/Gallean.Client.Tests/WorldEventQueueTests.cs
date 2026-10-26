using System.Reactive.Subjects;
using Gallean.Configuration;
using Gallean.Engine;
using Gallean.Engine.Tests;
using Gallean.Tests.Common;
using static Gallean.Client.WorldEventQueue;

namespace Gallean.Client.Tests;

public sealed class WorldEventQueueTests
{
    private static WorldState BuildState()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .BuildSession();

        return session.World.State;
    }

    private static WorldState BuildStateWithStack(WorldUnitId leaderId, WorldStackId stackId, WorldPlayerId ownerId, WorldPlayerId otherId)
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);
        WorldPlayer owner = TestHelpers.SimplePlayer(ownerId, WorldRace.Undead, map);
        WorldPlayer other = TestHelpers.SimplePlayer(otherId, WorldRace.Heretic, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("Leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);

        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leader)
            .Build();

        WorldStack stack = TestHelpers.SimpleStack(
            stackId,
            ownerId,
            new WorldPosition(0, 0),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [(TestHelpers.SimpleUnit(leaderId, leader), GridPosition.FrontBottom)]),
            movementPoints: 6);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(owner.LordId, owner.Race, LordCategory.Warrior))
            .WithPlayer(owner)
            .WithDefinition(TestHelpers.SimpleLord(other.LordId, other.Race, LordCategory.Warrior))
            .WithPlayer(other)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        return session.World.State;
    }

    private static EngineStep<WorldState, WorldEvent> Moved(
        WorldState state,
        WorldStackId stackId,
        WorldPosition from,
        WorldPosition target,
        WorldStackDirection direction)
    {
        return new EngineStep<WorldState, WorldEvent>(
            state,
            state,
            new WorldStackMovingEvent(stackId, from, target, direction, NewMovementPoints: 4, RemainingPath: [], EncounterPosition: null));
    }

    private static EngineStep<WorldState, WorldEvent> Relocated(
        WorldState state,
        WorldStackId stackId,
        WorldPosition from,
        WorldPosition position,
        WorldStackDirection direction)
    {
        return new EngineStep<WorldState, WorldEvent>(
            state,
            state,
            new WorldStackRelocatedEvent(stackId, from, position, direction));
    }

    private static EngineStep<WorldState, WorldEvent> FogRevealed(WorldState state, WorldPlayerId playerId)
    {
        return new EngineStep<WorldState, WorldEvent>(
            state,
            state,
            new WorldFogRevealedEvent(playerId, []));
    }

    private static EngineStep<WorldState, WorldEvent> UpgradeGranted(WorldState state, WorldStackId stackId)
    {
        return new EngineStep<WorldState, WorldEvent>(
            state,
            state,
            new WorldLeaderUpgradeGrantedEvent(stackId));
    }

    private static EngineStep<WorldState, WorldEvent> StackDestroyed(WorldState state, WorldStackId stackId)
    {
        return new EngineStep<WorldState, WorldEvent>(
            state,
            state with { Stacks = state.Stacks.Remove(stackId) },
            new WorldStackDestroyedEvent(stackId, Destroyer: null, DestroyerPlayerId: null));
    }

    private static EngineStep<WorldState, WorldEvent> StackRemoved(WorldState state, WorldStackId stackId)
    {
        return new EngineStep<WorldState, WorldEvent>(
            state,
            state with { Stacks = state.Stacks.Remove(stackId) },
            new WorldStackRemovedEvent(stackId));
    }

    private static EngineStep<WorldState, WorldEvent> StackSummoned(WorldState state, WorldStack stack)
    {
        return new EngineStep<WorldState, WorldEvent>(
            state,
            state with { Stacks = state.Stacks.Add(stack.Id, stack) },
            new WorldStackCreatedEvent(stack, WorldResourceSet.Empty));
    }

    private static EngineStep<WorldState, WorldEvent> LeaderPurchased(WorldState state, WorldStack stack)
    {
        return new EngineStep<WorldState, WorldEvent>(
            state,
            state with { Stacks = state.Stacks.Add(stack.Id, stack) },
            new WorldStackCreatedEvent(stack, WorldResourceSet.Empty));
    }

    private static EngineStep<WorldState, WorldEvent> SpellCast(
        WorldState state,
        WorldPlayerId casterId,
        WorldSpellDefinitionId spellId,
        WorldPosition position)
    {
        return new EngineStep<WorldState, WorldEvent>(
            state,
            state,
            new WorldSpellCastEvent(casterId, spellId, new WorldSpellTarget(position, Radius: 0), WorldResourceSet.Empty, WorldSpellCastSource.Player));
    }

    private static WorldScenarioMessage Message(WorldPlayerId triggererId)
    {
        return new WorldScenarioMessage(
            TurnNumber: 1,
            new ScenarioEventId("Event1"),
            new ScenarioTextId("Greetings Milord!"),
            PortraitId: null,
            PortraitName: null,
            IsPortraitLeft: true,
            triggererId,
            [triggererId]);
    }

    private static WorldScenarioPopupEvent Popup(WorldPlayerId triggererId, WorldPosition? triggererPosition)
    {
        return new WorldScenarioPopupEvent(
            new ScenarioEventId("Event1"),
            new ScenarioTextId("Greetings Milord!"),
            PortraitId: null,
            PortraitName: null,
            IsPortraitLeft: true,
            triggererId,
            triggererPosition,
            [triggererId]);
    }

    private static EngineStep<WorldState, WorldEvent> BattleStarted(WorldState state)
    {
        BattleState battleState = new(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder().Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder().Build(),
            CurrentUnitTurn: new BattleUnitTurn(new BattleUnitId(1), BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: null,
            Modifiers: BattleModifiers.None);

        return new EngineStep<WorldState, WorldEvent>(
            state,
            state,
            new WorldStackBattleStartedEvent(new WorldStackId("s1"), new WorldStackId("s2"), new WorldPosition(0, 2), battleState, IsDuel: false));
    }

    [Test]
    public void SingleStepTest()
    {
        WorldState state = BuildState();
        Subject<EngineStep<WorldState, WorldEvent>> events = new();
        WorldEventQueue queue = new(GameScenarioProvider.Empty, events);

        Assert.That(queue.IsIdle, Is.True);

        events.OnNext(Moved(state, new WorldStackId("s1"), new WorldPosition(0, 0), new WorldPosition(0, 1), WorldStackDirection.SouthWest));

        Assert.That(queue.IsIdle, Is.False);
        Assert.That(queue.PlayingEntry, Is.Null);
        Entry? entry = queue.PlayNext();
        Assert.That(entry, Is.EqualTo(new Entry.StackMove(
            state,
            new WorldStackId("s1"),
            [
                new Entry.StackMove.Step(new WorldPosition(0, 0), WorldStackDirection.SouthWest),
                new Entry.StackMove.Step(new WorldPosition(0, 1), WorldStackDirection.SouthWest)
            ])));
        Assert.That(queue.PlayingEntry, Is.SameAs(entry));
        Assert.That(queue.PlayNext(), Is.Null);

        queue.Stop();

        Assert.That(queue.IsIdle, Is.True);
        Assert.That(queue.PlayNext(), Is.Null);
    }

    [Test]
    public void PathStepsTest()
    {
        WorldState firstState = BuildState();
        WorldState secondState = BuildState();
        Subject<EngineStep<WorldState, WorldEvent>> events = new();
        WorldEventQueue queue = new(GameScenarioProvider.Empty, events);

        events.OnNext(Moved(firstState, new WorldStackId("s1"), new WorldPosition(0, 0), new WorldPosition(0, 1), WorldStackDirection.SouthWest));
        events.OnNext(Moved(secondState, new WorldStackId("s1"), new WorldPosition(0, 1), new WorldPosition(1, 1), WorldStackDirection.SouthEast));

        Entry entry = Assert.NotNull(queue.PlayNext());
        Assert.That(entry, Is.EqualTo(new Entry.StackMove(
            secondState,
            new WorldStackId("s1"),
            [
                new Entry.StackMove.Step(new WorldPosition(0, 0), WorldStackDirection.SouthWest),
                new Entry.StackMove.Step(new WorldPosition(0, 1), WorldStackDirection.SouthEast),
                new Entry.StackMove.Step(new WorldPosition(1, 1), WorldStackDirection.SouthEast)
            ])));
        Assert.That(entry.GetFocusPosition(new WorldPlayerId(1)), Is.EqualTo(new WorldPosition(0, 1)));

        queue.Stop();

        Assert.That(queue.IsIdle, Is.True);
    }

    [Test]
    public void RelocatedPathTest()
    {
        WorldStackId stackId = new("s1");
        WorldState firstState = BuildStateWithStack(new WorldUnitId("leader"), stackId, new WorldPlayerId(1), new WorldPlayerId(2));
        WorldState secondState = BuildStateWithStack(new WorldUnitId("leader"), stackId, new WorldPlayerId(1), new WorldPlayerId(2));
        Subject<EngineStep<WorldState, WorldEvent>> events = new();
        WorldEventQueue queue = new(GameScenarioProvider.Empty, events);

        events.OnNext(Relocated(firstState, stackId, new WorldPosition(0, 0), new WorldPosition(1, 0), WorldStackDirection.SouthEast));
        events.OnNext(Relocated(secondState, stackId, new WorldPosition(1, 0), new WorldPosition(1, 1), WorldStackDirection.South));

        Entry? entry = queue.PlayNext();
        Assert.That(entry, Is.EqualTo(new Entry.StackMove(
            secondState,
            stackId,
            [
                new Entry.StackMove.Step(new WorldPosition(0, 0), WorldStackDirection.SouthEast),
                new Entry.StackMove.Step(new WorldPosition(1, 0), WorldStackDirection.South),
                new Entry.StackMove.Step(new WorldPosition(1, 1), WorldStackDirection.South)
            ])));

        queue.Stop();

        Assert.That(queue.IsIdle, Is.True);
    }

    [Test]
    public void TwoStacksTest()
    {
        WorldState state = BuildState();
        Subject<EngineStep<WorldState, WorldEvent>> events = new();
        WorldEventQueue queue = new(GameScenarioProvider.Empty, events);

        events.OnNext(Moved(state, new WorldStackId("s1"), new WorldPosition(0, 0), new WorldPosition(0, 1), WorldStackDirection.SouthWest));
        events.OnNext(Moved(state, new WorldStackId("s2"), new WorldPosition(3, 3), new WorldPosition(3, 2), WorldStackDirection.NorthEast));

        Entry? first = queue.PlayNext();
        Assert.That(first, Is.EqualTo(new Entry.StackMove(
            state,
            new WorldStackId("s1"),
            [
                new Entry.StackMove.Step(new WorldPosition(0, 0), WorldStackDirection.SouthWest),
                new Entry.StackMove.Step(new WorldPosition(0, 1), WorldStackDirection.SouthWest)
            ])));

        queue.Stop();

        Entry? second = queue.PlayNext();
        Assert.That(second, Is.EqualTo(new Entry.StackMove(
            state,
            new WorldStackId("s2"),
            [
                new Entry.StackMove.Step(new WorldPosition(3, 3), WorldStackDirection.NorthEast),
                new Entry.StackMove.Step(new WorldPosition(3, 2), WorldStackDirection.NorthEast)
            ])));

        queue.Stop();

        Assert.That(queue.IsIdle, Is.True);
    }

    [Test]
    public void RestartedMoveTest()
    {
        WorldState state = BuildState();
        Subject<EngineStep<WorldState, WorldEvent>> events = new();
        WorldEventQueue queue = new(GameScenarioProvider.Empty, events);

        events.OnNext(Moved(state, new WorldStackId("s1"), new WorldPosition(0, 0), new WorldPosition(0, 1), WorldStackDirection.SouthWest));
        events.OnNext(Moved(state, new WorldStackId("s1"), new WorldPosition(2, 2), new WorldPosition(2, 3), WorldStackDirection.SouthWest));

        Entry? first = queue.PlayNext();
        Assert.That(first, Is.EqualTo(new Entry.StackMove(
            state,
            new WorldStackId("s1"),
            [
                new Entry.StackMove.Step(new WorldPosition(0, 0), WorldStackDirection.SouthWest),
                new Entry.StackMove.Step(new WorldPosition(0, 1), WorldStackDirection.SouthWest)
            ])));

        queue.Stop();

        Entry? second = queue.PlayNext();
        Assert.That(second, Is.EqualTo(new Entry.StackMove(
            state,
            new WorldStackId("s1"),
            [
                new Entry.StackMove.Step(new WorldPosition(2, 2), WorldStackDirection.SouthWest),
                new Entry.StackMove.Step(new WorldPosition(2, 3), WorldStackDirection.SouthWest)
            ])));
    }

    [Test]
    public void AppendAfterPromoteTest()
    {
        WorldState state = BuildState();
        Subject<EngineStep<WorldState, WorldEvent>> events = new();
        WorldEventQueue queue = new(GameScenarioProvider.Empty, events);

        events.OnNext(Moved(state, new WorldStackId("s1"), new WorldPosition(0, 0), new WorldPosition(0, 1), WorldStackDirection.SouthWest));

        Entry? first = queue.PlayNext();
        Assert.That(first, Is.EqualTo(new Entry.StackMove(
            state,
            new WorldStackId("s1"),
            [
                new Entry.StackMove.Step(new WorldPosition(0, 0), WorldStackDirection.SouthWest),
                new Entry.StackMove.Step(new WorldPosition(0, 1), WorldStackDirection.SouthWest)
            ])));

        events.OnNext(Moved(state, new WorldStackId("s1"), new WorldPosition(0, 1), new WorldPosition(0, 2), WorldStackDirection.SouthWest));

        Assert.That(queue.PlayingEntry, Is.SameAs(first));

        queue.Stop();

        Entry? second = queue.PlayNext();
        Assert.That(second, Is.EqualTo(new Entry.StackMove(
            state,
            new WorldStackId("s1"),
            [
                new Entry.StackMove.Step(new WorldPosition(0, 1), WorldStackDirection.SouthWest),
                new Entry.StackMove.Step(new WorldPosition(0, 2), WorldStackDirection.SouthWest)
            ])));
    }

    [Test]
    public void BattleTest()
    {
        WorldState state = BuildState();
        Subject<EngineStep<WorldState, WorldEvent>> events = new();
        WorldEventQueue queue = new(GameScenarioProvider.Empty, events);

        events.OnNext(Moved(state, new WorldStackId("s1"), new WorldPosition(0, 0), new WorldPosition(0, 1), WorldStackDirection.SouthWest));
        events.OnNext(BattleStarted(state));

        Entry? first = queue.PlayNext();
        Assert.That(first, Is.EqualTo(new Entry.StackMove(
            state,
            new WorldStackId("s1"),
            [
                new Entry.StackMove.Step(new WorldPosition(0, 0), WorldStackDirection.SouthWest),
                new Entry.StackMove.Step(new WorldPosition(0, 1), WorldStackDirection.SouthWest)
            ])));

        queue.Stop();

        Entry? second = queue.PlayNext();
        Assert.That(second, Is.EqualTo(new Entry.Battle(
            state,
            new WorldStackId("s1"),
            new WorldPosition(0, 2))));

        queue.Stop();

        Assert.That(queue.IsIdle, Is.True);
    }

    [Test]
    public void ExhaustedBattleTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer enemyPlayer = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map) with { IsAi = true };

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("Leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 2, leadership: 6);
        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leader)
            .Build();

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(0, 0),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader), GridPosition.FrontBottom)]),
            movementPoints: 2);
        WorldStack enemyStack = TestHelpers.SimpleStack(
            new WorldStackId("Stack2"),
            enemyPlayer.Id,
            new WorldPosition(2, 0),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [(TestHelpers.SimpleUnit(new WorldUnitId("leader2"), leader), GridPosition.FrontBottom)]),
            movementPoints: 6);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(enemyPlayer.LordId, enemyPlayer.Race, LordCategory.Warrior))
            .WithPlayer(enemyPlayer)
            .WithStack(stack)
            .WithStack(enemyStack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        using WorldEventQueue queue = new(GameScenarioProvider.Empty, session.World.Events);

        session.World.DispatchCommand(new WorldStartGameCommand());

        Entry? startEntry = queue.PlayNext();

        Entry.TurnStart turnStart = Assert.IsOfType<Entry.TurnStart>(startEntry);

        Assert.That(turnStart.PlayerId, Is.EqualTo(player.Id));

        queue.Stop();

        session.World.DispatchCommand(new WorldMoveStackCommand(stack.Id, enemyStack.Position));
        WorldState stateAfterMove = session.World.State;

        Entry? moveEntry = queue.PlayNext();

        Entry.StackMove stackMove = Assert.IsOfType<Entry.StackMove>(moveEntry);

        Assert.That(stackMove.Steps, Is.EqualTo([
            new Entry.StackMove.Step(new WorldPosition(0, 0), WorldStackDirection.SouthEast),
            new Entry.StackMove.Step(new WorldPosition(1, 0), WorldStackDirection.SouthEast)
        ]));

        queue.Stop();

        Entry? battleEntry = queue.PlayNext();
        Assert.That(battleEntry, Is.EqualTo(new Entry.Battle(stateAfterMove, stack.Id, enemyStack.Position)));

        queue.Stop();

        Assert.That(queue.IsIdle, Is.True);
    }

    [Test]
    public void EngineMoveTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("Leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leader)
            .Build();

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(0, 0),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader), GridPosition.FrontBottom)]),
            movementPoints: 6);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        using WorldEventQueue queue = new(GameScenarioProvider.Empty, session.World.Events);

        session.World.DispatchCommand(new WorldStartGameCommand());

        Entry? startEntry = queue.PlayNext();

        Entry.TurnStart turnStart = Assert.IsOfType<Entry.TurnStart>(startEntry);

        Assert.That(turnStart.PlayerId, Is.EqualTo(player.Id));

        queue.Stop();

        session.World.DispatchCommand(new WorldMoveStackCommand(stack.Id, new WorldPosition(2, 0)));
        WorldState stateAfterMove = session.World.State;

        Entry? entry = queue.PlayNext();
        Assert.That(entry, Is.EqualTo(new Entry.StackMove(
            stateAfterMove,
            stack.Id,
            [
                new Entry.StackMove.Step(new WorldPosition(0, 0), WorldStackDirection.SouthEast),
                new Entry.StackMove.Step(new WorldPosition(1, 0), WorldStackDirection.SouthEast),
                new Entry.StackMove.Step(new WorldPosition(2, 0), WorldStackDirection.SouthEast)
            ])));

        queue.Stop();

        Assert.That(queue.IsIdle, Is.True);
    }

    [Test]
    public void EngineFogRevealTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            FogOfWar = TestHelpers.BuildFogOfWar(["....?"])
        };

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("Leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 6, leadership: 6) with
            {
                VisionRadius = 1
            };
        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leader)
            .Build();

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(0, 0),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader), GridPosition.FrontBottom)]),
            movementPoints: 6);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        using WorldEventQueue queue = new(GameScenarioProvider.Empty, session.World.Events);

        session.World.DispatchCommand(new WorldStartGameCommand());

        Entry? startEntry = queue.PlayNext();

        Entry.TurnStart turnStart = Assert.IsOfType<Entry.TurnStart>(startEntry);

        Assert.That(turnStart.PlayerId, Is.EqualTo(player.Id));

        queue.Stop();

        session.World.DispatchCommand(new WorldMoveStackCommand(stack.Id, new WorldPosition(3, 0)));

        Entry? entry = queue.PlayNext();

        Entry.StackMove stackMove = Assert.IsOfType<Entry.StackMove>(entry);

        Assert.That(stackMove.Steps, Is.EqualTo([
            new Entry.StackMove.Step(new WorldPosition(0, 0), WorldStackDirection.SouthEast),
            new Entry.StackMove.Step(new WorldPosition(1, 0), WorldStackDirection.SouthEast),
            new Entry.StackMove.Step(new WorldPosition(2, 0), WorldStackDirection.SouthEast),
            new Entry.StackMove.Step(new WorldPosition(3, 0), WorldStackDirection.SouthEast)
        ]));

        queue.Stop();

        Entry? exhaustEntry = queue.PlayNext();

        Entry.StackExhaust exhaust = Assert.IsOfType<Entry.StackExhaust>(exhaustEntry);

        Assert.That(exhaust.StackId, Is.EqualTo(stack.Id));

        queue.Stop();

        Assert.That(queue.IsIdle, Is.True);
    }

    [Test]
    public void OwnFogRevealTest()
    {
        WorldStackId stackId = new("Stack1");
        WorldPlayerId ownerId = new(1);
        WorldPlayerId otherId = new(2);
        WorldState state = BuildStateWithStack(new WorldUnitId("leader"), stackId, ownerId, otherId);
        WorldState revealedState = state with { Difficulty = WorldDifficulty.Hard };

        Subject<EngineStep<WorldState, WorldEvent>> events = new();
        WorldEventQueue queue = new(GameScenarioProvider.Empty, events);

        events.OnNext(Moved(state, stackId, new WorldPosition(0, 0), new WorldPosition(1, 0), WorldStackDirection.SouthEast));
        events.OnNext(FogRevealed(revealedState, ownerId));

        Entry? entry = queue.PlayNext();

        Entry.StackMove stackMove = Assert.IsOfType<Entry.StackMove>(entry);

        Assert.That(stackMove.State, Is.SameAs(revealedState));
    }

    [Test]
    public void FogRevealAloneTest()
    {
        WorldStackId stackId = new("Stack1");
        WorldPlayerId ownerId = new(1);
        WorldPlayerId otherId = new(2);
        WorldState state = BuildStateWithStack(new WorldUnitId("leader"), stackId, ownerId, otherId);

        Subject<EngineStep<WorldState, WorldEvent>> events = new();
        WorldEventQueue queue = new(GameScenarioProvider.Empty, events);

        events.OnNext(FogRevealed(state, ownerId));

        Assert.That(queue.IsIdle, Is.True);
    }

    [Test]
    public void FogRevealAfterPromoteTest()
    {
        WorldStackId stackId = new("Stack1");
        WorldPlayerId ownerId = new(1);
        WorldPlayerId otherId = new(2);
        WorldState state = BuildStateWithStack(new WorldUnitId("leader"), stackId, ownerId, otherId);
        WorldState revealedState = state with { Difficulty = WorldDifficulty.Hard };

        Subject<EngineStep<WorldState, WorldEvent>> events = new();
        WorldEventQueue queue = new(GameScenarioProvider.Empty, events);

        events.OnNext(Moved(state, stackId, new WorldPosition(0, 0), new WorldPosition(1, 0), WorldStackDirection.SouthEast));

        Entry entry = Assert.NotNull(queue.PlayNext());

        events.OnNext(FogRevealed(revealedState, ownerId));

        Assert.That(queue.PlayingEntry, Is.SameAs(entry));
        Assert.That(entry.State, Is.SameAs(state));

        queue.Stop();

        Assert.That(queue.IsIdle, Is.True);
    }

    [Test]
    public void OtherPlayerFogRevealTest()
    {
        WorldStackId stackId = new("Stack1");
        WorldPlayerId ownerId = new(1);
        WorldPlayerId otherId = new(2);
        WorldState state = BuildStateWithStack(new WorldUnitId("leader"), stackId, ownerId, otherId);
        WorldState revealedState = state with { Difficulty = WorldDifficulty.Hard };

        Subject<EngineStep<WorldState, WorldEvent>> events = new();
        WorldEventQueue queue = new(GameScenarioProvider.Empty, events);

        events.OnNext(Moved(state, stackId, new WorldPosition(0, 0), new WorldPosition(1, 0), WorldStackDirection.SouthEast));
        events.OnNext(FogRevealed(revealedState, otherId));

        Entry? entry = queue.PlayNext();

        Entry.StackMove stackMove = Assert.IsOfType<Entry.StackMove>(entry);

        Assert.That(stackMove.State, Is.SameAs(state));
    }

    [Test]
    public void LeaderUpgradePerGrantTest()
    {
        WorldStackId stackId = new("Stack1");
        WorldStackId otherStackId = new("Stack2");
        WorldPlayerId ownerId = new(1);
        WorldPlayerId otherId = new(2);
        WorldState singleStackState = BuildStateWithStack(new WorldUnitId("leader"), stackId, ownerId, otherId);
        WorldStack otherStack = singleStackState.Stacks[stackId] with
        {
            Id = otherStackId,
            Position = new WorldPosition(1, 0)
        };
        WorldState state = singleStackState with
        {
            Stacks = singleStackState.Stacks.Add(otherStackId, otherStack)
        };
        WorldState laterState = state with { Difficulty = WorldDifficulty.Hard };

        Subject<EngineStep<WorldState, WorldEvent>> events = new();
        WorldEventQueue queue = new(GameScenarioProvider.Empty, events);

        events.OnNext(UpgradeGranted(state, stackId));
        events.OnNext(UpgradeGranted(laterState, stackId));
        events.OnNext(UpgradeGranted(laterState, otherStackId));

        Entry? first = queue.PlayNext();
        Assert.That(first, Is.EqualTo(new Entry.LeaderUpgrade(state, stackId)));

        queue.Stop();

        Entry? second = queue.PlayNext();
        Assert.That(second, Is.EqualTo(new Entry.LeaderUpgrade(laterState, stackId)));

        queue.Stop();

        Entry? third = queue.PlayNext();
        Assert.That(third, Is.EqualTo(new Entry.LeaderUpgrade(laterState, otherStackId)));

        queue.Stop();

        Assert.That(queue.IsIdle, Is.True);
    }

    [Test]
    public void LeaderUpgradeOnAnotherTurnTest()
    {
        WorldStackId stackId = new("Stack1");
        WorldPlayerId ownerId = new(1);
        WorldPlayerId otherId = new(2);
        WorldState ownTurnState = BuildStateWithStack(new WorldUnitId("leader"), stackId, ownerId, otherId);
        WorldState state = ownTurnState with
        {
            TurnInfo = ownTurnState.TurnInfo with { CurrentPlayerId = otherId }
        };

        Subject<EngineStep<WorldState, WorldEvent>> events = new();
        WorldEventQueue queue = new(GameScenarioProvider.Empty, events);

        events.OnNext(UpgradeGranted(state, stackId));

        Assert.That(queue.IsIdle, Is.True);
    }

    [Test]
    public void LeaderUpgradeAfterAnotherTurnTest()
    {
        WorldStackId stackId = new("Stack1");
        WorldPlayerId ownerId = new(1);
        WorldPlayerId otherId = new(2);
        WorldState ownTurnState = BuildStateWithStack(new WorldUnitId("leader"), stackId, ownerId, otherId);
        WorldState grantedState = ownTurnState with
        {
            Stacks = ownTurnState.Stacks.SetItem(stackId, ownTurnState.Stacks[stackId] with { PendingLeaderUpgrades = 1 })
        };
        WorldState otherTurnState = grantedState with
        {
            TurnInfo = grantedState.TurnInfo with { CurrentPlayerId = otherId }
        };

        Subject<EngineStep<WorldState, WorldEvent>> events = new();
        WorldEventQueue queue = new(GameScenarioProvider.Empty, events);

        events.OnNext(UpgradeGranted(otherTurnState, stackId));
        events.OnNext(new EngineStep<WorldState, WorldEvent>(otherTurnState, grantedState, new WorldTurnStartedEvent(ownerId)));

        Entry? first = queue.PlayNext();
        Assert.That(first, Is.EqualTo(new Entry.TurnStart(grantedState, ownerId)));

        queue.Stop();

        Entry? second = queue.PlayNext();
        Assert.That(second, Is.EqualTo(new Entry.LeaderUpgrade(grantedState, stackId)));

        queue.Stop();

        Assert.That(queue.IsIdle, Is.True);
    }

    [Test]
    public void LeaderUpgradePostponedTest()
    {
        WorldStackId stackId = new("Stack1");
        WorldPlayerId ownerId = new(1);
        WorldPlayerId otherId = new(2);
        WorldState initialState = BuildStateWithStack(new WorldUnitId("leader"), stackId, ownerId, otherId);
        WorldState state = initialState with
        {
            Stacks = initialState.Stacks.SetItem(
                stackId,
                initialState.Stacks[stackId] with { PendingLeaderUpgrades = 1, LeaderUpgradePostponed = true })
        };

        Subject<EngineStep<WorldState, WorldEvent>> events = new();
        WorldEventQueue queue = new(GameScenarioProvider.Empty, events);

        events.OnNext(new EngineStep<WorldState, WorldEvent>(state, state, new WorldTurnStartedEvent(ownerId)));

        Entry? first = queue.PlayNext();
        Assert.That(first, Is.EqualTo(new Entry.TurnStart(state, ownerId)));

        queue.Stop();

        Assert.That(queue.IsIdle, Is.True);
    }

    [Test]
    public void LeaderUpgradeForAiTest()
    {
        WorldStackId stackId = new("Stack1");
        WorldPlayerId ownerId = new(1);
        WorldPlayerId otherId = new(2);
        WorldState humanState = BuildStateWithStack(new WorldUnitId("leader"), stackId, ownerId, otherId);
        WorldState state = humanState with
        {
            Players = humanState.Players.SetItem(ownerId, humanState.Players[ownerId] with { IsAi = true })
        };

        Subject<EngineStep<WorldState, WorldEvent>> events = new();
        WorldEventQueue queue = new(GameScenarioProvider.Empty, events);

        events.OnNext(UpgradeGranted(state, stackId));

        Assert.That(queue.IsIdle, Is.True);
    }

    [Test]
    public void StackDestroyTest()
    {
        WorldStackId stackId = new("Stack1");
        WorldState state = BuildStateWithStack(new WorldUnitId("leader"), stackId, new WorldPlayerId(1), new WorldPlayerId(2));
        EngineStep<WorldState, WorldEvent> step = StackDestroyed(state, stackId);

        Subject<EngineStep<WorldState, WorldEvent>> events = new();
        WorldEventQueue queue = new(GameScenarioProvider.Empty, events);

        events.OnNext(step);

        Entry? entry = queue.PlayNext();
        Assert.That(entry, Is.EqualTo(new Entry.StackDestroy(step.NewState, state.Stacks[stackId])));

        queue.Stop();

        Assert.That(queue.IsIdle, Is.True);
    }

    [Test]
    public void SeparateStackDestroyTest()
    {
        WorldStackId stackId = new("Stack1");
        WorldStackId otherStackId = new("Stack2");
        WorldState state = BuildStateWithStack(new WorldUnitId("leader"), stackId, new WorldPlayerId(1), new WorldPlayerId(2));
        WorldStack other = state.Stacks[stackId] with { Id = otherStackId, Position = new WorldPosition(2, 0) };
        WorldState bothState = state with { Stacks = state.Stacks.Add(otherStackId, other) };

        EngineStep<WorldState, WorldEvent> first = StackDestroyed(bothState, stackId);
        EngineStep<WorldState, WorldEvent> second = StackDestroyed(first.NewState, otherStackId);

        Subject<EngineStep<WorldState, WorldEvent>> events = new();
        WorldEventQueue queue = new(GameScenarioProvider.Empty, events);

        events.OnNext(first);
        events.OnNext(second);

        Entry? firstEntry = queue.PlayNext();
        Assert.That(firstEntry, Is.EqualTo(new Entry.StackDestroy(
            first.NewState,
            bothState.Stacks[stackId])));

        queue.Stop();

        Entry? secondEntry = queue.PlayNext();
        Assert.That(secondEntry, Is.EqualTo(new Entry.StackDestroy(
            second.NewState,
            other)));

        queue.Stop();

        Assert.That(queue.IsIdle, Is.True);
    }

    [Test]
    public void StackRemoveTest()
    {
        WorldStackId stackId = new("Stack1");
        WorldState state = BuildStateWithStack(new WorldUnitId("leader"), stackId, new WorldPlayerId(1), new WorldPlayerId(2));
        EngineStep<WorldState, WorldEvent> step = StackRemoved(state, stackId);

        Subject<EngineStep<WorldState, WorldEvent>> events = new();
        WorldEventQueue queue = new(GameScenarioProvider.Empty, events);

        events.OnNext(step);

        Entry? entry = queue.PlayNext();
        Assert.That(entry, Is.EqualTo(new Entry.StackRemove(step.NewState, state.Stacks[stackId])));

        queue.Stop();

        Assert.That(queue.IsIdle, Is.True);
    }

    [Test]
    public void UnknownStackDestroyTest()
    {
        WorldState state = BuildStateWithStack(new WorldUnitId("stack1Leader"), new WorldStackId("Stack1"), new WorldPlayerId(1), new WorldPlayerId(2));

        Subject<EngineStep<WorldState, WorldEvent>> events = new();
        WorldEventQueue queue = new(GameScenarioProvider.Empty, events);

        events.OnNext(StackDestroyed(state, new WorldStackId("Missing")));

        Assert.That(queue.IsIdle, Is.True);
    }

    [Test]
    public void StackCreateTest()
    {
        WorldStackId stackId = new("Stack1");
        WorldState state = BuildStateWithStack(new WorldUnitId("leader"), stackId, new WorldPlayerId(1), new WorldPlayerId(2));
        WorldStack summoned = state.Stacks[stackId] with { Id = new WorldStackId("Summoned"), Position = new WorldPosition(2, 0) };
        EngineStep<WorldState, WorldEvent> step = StackSummoned(state, summoned);

        Subject<EngineStep<WorldState, WorldEvent>> events = new();
        WorldEventQueue queue = new(GameScenarioProvider.Empty, events);

        events.OnNext(step);

        Entry? entry = queue.PlayNext();
        Assert.That(entry, Is.EqualTo(new Entry.StackCreate(step.NewState, summoned)));

        queue.Stop();

        Assert.That(queue.IsIdle, Is.True);
    }

    [Test]
    public void PurchasedLeaderCreateTest()
    {
        WorldStackId stackId = new("Stack1");
        WorldState state = BuildStateWithStack(new WorldUnitId("leader"), stackId, new WorldPlayerId(1), new WorldPlayerId(2));
        WorldStack purchased = state.Stacks[stackId] with { Id = new WorldStackId("Hired"), Position = new WorldPosition(1, 1) };
        EngineStep<WorldState, WorldEvent> step = LeaderPurchased(state, purchased);

        Subject<EngineStep<WorldState, WorldEvent>> events = new();
        WorldEventQueue queue = new(GameScenarioProvider.Empty, events);

        events.OnNext(step);

        Entry? entry = queue.PlayNext();
        Assert.That(entry, Is.EqualTo(new Entry.StackCreate(step.NewState, purchased)));

        queue.Stop();

        Assert.That(queue.IsIdle, Is.True);
    }

    [Test]
    public void CapitalLeaderCreateTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 5);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("Leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);

        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leader)
            .Build();

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCapital(new WorldCapital(
                Id: new WorldFortificationId("capital"),
                NameTextId: new ScenarioTextId("Name"),
                OwnerId: player.Id,
                Footprint: new WorldFootprint(new WorldPosition(0, 0), 5, 5),
                Garrison: UnitGrid<WorldUnit>.Empty,
                Inventory: WorldInventory.Empty,
                AiPriority: 1,
                CapturedById: null))
            .BuildSession();

        WorldStack hired = TestHelpers.SimpleStack(
            new WorldStackId("Hired"),
            player.Id,
            new WorldPosition(4, 4),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader), GridPosition.FrontBottom)]),
            movementPoints: 6);

        Subject<EngineStep<WorldState, WorldEvent>> events = new();
        WorldEventQueue queue = new(GameScenarioProvider.Empty, events);

        events.OnNext(LeaderPurchased(session.World.State, hired));

        Assert.That(queue.IsIdle, Is.True);
    }

    [Test]
    public void SeparateStackCreateTest()
    {
        WorldStackId stackId = new("Stack1");
        WorldState state = BuildStateWithStack(new WorldUnitId("leader"), stackId, new WorldPlayerId(1), new WorldPlayerId(2));
        WorldStack first = state.Stacks[stackId] with { Id = new WorldStackId("Summoned1"), Position = new WorldPosition(2, 0) };
        WorldStack second = state.Stacks[stackId] with { Id = new WorldStackId("Summoned2"), Position = new WorldPosition(2, 1) };

        EngineStep<WorldState, WorldEvent> firstStep = StackSummoned(state, first);
        EngineStep<WorldState, WorldEvent> secondStep = LeaderPurchased(firstStep.NewState, second);

        Subject<EngineStep<WorldState, WorldEvent>> events = new();
        WorldEventQueue queue = new(GameScenarioProvider.Empty, events);

        events.OnNext(firstStep);
        events.OnNext(secondStep);

        Entry? firstEntry = queue.PlayNext();
        Assert.That(firstEntry, Is.EqualTo(new Entry.StackCreate(firstStep.NewState, first)));

        queue.Stop();

        Entry? secondEntry = queue.PlayNext();
        Assert.That(secondEntry, Is.EqualTo(new Entry.StackCreate(secondStep.NewState, second)));

        queue.Stop();

        Assert.That(queue.IsIdle, Is.True);
    }

    [Test]
    public void SummonSpellTest()
    {
        WorldStackId stackId = new("Stack1");
        WorldPlayerId casterId = new(1);
        WorldSpellDefinitionId spellId = new("G000SS0008");
        WorldPosition target = new(2, 0);
        WorldState state = BuildStateWithStack(new WorldUnitId("leader"), stackId, casterId, new WorldPlayerId(2));
        WorldStack summoned = state.Stacks[stackId] with { Id = new WorldStackId("Summoned"), Position = target };
        EngineStep<WorldState, WorldEvent> cast = SpellCast(state, casterId, spellId, target);
        EngineStep<WorldState, WorldEvent> created = StackSummoned(state, summoned);

        Subject<EngineStep<WorldState, WorldEvent>> events = new();
        WorldEventQueue queue = new(GameScenarioProvider.Empty, events);

        events.OnNext(cast);
        events.OnNext(created);

        Entry? entry = queue.PlayNext();
        Assert.That(entry, Is.EqualTo(new Entry.SpellCast(created.NewState, casterId, spellId, target, summoned.Id)));

        queue.Stop();

        Assert.That(queue.IsIdle, Is.True);
    }

    [Test]
    public void SpellWithoutSummonTest()
    {
        WorldPlayerId casterId = new(1);
        WorldSpellDefinitionId spellId = new("G000SS0002");
        WorldPosition target = new(1, 0);
        WorldState state = BuildState();
        EngineStep<WorldState, WorldEvent> cast = SpellCast(state, casterId, spellId, target);

        Subject<EngineStep<WorldState, WorldEvent>> events = new();
        WorldEventQueue queue = new(GameScenarioProvider.Empty, events);

        events.OnNext(cast);

        Entry? entry = queue.PlayNext();
        Assert.That(entry, Is.EqualTo(new Entry.SpellCast(cast.NewState, casterId, spellId, target, SummonedStackId: null)));

        queue.Stop();

        Assert.That(queue.IsIdle, Is.True);
    }

    [Test]
    public void SummonAtOtherPositionTest()
    {
        WorldStackId stackId = new("Stack1");
        WorldPlayerId casterId = new(1);
        WorldSpellDefinitionId spellId = new("G000SS0008");
        WorldPosition target = new(2, 0);
        WorldState state = BuildStateWithStack(new WorldUnitId("leader"), stackId, casterId, new WorldPlayerId(2));
        WorldStack summoned = state.Stacks[stackId] with { Id = new WorldStackId("Summoned"), Position = new WorldPosition(1, 1) };
        EngineStep<WorldState, WorldEvent> cast = SpellCast(state, casterId, spellId, target);
        EngineStep<WorldState, WorldEvent> created = StackSummoned(state, summoned);

        Subject<EngineStep<WorldState, WorldEvent>> events = new();
        WorldEventQueue queue = new(GameScenarioProvider.Empty, events);

        events.OnNext(cast);
        events.OnNext(created);

        Entry? first = queue.PlayNext();
        Assert.That(first, Is.EqualTo(new Entry.SpellCast(cast.NewState, casterId, spellId, target, SummonedStackId: null)));

        queue.Stop();

        Entry? second = queue.PlayNext();
        Assert.That(second, Is.EqualTo(new Entry.StackCreate(created.NewState, summoned)));
    }

    [Test]
    public void SpellFocusTest()
    {
        WorldPlayerId casterId = new(1);
        WorldPosition target = new(2, 0);
        WorldState state = BuildState();
        Entry.SpellCast entry = new(state, casterId, new WorldSpellDefinitionId("G000SS0008"), target, SummonedStackId: null);

        Assert.That(entry.GetFocusPosition(casterId), Is.Null);
        Assert.That(entry.GetFocusPosition(new WorldPlayerId(2)), Is.EqualTo(target));
    }

    [Test]
    public void RelocatedStackFocusTest()
    {
        WorldStackId stackId = new("Stack1");
        WorldPlayerId owner = new(1);
        WorldState state = BuildStateWithStack(new WorldUnitId("leader"), stackId, owner, new WorldPlayerId(2));

        Subject<EngineStep<WorldState, WorldEvent>> events = new();
        WorldEventQueue queue = new(GameScenarioProvider.Empty, events);

        events.OnNext(Relocated(state, stackId, new WorldPosition(0, 0), new WorldPosition(1, 0), WorldStackDirection.SouthEast));

        Entry entry = Assert.NotNull(queue.PlayNext());
        Assert.That(entry, Is.EqualTo(new Entry.StackMove(
            state,
            stackId,
            [
                new Entry.StackMove.Step(new WorldPosition(0, 0), WorldStackDirection.SouthEast),
                new Entry.StackMove.Step(new WorldPosition(1, 0), WorldStackDirection.SouthEast)
            ])));
        Assert.That(entry.GetFocusPosition(owner), Is.EqualTo(new WorldPosition(1, 0)));
    }

    [Test]
    public void MovedStackFocusTest()
    {
        WorldStackId stackId = new("Stack1");
        WorldPlayerId owner = new(1);
        WorldState state = BuildStateWithStack(new WorldUnitId("leader"), stackId, owner, new WorldPlayerId(2));

        Subject<EngineStep<WorldState, WorldEvent>> events = new();
        WorldEventQueue queue = new(GameScenarioProvider.Empty, events);

        events.OnNext(Moved(state, stackId, new WorldPosition(0, 0), new WorldPosition(1, 0), WorldStackDirection.SouthEast));

        Entry entry = Assert.NotNull(queue.PlayNext());
        Assert.That(entry.GetFocusPosition(owner), Is.EqualTo(new WorldPosition(1, 0)));
        Assert.That(entry.GetFocusPosition(new WorldPlayerId(2)), Is.EqualTo(new WorldPosition(1, 0)));
    }

    [Test]
    public void StackCreateFocusTest()
    {
        WorldStackId stackId = new("Stack1");
        WorldPlayerId owner = new(1);
        WorldState state = BuildStateWithStack(new WorldUnitId("leader"), stackId, owner, new WorldPlayerId(2));
        WorldStack purchased = state.Stacks[stackId] with { Id = new WorldStackId("Hired"), Position = new WorldPosition(1, 1) };
        EngineStep<WorldState, WorldEvent> step = LeaderPurchased(state, purchased);

        Subject<EngineStep<WorldState, WorldEvent>> events = new();
        WorldEventQueue queue = new(GameScenarioProvider.Empty, events);

        events.OnNext(step);

        Entry entry = Assert.NotNull(queue.PlayNext());
        Assert.That(entry.GetFocusPosition(owner), Is.EqualTo(new WorldPosition(1, 1)));
        Assert.That(entry.GetFocusPosition(new WorldPlayerId(2)), Is.EqualTo(new WorldPosition(1, 1)));
    }

    [Test]
    public void FortificationCapturedFocusTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer defender = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldPlayer attacker = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Human, 1),
            WorldResourceSet.Empty);
        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("City1"),
            cityDefinition,
            defender.Id,
            new WorldPosition(3, 3),
            UnitGrid<WorldUnit>.Empty);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(defender.LordId, defender.Race, LordCategory.Warrior))
            .WithPlayer(defender)
            .WithDefinition(TestHelpers.SimpleLord(attacker.LordId, attacker.Race, LordCategory.Warrior))
            .WithPlayer(attacker)
            .WithCity(city)
            .BuildSession();

        WorldState state = session.World.State;
        Subject<EngineStep<WorldState, WorldEvent>> events = new();
        WorldEventQueue queue = new(GameScenarioProvider.Empty, events);

        events.OnNext(new EngineStep<WorldState, WorldEvent>(
            state,
            state,
            new WorldFortificationCapturingEvent(city.Id, defender.Id, attacker.Id, new WorldStackId("Stack1"))));

        Entry entry = Assert.NotNull(queue.PlayNext());
        Assert.That(entry, Is.EqualTo(new Entry.Pause(state, city.Entrance, attacker.Id, TimeSpan.FromSeconds(0.75))));
        Assert.That(entry.GetFocusPosition(defender.Id), Is.EqualTo(city.Entrance));
        Assert.That(entry.GetFocusPosition(attacker.Id), Is.Null);

        queue.Stop();

        Assert.That(queue.IsIdle, Is.True);
    }

    [Test]
    public void CityRiotEndedTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer owner = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Human, 1),
            WorldResourceSet.Empty);
        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("City1"),
            cityDefinition,
            owner.Id,
            new WorldPosition(3, 3),
            UnitGrid<WorldUnit>.Empty);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(owner.LordId, owner.Race, LordCategory.Warrior))
            .WithPlayer(owner)
            .WithCity(city)
            .BuildSession();

        WorldState state = session.World.State;
        Subject<EngineStep<WorldState, WorldEvent>> events = new();
        WorldEventQueue queue = new(GameScenarioProvider.Empty, events);

        events.OnNext(new EngineStep<WorldState, WorldEvent>(state, state, new WorldCityRiotEndedEvent(city.Id)));

        Entry entry = Assert.NotNull(queue.PlayNext());
        Assert.That(entry, Is.EqualTo(new Entry.CityRiotEnd(state, city.Entrance)));
        Assert.That(entry.GetFocusPosition(owner.Id), Is.Null);
    }

    [Test]
    public void FortificationEnteredTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldRuin ruin = new(
            Id: new WorldFortificationId("Ruin1"),
            NameTextId: new ScenarioTextId("Ruin"),
            Footprint: new WorldFootprint(new WorldPosition(3, 3), 3, 3),
            Variant: new WorldObjectVariant(0),
            Garrison: UnitGrid<WorldUnit>.Empty,
            Resources: new WorldResourceSet(300, 0, 0, 0, 0, 0),
            Item: null,
            LooterId: null,
            AiPriority: 1);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithRuin(ruin)
            .BuildSession();

        WorldState state = session.World.State;
        Subject<EngineStep<WorldState, WorldEvent>> events = new();
        WorldEventQueue queue = new(GameScenarioProvider.Empty, events);

        events.OnNext(new EngineStep<WorldState, WorldEvent>(state, state, new WorldFortificationEnteredEvent(new WorldStackId("Stack1"), ruin.Id)));

        Entry entry = Assert.NotNull(queue.PlayNext());
        Assert.That(entry, Is.EqualTo(new Entry.FortificationEnter(state, ruin.Entrance)));
        Assert.That(entry.GetFocusPosition(player.Id), Is.Null);
    }

    [Test]
    public void FortificationExitedTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldRuin ruin = new(
            Id: new WorldFortificationId("Ruin1"),
            NameTextId: new ScenarioTextId("Ruin"),
            Footprint: new WorldFootprint(new WorldPosition(3, 3), 3, 3),
            Variant: new WorldObjectVariant(0),
            Garrison: UnitGrid<WorldUnit>.Empty,
            Resources: new WorldResourceSet(300, 0, 0, 0, 0, 0),
            Item: null,
            LooterId: null,
            AiPriority: 1);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithRuin(ruin)
            .BuildSession();

        WorldState state = session.World.State;
        Subject<EngineStep<WorldState, WorldEvent>> events = new();
        WorldEventQueue queue = new(GameScenarioProvider.Empty, events);

        events.OnNext(new EngineStep<WorldState, WorldEvent>(state, state, new WorldFortificationExitedEvent(new WorldStackId("Stack1"), ruin.Id)));

        Entry entry = Assert.NotNull(queue.PlayNext());
        Assert.That(entry, Is.EqualTo(new Entry.FortificationExit(state, ruin.Entrance, new WorldStackId("Stack1"))));
        Assert.That(entry.GetFocusPosition(player.Id), Is.Null);
    }

    [Test]
    public void RuinLootedTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldRuin ruin = new(
            Id: new WorldFortificationId("Ruin1"),
            NameTextId: new ScenarioTextId("Ruin"),
            Footprint: new WorldFootprint(new WorldPosition(3, 3), 3, 3),
            Variant: new WorldObjectVariant(0),
            Garrison: UnitGrid<WorldUnit>.Empty,
            Resources: new WorldResourceSet(300, 0, 0, 0, 0, 0),
            Item: null,
            LooterId: null,
            AiPriority: 1);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithRuin(ruin)
            .BuildSession();

        WorldState state = session.World.State;
        Subject<EngineStep<WorldState, WorldEvent>> events = new();
        WorldEventQueue queue = new(GameScenarioProvider.Empty, events);

        events.OnNext(new EngineStep<WorldState, WorldEvent>(
            state,
            state,
            new WorldRuinLootedEvent(ruin.Id, new WorldStackId("Stack1"), new WorldResourceSet(300, 0, 0, 0, 0, 0), Item: null)));

        Entry entry = Assert.NotNull(queue.PlayNext());
        Assert.That(entry, Is.EqualTo(new Entry.RuinLoot(
            state,
            ruin.Entrance,
            new WorldStackId("Stack1"),
            new WorldResourceSet(300, 0, 0, 0, 0, 0),
            Item: null)));
        Assert.That(entry.GetFocusPosition(player.Id), Is.Null);
    }

    [Test]
    public void BagPickedUpTest()
    {
        WorldStackId stackId = new("Stack1");
        WorldState state = BuildStateWithStack(new WorldUnitId("leader"), stackId, new WorldPlayerId(1), new WorldPlayerId(2));
        WorldInventory items = new([new WorldItem(new WorldItemId("BagItem"), new ItemDefinitionId("Item1"), Charges: null)]);
        Subject<EngineStep<WorldState, WorldEvent>> events = new();
        WorldEventQueue queue = new(GameScenarioProvider.Empty, events);

        events.OnNext(new EngineStep<WorldState, WorldEvent>(
            state,
            state,
            new WorldBagPickedUpEvent(stackId, new WorldBagId("Bag1"), items)));

        Entry pause = Assert.NotNull(queue.PlayNext());
        Assert.That(pause, Is.EqualTo(new Entry.Pause(state, new WorldPosition(0, 0), new WorldPlayerId(1), TimeSpan.FromSeconds(0.75))));

        queue.Stop();

        Entry entry = Assert.NotNull(queue.PlayNext());
        Assert.That(entry, Is.EqualTo(new Entry.BagPickUp(state, stackId, items)));
        Assert.That(entry.GetFocusPosition(new WorldPlayerId(1)), Is.Null);
    }

    [Test]
    public void RodCreatedFocusTest()
    {
        WorldPlayerId owner = new(1);
        WorldState state = BuildState();
        Subject<EngineStep<WorldState, WorldEvent>> events = new();
        WorldEventQueue queue = new(GameScenarioProvider.Empty, events);

        events.OnNext(new EngineStep<WorldState, WorldEvent>(
            state,
            state,
            new WorldRodCreatedEvent(new WorldRodId("Rod1"), owner, new WorldPosition(2, 0))));

        Entry entry = Assert.NotNull(queue.PlayNext());
        Assert.That(entry, Is.EqualTo(new Entry.Pause(state, new WorldPosition(2, 0), owner, TimeSpan.FromSeconds(0.75))));
        Assert.That(entry.GetFocusPosition(owner), Is.Null);
        Assert.That(entry.GetFocusPosition(new WorldPlayerId(2)), Is.EqualTo(new WorldPosition(2, 0)));
    }

    [Test]
    public void RodDestroyedFocusTest()
    {
        WorldPlayerId destroyer = new(1);
        WorldPlayerId owner = new(2);
        WorldState state = BuildState();
        Subject<EngineStep<WorldState, WorldEvent>> events = new();
        WorldEventQueue queue = new(GameScenarioProvider.Empty, events);

        events.OnNext(new EngineStep<WorldState, WorldEvent>(
            state,
            state,
            new WorldRodDestroyedEvent(new WorldRodId("Rod1"), destroyer, owner, new WorldPosition(2, 0))));

        Entry entry = Assert.NotNull(queue.PlayNext());
        Assert.That(entry, Is.EqualTo(new Entry.Pause(state, new WorldPosition(2, 0), destroyer, TimeSpan.FromSeconds(0.75))));
        Assert.That(entry.GetFocusPosition(destroyer), Is.Null);
        Assert.That(entry.GetFocusPosition(owner), Is.EqualTo(new WorldPosition(2, 0)));
    }

    [Test]
    public void ScenarioPopupTest()
    {
        WorldPlayerId triggerer = new(1);
        WorldState state = BuildState();
        WorldScenarioMessage message = Message(triggerer);
        WorldState newState = state with { ScenarioState = state.ScenarioState with { Messages = [message] } };
        WorldScenarioPopupEvent popup = Popup(triggerer, new WorldPosition(2, 0));
        Subject<EngineStep<WorldState, WorldEvent>> events = new();
        WorldEventQueue queue = new(GameScenarioProvider.Empty, events);

        events.OnNext(new EngineStep<WorldState, WorldEvent>(state, newState, popup));

        Entry pause = Assert.NotNull(queue.PlayNext());
        Assert.That(pause, Is.EqualTo(new Entry.Pause(newState, new WorldPosition(2, 0), null, TimeSpan.FromSeconds(0.25))));
        Assert.That(pause.GetFocusPosition(triggerer), Is.EqualTo(new WorldPosition(2, 0)));
        Assert.That(pause.GetFocusPosition(new WorldPlayerId(2)), Is.EqualTo(new WorldPosition(2, 0)));

        queue.Stop();

        Entry entry = Assert.NotNull(queue.PlayNext());
        Assert.That(entry, Is.EqualTo(new Entry.ScenarioPopup(newState, [message])));
        Assert.That(entry.GetFocusPosition(triggerer), Is.Null);
    }

    [Test]
    public void ScenarioPopupWithoutPositionTest()
    {
        WorldPlayerId triggerer = new(1);
        WorldState state = BuildState();
        WorldScenarioMessage message = Message(triggerer);
        WorldState newState = state with { ScenarioState = state.ScenarioState with { Messages = [message] } };
        WorldScenarioPopupEvent popup = Popup(triggerer, triggererPosition: null);
        Subject<EngineStep<WorldState, WorldEvent>> events = new();
        WorldEventQueue queue = new(GameScenarioProvider.Empty, events);

        events.OnNext(new EngineStep<WorldState, WorldEvent>(state, newState, popup));

        Entry entry = Assert.NotNull(queue.PlayNext());
        Assert.That(entry, Is.EqualTo(new Entry.ScenarioPopup(newState, [message])));
        Assert.That(entry.GetFocusPosition(triggerer), Is.Null);

        queue.Stop();

        Assert.That(queue.IsIdle, Is.True);
    }

    [Test]
    public void LeaderUpgradeFocusTest()
    {
        WorldStackId stackId = new("Stack1");
        WorldPlayerId owner = new(1);
        WorldState state = BuildStateWithStack(new WorldUnitId("leader"), stackId, owner, new WorldPlayerId(2));
        Entry.LeaderUpgrade entry = new(state, stackId);

        Assert.That(entry.GetFocusPosition(owner), Is.EqualTo(new WorldPosition(0, 0)));
    }

    [Test]
    public void MountainsRemovedTest()
    {
        ScenarioLocationDefinition location = new(
            new ScenarioLocationId("Pass"),
            new ScenarioTextId("Pass"),
            new WorldPosition(1, 0),
            Radius: 2);

        WorldMap map = TestHelpers.SimpleMap(3, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithScenarioLocation(location)
            .BuildSession();

        WorldState state = session.World.State;
        Subject<EngineStep<WorldState, WorldEvent>> events = new();
        WorldEventQueue queue = new(engine.Scenario, events);

        events.OnNext(new EngineStep<WorldState, WorldEvent>(
            state,
            state,
            new WorldMountainsRemovedEvent(location.Id, [new WorldMountainId(1)])));

        Entry entry = Assert.NotNull(queue.PlayNext());
        Assert.That(entry, Is.EqualTo(new Entry.MountainRemove(state, new WorldPosition(1, 0), Radius: 2)));
        Assert.That(entry.GetFocusPosition(new WorldPlayerId(1)), Is.EqualTo(new WorldPosition(1, 0)));
    }

    [Test]
    public void ScenarioUnfogTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer other = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        ScenarioLocationDefinition watchtower = new(
            new ScenarioLocationId("Watchtower"),
            new ScenarioTextId("Watchtower"),
            new WorldPosition(2, 0),
            Radius: 1);

        ScenarioLocationDefinition crypt = new(
            new ScenarioLocationId("Crypt"),
            new ScenarioTextId("Crypt"),
            new WorldPosition(0, 0),
            Radius: 1);

        ScenarioEventDefinition scenarioEvent = new(
            Id: new ScenarioEventId("Event1"),
            AffectedRaces: [WorldRace.Undead],
            VerifiedRaces: [WorldRace.Undead],
            Conditions: [new ScenarioEventFrequencyCondition(1)],
            Effects:
            [
                new ScenarioEventUnfogEffect(watchtower.Id, Reveal: true, Radius: null, [player.Id, other.Id]),
                new ScenarioEventUnfogEffect(crypt.Id, Reveal: false, Radius: null, [player.Id])
            ],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(other.LordId, other.Race, LordCategory.Warrior))
            .WithPlayer(other)
            .WithScenarioLocation(watchtower)
            .WithScenarioLocation(crypt)
            .WithScenarioEvent(scenarioEvent)
            .BuildSession();

        WorldState state = session.World.State;
        Subject<EngineStep<WorldState, WorldEvent>> events = new();
        WorldEventQueue queue = new(engine.Scenario, events);

        events.OnNext(new EngineStep<WorldState, WorldEvent>(
            state,
            state,
            new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id))));

        Entry playerEntry = Assert.NotNull(queue.PlayNext());

        Assert.That(playerEntry, Is.EqualTo(new Entry.Unfog(state, new WorldPosition(2, 0), player.Id)));
        Assert.That(playerEntry.GetFocusPosition(player.Id), Is.EqualTo(new WorldPosition(2, 0)));

        queue.Stop();

        Entry otherEntry = Assert.NotNull(queue.PlayNext());

        Assert.That(otherEntry, Is.EqualTo(new Entry.Unfog(state, new WorldPosition(2, 0), other.Id)));

        queue.Stop();

        Assert.That(queue.IsIdle, Is.True);
    }

    [Test]
    public void CreateAnimationTest()
    {
        WorldMap map = TestHelpers.SimpleMap(4, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("Leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder().AddUnit(leader).Build();

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(0, 0),
            TestHelpers.StackUnitGrid(definitions, [(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader), GridPosition.FrontBottom)]),
            movementPoints: 6);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        WorldState state = session.World.State;
        Entry.StackMove entry = new(
            state,
            stack.Id,
            [
                new Entry.StackMove.Step(new WorldPosition(0, 0), WorldStackDirection.SouthEast),
                new Entry.StackMove.Step(new WorldPosition(1, 0), WorldStackDirection.SouthEast),
                new Entry.StackMove.Step(new WorldPosition(2, 0), WorldStackDirection.SouthEast)
            ]);

        WorldAnimationState animation = entry.CreateAnimation(definitions, WorldVisibility.Create(state, player.Id, fogEnabled: true), GameSettings.Default);

        Assert.That(animation, Is.EqualTo(new WorldAnimationState.StackMove(
            stack.Id,
            [
                new WorldAnimationState.StackMoveStep(new WorldPosition(0, 0), WorldStackDirection.SouthEast, TimeSpan.Zero),
                new WorldAnimationState.StackMoveStep(new WorldPosition(1, 0), WorldStackDirection.SouthEast, TimeSpan.FromMilliseconds(200)),
                new WorldAnimationState.StackMoveStep(new WorldPosition(2, 0), WorldStackDirection.SouthEast, TimeSpan.FromMilliseconds(400))
            ])));
    }

    [Test]
    public void FastMovementCreateAnimationTest()
    {
        WorldMap map = TestHelpers.SimpleMap(4, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("Leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder().AddUnit(leader).Build();

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(0, 0),
            TestHelpers.StackUnitGrid(definitions, [(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader), GridPosition.FrontBottom)]),
            movementPoints: 6);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        WorldState state = session.World.State;
        Entry.StackMove entry = new(
            state,
            stack.Id,
            [
                new Entry.StackMove.Step(new WorldPosition(0, 0), WorldStackDirection.SouthEast),
                new Entry.StackMove.Step(new WorldPosition(1, 0), WorldStackDirection.SouthEast),
                new Entry.StackMove.Step(new WorldPosition(2, 0), WorldStackDirection.SouthEast)
            ]);

        WorldAnimationState animation = entry.CreateAnimation(definitions, WorldVisibility.Create(state, player.Id, fogEnabled: true), GameSettings.Default with { MovementSpeed = MovementSpeed.Fast });

        Assert.That(animation, Is.EqualTo(new WorldAnimationState.StackMove(
            stack.Id,
            [
                new WorldAnimationState.StackMoveStep(new WorldPosition(0, 0), WorldStackDirection.SouthEast, TimeSpan.Zero),
                new WorldAnimationState.StackMoveStep(new WorldPosition(1, 0), WorldStackDirection.SouthEast, TimeSpan.FromTicks(1538462)),
                new WorldAnimationState.StackMoveStep(new WorldPosition(2, 0), WorldStackDirection.SouthEast, TimeSpan.FromTicks(3076924))
            ])));
    }

    [Test]
    public void LargeLeaderCreateAnimationTest()
    {
        WorldMap map = TestHelpers.SimpleMap(4, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("Leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 6, leadership: 6) with
            {
                IsLarge = true
            };
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder().AddUnit(leader).Build();

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(0, 0),
            TestHelpers.StackUnitGrid(definitions, [(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader), GridPosition.FrontBottom)]),
            movementPoints: 6);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        WorldState state = session.World.State;
        Entry.StackMove entry = new(
            state,
            stack.Id,
            [
                new Entry.StackMove.Step(new WorldPosition(0, 0), WorldStackDirection.SouthEast),
                new Entry.StackMove.Step(new WorldPosition(1, 0), WorldStackDirection.SouthEast)
            ]);

        WorldAnimationState animation = entry.CreateAnimation(definitions, WorldVisibility.Create(state, player.Id, fogEnabled: true), GameSettings.Default);

        Assert.That(animation, Is.EqualTo(new WorldAnimationState.StackMove(
            stack.Id,
            [
                new WorldAnimationState.StackMoveStep(new WorldPosition(0, 0), WorldStackDirection.SouthEast, TimeSpan.Zero),
                new WorldAnimationState.StackMoveStep(new WorldPosition(1, 0), WorldStackDirection.SouthEast, TimeSpan.FromMilliseconds(400))
            ])));
    }

    [Test]
    public void EastWestCreateAnimationTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 2);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("Leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder().AddUnit(leader).Build();

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(0, 1),
            TestHelpers.StackUnitGrid(definitions, [(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader), GridPosition.FrontBottom)]),
            movementPoints: 6);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        WorldState state = session.World.State;
        Entry.StackMove entry = new(
            state,
            stack.Id,
            [
                new Entry.StackMove.Step(new WorldPosition(0, 1), WorldStackDirection.East),
                new Entry.StackMove.Step(new WorldPosition(1, 0), WorldStackDirection.SouthEast),
                new Entry.StackMove.Step(new WorldPosition(2, 0), WorldStackDirection.SouthEast)
            ]);

        WorldAnimationState animation = entry.CreateAnimation(definitions, WorldVisibility.Create(state, player.Id, fogEnabled: true), GameSettings.Default);

        Assert.That(animation, Is.EqualTo(new WorldAnimationState.StackMove(
            stack.Id,
            [
                new WorldAnimationState.StackMoveStep(new WorldPosition(0, 1), WorldStackDirection.East, TimeSpan.Zero),
                new WorldAnimationState.StackMoveStep(new WorldPosition(1, 0), WorldStackDirection.SouthEast, TimeSpan.FromMilliseconds(400)),
                new WorldAnimationState.StackMoveStep(new WorldPosition(2, 0), WorldStackDirection.SouthEast, TimeSpan.FromMilliseconds(600))
            ])));
    }

    [Test]
    public void FoggedCreateAnimationTest()
    {
        WorldMap map = TestHelpers.SimpleMap(4, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            FogOfWar = TestHelpers.BuildFogOfWar(["??.."])
        };
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("Leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder().AddUnit(leader).Build();

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(0, 0),
            TestHelpers.StackUnitGrid(definitions, [(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader), GridPosition.FrontBottom)]),
            movementPoints: 6);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        WorldState state = session.World.State;
        Entry.StackMove entry = new(
            state,
            stack.Id,
            [
                new Entry.StackMove.Step(new WorldPosition(0, 0), WorldStackDirection.SouthEast),
                new Entry.StackMove.Step(new WorldPosition(1, 0), WorldStackDirection.SouthEast),
                new Entry.StackMove.Step(new WorldPosition(2, 0), WorldStackDirection.SouthEast),
                new Entry.StackMove.Step(new WorldPosition(3, 0), WorldStackDirection.SouthEast)
            ]);

        WorldAnimationState animation = entry.CreateAnimation(definitions, WorldVisibility.Create(state, player.Id, fogEnabled: true), GameSettings.Default);

        Assert.That(animation, Is.EqualTo(new WorldAnimationState.StackMove(
            stack.Id,
            [
                new WorldAnimationState.StackMoveStep(new WorldPosition(0, 0), WorldStackDirection.SouthEast, TimeSpan.Zero),
                new WorldAnimationState.StackMoveStep(new WorldPosition(1, 0), WorldStackDirection.SouthEast, TimeSpan.Zero),
                new WorldAnimationState.StackMoveStep(new WorldPosition(2, 0), WorldStackDirection.SouthEast, TimeSpan.FromMilliseconds(200)),
                new WorldAnimationState.StackMoveStep(new WorldPosition(3, 0), WorldStackDirection.SouthEast, TimeSpan.FromMilliseconds(400))
            ])));
    }

    [Test]
    public void OtherEventsTest()
    {
        WorldState state = BuildStateWithStack(new WorldUnitId("leader"), new WorldStackId("s1"), new WorldPlayerId(1), new WorldPlayerId(2));
        Subject<EngineStep<WorldState, WorldEvent>> events = new();
        WorldEventQueue queue = new(GameScenarioProvider.Empty, events);

        events.OnNext(new EngineStep<WorldState, WorldEvent>(
            state,
            state,
            new WorldStackMovementConsumedEvent(new WorldStackId("s1"), 2)));

        Assert.That(queue.IsIdle, Is.True);
    }

    [Test]
    public void BattleStackExhaustTest()
    {
        WorldState state = BuildStateWithStack(new WorldUnitId("leader"), new WorldStackId("s1"), new WorldPlayerId(1), new WorldPlayerId(2));
        Subject<EngineStep<WorldState, WorldEvent>> events = new();
        WorldEventQueue queue = new(GameScenarioProvider.Empty, events);

        events.OnNext(new EngineStep<WorldState, WorldEvent>(
            state,
            state,
            new WorldStackMovementConsumedEvent(new WorldStackId("s1"), 0)));
        events.OnNext(BattleStarted(state));

        Entry? entry = queue.PlayNext();
        Assert.That(entry, Is.EqualTo(new Entry.Battle(
            state,
            new WorldStackId("s1"),
            new WorldPosition(0, 2))));

        queue.Stop();

        Assert.That(queue.IsIdle, Is.True);
    }

    [Test]
    public void GlobalSpellTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldSpellDefinitionId spellId = new("G000SS0087");

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCapital(new WorldCapital(
                Id: new WorldFortificationId("capital"),
                NameTextId: new ScenarioTextId("Name"),
                OwnerId: player.Id,
                Footprint: new WorldFootprint(new WorldPosition(0, 0), 5, 5),
                Garrison: UnitGrid<WorldUnit>.Empty,
                Inventory: WorldInventory.Empty,
                AiPriority: 1,
                CapturedById: null))
            .BuildSession();

        WorldState state = session.World.State;
        WorldSpellCastEvent cast = new(player.Id, spellId, Target: null, WorldResourceSet.Empty, WorldSpellCastSource.Player);
        Subject<EngineStep<WorldState, WorldEvent>> events = new();
        WorldEventQueue queue = new(GameScenarioProvider.Empty, events);

        events.OnNext(new EngineStep<WorldState, WorldEvent>(state, state, cast));

        Entry entry = Assert.NotNull(queue.PlayNext());
        Assert.That(entry, Is.EqualTo(new Entry.GlobalSpellCast(state, spellId, new WorldPosition(4, 4))));
        Assert.That(entry.GetFocusPosition(player.Id), Is.EqualTo(new WorldPosition(4, 4)));
    }

    [Test]
    public void GlobalSpellWithoutCapitalTest()
    {
        WorldPlayerId casterId = new(1);
        WorldState state = BuildState();
        WorldSpellCastEvent cast = new(casterId, new WorldSpellDefinitionId("G000SS0087"), Target: null, WorldResourceSet.Empty, WorldSpellCastSource.Player);
        Subject<EngineStep<WorldState, WorldEvent>> events = new();
        WorldEventQueue queue = new(GameScenarioProvider.Empty, events);

        events.OnNext(new EngineStep<WorldState, WorldEvent>(state, state, cast));

        Assert.That(queue.IsIdle, Is.True);
    }

    [Test]
    public void ItemsReceivedTest()
    {
        WorldStackId stackId = new("Stack1");
        WorldState state = BuildStateWithStack(new WorldUnitId("leader"), stackId, new WorldPlayerId(1), new WorldPlayerId(2));
        WorldItem item = new(new WorldItemId("Item0001"), new ItemDefinitionId("Item1"), Charges: null);
        Subject<EngineStep<WorldState, WorldEvent>> events = new();
        WorldEventQueue queue = new(GameScenarioProvider.Empty, events);

        events.OnNext(new EngineStep<WorldState, WorldEvent>(
            state,
            state,
            new WorldItemsReceivedEvent(new WorldPlayerId(1), new WorldUnitGroup.Stack(stackId), [item])));

        Entry entry = Assert.NotNull(queue.PlayNext());
        Assert.That(entry, Is.EqualTo(new Entry.ItemsReceive(state, new WorldPlayerId(1), [item])));
        Assert.That(entry.GetFocusPosition(new WorldPlayerId(1)), Is.Null);
    }

    [Test]
    public void SpellsReceivedTest()
    {
        WorldState state = BuildStateWithStack(new WorldUnitId("leader"), new WorldStackId("Stack1"), new WorldPlayerId(1), new WorldPlayerId(2));
        Subject<EngineStep<WorldState, WorldEvent>> events = new();
        WorldEventQueue queue = new(GameScenarioProvider.Empty, events);

        events.OnNext(new EngineStep<WorldState, WorldEvent>(
            state,
            state,
            new WorldSpellsReceivedEvent(new WorldPlayerId(1), [new WorldSpellDefinitionId("Spell1"), new WorldSpellDefinitionId("Spell2")])));

        Entry entry = Assert.NotNull(queue.PlayNext());
        Assert.That(entry, Is.EqualTo(new Entry.SpellsReceive(
            state,
            new WorldPlayerId(1),
            [new WorldSpellDefinitionId("Spell1"), new WorldSpellDefinitionId("Spell2")])));
        Assert.That(entry.GetFocusPosition(new WorldPlayerId(1)), Is.Null);
    }

    [Test]
    public void DestroyedStackItemsReceiveTest()
    {
        WorldStackId stackId = new("Stack1");
        WorldItem carried = new(new WorldItemId("Item0001"), new ItemDefinitionId("Item1"), Charges: null);
        WorldItem banner = new(new WorldItemId("Item0002"), new ItemDefinitionId("Item2"), Charges: null);
        WorldState state = BuildStateWithStack(new WorldUnitId("leader"), stackId, new WorldPlayerId(1), new WorldPlayerId(2));
        WorldStack stack = state.Stacks[stackId] with
        {
            Inventory = new WorldInventory([carried]),
            Equipment = LeaderEquipment.Empty.With(LeaderEquipmentSlot.Banner, banner)
        };
        WorldState oldState = state with { Stacks = state.Stacks.SetItem(stackId, stack) };
        WorldState newState = state with { Stacks = state.Stacks.Remove(stackId) };

        Subject<EngineStep<WorldState, WorldEvent>> events = new();
        WorldEventQueue queue = new(GameScenarioProvider.Empty, events);

        events.OnNext(new EngineStep<WorldState, WorldEvent>(
            oldState,
            newState,
            new WorldStackDestroyedEvent(stackId, new WorldUnitGroup.Stack(new WorldStackId("Stack2")), new WorldPlayerId(2))));

        Entry? destroyEntry = queue.PlayNext();
        Assert.That(destroyEntry, Is.EqualTo(new Entry.StackDestroy(newState, stack)));

        queue.Stop();

        Entry? receiveEntry = queue.PlayNext();
        Assert.That(receiveEntry, Is.EqualTo(new Entry.ItemsReceive(newState, new WorldPlayerId(2), [carried, banner])));

        queue.Stop();

        Assert.That(queue.IsIdle, Is.True);
    }
}
