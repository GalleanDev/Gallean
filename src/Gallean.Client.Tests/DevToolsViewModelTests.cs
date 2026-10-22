using Gallean.Common;
using Gallean.Engine;
using Gallean.Engine.Tests;
using Gallean.Tests.Common;

namespace Gallean.Client.Tests;

public sealed class DevToolsViewModelTests
{
    [Test]
    public void EventLogAvailableTest()
    {
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        DevToolsViewModel devTools = gameViewModel.DevTools;

        Assert.That(devTools.OpenEventLogCommand.CanExecute(null), Is.False);

        devTools.StartEventLog(gameModel);

        Assert.That(devTools.OpenEventLogCommand.CanExecute(null), Is.True);

        devTools.StopEventLog();

        Assert.That(devTools.OpenEventLogCommand.CanExecute(null), Is.False);
    }

    [Test]
    public async Task WorldEventsTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 1);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("Leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leader)
            .Build();

        WorldStack stack1 = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player1.Id,
            new WorldPosition(0, 0),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [(new WorldUnit(new WorldUnitId("leader"), leader.Id, 100, 0, 1, [], null, false), GridPosition.FrontBottom)]),
            movementPoints: 6);

        WorldStack stack2 = TestHelpers.SimpleStack(
            new WorldStackId("Stack2"),
            player2.Id,
            new WorldPosition(5, 0),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [(new WorldUnit(new WorldUnitId("leader2"), leader.Id, 100, 0, 1, [], null, false), GridPosition.FrontBottom)]),
            movementPoints: 6);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(stack1)
            .WithStack(stack2)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        gameViewModel.DevTools.StartEventLog(gameModel);

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());
        await gameModel.World.DispatchCommandAsync(new WorldMoveStackCommand(stack1.Id, new WorldPosition(1, 0)));
        await gameModel.World.DispatchCommandAsync(new WorldEndTurnCommand());
        await gameModel.World.DispatchCommandAsync(new WorldMoveStackCommand(stack2.Id, new WorldPosition(4, 0)));

        gameViewModel.DevTools.OpenEventLogCommand.Execute(null);

        EventLogPopupViewModel popup = Assert.IsOfType<EventLogPopupViewModel>(gameViewModel.TopScreen);

        Assert.That(popup.Entries, Is.EqualTo(
        [
            new EventLogEntry(
                EventLogEntryType.World,
                [
                    new EventLogSpan("Stack ", EventLogSpanType.Text),
                    new EventLogSpan("Stack1", EventLogSpanType.Reference),
                    new EventLogSpan(" movement restored to ", EventLogSpanType.Text),
                    new EventLogSpan("6", EventLogSpanType.Value),
                    new EventLogSpan(".", EventLogSpanType.Text)
                ],
                null),
            new EventLogEntry(
                EventLogEntryType.World,
                [
                    new EventLogSpan("Player 1", EventLogSpanType.Actor),
                    new EventLogSpan(" turn started.", EventLogSpanType.Text)
                ],
                null),
            new EventLogEntry(
                EventLogEntryType.World,
                [
                    new EventLogSpan("Player 1", EventLogSpanType.Actor),
                    new EventLogSpan(" received ", EventLogSpanType.Text),
                    new EventLogSpan("100 gold, 25 death mana", EventLogSpanType.Value),
                    new EventLogSpan(".", EventLogSpanType.Text)
                ],
                null),
            new EventLogEntry(
                EventLogEntryType.World,
                [
                    new EventLogSpan("Stack ", EventLogSpanType.Text),
                    new EventLogSpan("Stack1", EventLogSpanType.Reference),
                    new EventLogSpan(" moved to ", EventLogSpanType.Text),
                    new EventLogSpan("1,0", EventLogSpanType.Reference),
                    new EventLogSpan(".", EventLogSpanType.Text)
                ],
                null),
            new EventLogEntry(
                EventLogEntryType.World,
                [
                    new EventLogSpan("Player 1", EventLogSpanType.Actor),
                    new EventLogSpan(" ended the turn.", EventLogSpanType.Text)
                ],
                null),
            new EventLogEntry(
                EventLogEntryType.World,
                [
                    new EventLogSpan("Stack ", EventLogSpanType.Text),
                    new EventLogSpan("Stack2", EventLogSpanType.Reference),
                    new EventLogSpan(" movement restored to ", EventLogSpanType.Text),
                    new EventLogSpan("6", EventLogSpanType.Value),
                    new EventLogSpan(".", EventLogSpanType.Text)
                ],
                null),
            new EventLogEntry(
                EventLogEntryType.World,
                [
                    new EventLogSpan("Player 2", EventLogSpanType.Actor),
                    new EventLogSpan(" turn started.", EventLogSpanType.Text)
                ],
                null),
            new EventLogEntry(
                EventLogEntryType.World,
                [
                    new EventLogSpan("Player 2", EventLogSpanType.Actor),
                    new EventLogSpan(" received ", EventLogSpanType.Text),
                    new EventLogSpan("100 gold, 25 infernal mana", EventLogSpanType.Value),
                    new EventLogSpan(".", EventLogSpanType.Text)
                ],
                null),
            new EventLogEntry(
                EventLogEntryType.World,
                [
                    new EventLogSpan("Stack ", EventLogSpanType.Text),
                    new EventLogSpan("Stack2", EventLogSpanType.Reference),
                    new EventLogSpan(" moved to ", EventLogSpanType.Text),
                    new EventLogSpan("4,0", EventLogSpanType.Reference),
                    new EventLogSpan(".", EventLogSpanType.Text)
                ],
                null)
        ]));
    }

    [Test]
    public async Task BattleEventsTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 100, initiative: 50, damage: 50);
        UnitDefinition enemyDefinition = TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 30, initiative: 30, damage: 5);

        GlobalVariables globals = new() { BattleInitiativeVariance = 0, BattleDamageVariance = 0 };
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithGlobals(globals)
            .WithDefinition(attackerDefinition)
            .WithDefinition(enemyDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        gameViewModel.DevTools.StartEventLog(gameModel);

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());

        BattleUnitId attackerId = new(1);
        BattleUnitId enemyId = new(2);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(attackerId, null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit enemy = TestHelpers.SimpleBattleUnit(enemyId, null, enemyDefinition.Id, BattleSide.Defender, hitpoints: 30);

        BattleState battleState = new BattleStateBuilder(engine.Definitions)
            .Add(attacker)
            .Add(enemy)
            .Build(BattleModifiers.None);

        EngineContext battleEngine = new(new RandomProvider(0), engine.Definitions, engine.UnitStats, GameScenarioProvider.Empty, globals);
        await gameModel.RunAsync(() =>
        {
            gameModel.Session.StartBattle(new BattleSession(
                battleEngine,
                battleState,
                NullBattleUnitUpgradeResolver.Instance,
                _ =>
                {
                },
                isAttackerAi: false,
                isDefenderAi: false,
                canAutoResolve: true,
                new WorldPosition(0,
                    0)));
        });
        BattleModel battle = Assert.NotNull(gameModel.ActiveBattle);
        gameViewModel.DevTools.LogBattle(battle);

        await battle.DispatchCommandAsync(new BattleAutoResolveCommand());

        gameViewModel.DevTools.OpenEventLogCommand.Execute(null);

        EventLogPopupViewModel popup = Assert.IsOfType<EventLogPopupViewModel>(gameViewModel.TopScreen);

        Assert.That(popup.Entries, Is.EqualTo(
        [
            new EventLogEntry(
                EventLogEntryType.World,
                [
                    new EventLogSpan("Player 1", EventLogSpanType.Actor),
                    new EventLogSpan(" turn started.", EventLogSpanType.Text)
                ],
                null),
            new EventLogEntry(
                EventLogEntryType.World,
                [
                    new EventLogSpan("Player 1", EventLogSpanType.Actor),
                    new EventLogSpan(" received ", EventLogSpanType.Text),
                    new EventLogSpan("100 gold, 25 life mana", EventLogSpanType.Value),
                    new EventLogSpan(".", EventLogSpanType.Text)
                ],
                null),
            new EventLogEntry(
                EventLogEntryType.Battle,
                [
                    new EventLogSpan("name", EventLogSpanType.Actor),
                    new EventLogSpan(" attacked.", EventLogSpanType.Text)
                ],
                null),
            new EventLogEntry(
                EventLogEntryType.Battle,
                [
                    new EventLogSpan("name", EventLogSpanType.Actor),
                    new EventLogSpan(" hit ", EventLogSpanType.Text),
                    new EventLogSpan("name", EventLogSpanType.Actor),
                    new EventLogSpan(" for ", EventLogSpanType.Text),
                    new EventLogSpan("50", EventLogSpanType.Damage),
                    new EventLogSpan(" damage.", EventLogSpanType.Text)
                ],
                null),
            new EventLogEntry(
                EventLogEntryType.Battle,
                [
                    new EventLogSpan("name", EventLogSpanType.Actor),
                    new EventLogSpan(" died.", EventLogSpanType.Text)
                ],
                null),
            new EventLogEntry(
                EventLogEntryType.Battle,
                [
                    new EventLogSpan("Battle ended. ", EventLogSpanType.Text),
                    new EventLogSpan("Attacker", EventLogSpanType.Actor),
                    new EventLogSpan(" won.", EventLogSpanType.Text)
                ],
                null),
            new EventLogEntry(
                EventLogEntryType.Battle,
                [
                    new EventLogSpan("name", EventLogSpanType.Actor),
                    new EventLogSpan(" has gained ", EventLogSpanType.Text),
                    new EventLogSpan("1", EventLogSpanType.Value),
                    new EventLogSpan(" XP this battle.", EventLogSpanType.Text)
                ],
                null)
        ]));

        popup.CloseCommand.Execute(null);

        Assert.That(gameViewModel.TopScreen, Is.Null);
    }
}
