using CommunityToolkit.Mvvm.ComponentModel;
using Gallean.Common;
using Gallean.Engine;

namespace Gallean.Client;

internal sealed partial class GameModel : ObservableObject, IDisposable
{
    private readonly IDisposable battleStartedSubscription;
    private readonly TaskScheduler scheduler;
    private volatile BattleModel? activeBattle;
    private volatile int activeActions;
    private WorldPlayerId viewPlayerId;

    public GameModel(GameSession session, IGameTextProvider texts, ILog log, TaskScheduler scheduler)
    {
        this.scheduler = scheduler;

        Session = session;
        Definitions = session.EngineContext.Definitions;
        Texts = texts;
        Log = log;
        Stats = session.EngineContext.UnitStats;
        Scenario = session.EngineContext.Scenario;
        Globals = session.EngineContext.Globals;
        World = new WorldModel(this, session.World, log);
        Snapshot = BuildSnapshot();

        battleStartedSubscription = session.BattleStarted.Subscribe(OnBattleStarted);
    }

    public IGameDefinitionProvider Definitions { get; }
    public IGameTextProvider Texts { get; }
    public IUnitStatsProvider Stats { get; }
    public IGameScenarioProvider Scenario { get; }
    public GlobalVariables Globals { get; }
    public ILog Log { get; }
    public WorldModel World { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBusy))]
    public partial GameSnapshot Snapshot { get; private set; }

    public string ScenarioId => Scenario.Definition.Id.Value;
    public string ScenarioName => Texts.GetScenarioText(Scenario.Definition.InfoTexts.Name);
    public BattleModel? ActiveBattle => activeBattle;
    public bool IsBusy => Snapshot.IsBusy;

    internal GameSession Session { get; }

    public void Dispose()
    {
        battleStartedSubscription.Dispose();
    }

    public Task ExecuteAiActionAsync()
    {
        return RunAsync(() =>
        {
            Session.ExecuteAiAction();

            if (Session.Battle is null)
            {
                activeBattle = null;
            }
        });
    }

    public void DismissBattle()
    {
        if (activeBattle is not null)
        {
            activeBattle = null;
            PublishSnapshot();
        }
    }

    internal async Task<TResult> RunAsync<TResult>(Func<TResult> dispatch)
    {
        using IDisposable action = BeginAction();

        return await Task.Factory.StartNew(dispatch, CancellationToken.None, TaskCreationOptions.DenyChildAttach, scheduler);
    }

    internal async Task RunAsync(Action dispatch)
    {
        using IDisposable action = BeginAction();

        await Task.Factory.StartNew(dispatch, CancellationToken.None, TaskCreationOptions.DenyChildAttach, scheduler);
    }

    private void OnBattleStarted(BattleSession battle)
    {
        activeBattle = new BattleModel(this, battle, Log);
    }

    private IDisposable BeginAction()
    {
        Interlocked.Increment(ref activeActions);
        PublishSnapshot();

        return new ActionDisposable(EndAction);
    }

    private void EndAction()
    {
        Interlocked.Decrement(ref activeActions);
        PublishSnapshot();
    }

    private void PublishSnapshot()
    {
        Snapshot = BuildSnapshot();
    }

    private GameSnapshot BuildSnapshot()
    {
        WorldState state = World.State;

        viewPlayerId = ResolveViewPlayerId(state, viewPlayerId);

        return new GameSnapshot(state, activeBattle?.State, activeActions, viewPlayerId);
    }

    private static WorldPlayerId ResolveViewPlayerId(WorldState state, WorldPlayerId current)
    {
        WorldPlayerId turnPlayerId = state.TurnInfo.CurrentPlayerId;

        if (!state.Players[turnPlayerId].IsAi)
        {
            return turnPlayerId;
        }
        else if (state.Players.TryGetValue(current, out WorldPlayer? player) && !player.IsAi)
        {
            return current;
        }
        else
        {
            foreach (WorldPlayer candidate in state.Players.Values)
            {
                if (!candidate.IsAi)
                {
                    return candidate.Id;
                }
            }

            throw new InvalidOperationException("The scenario has no human player.");
        }
    }
}
