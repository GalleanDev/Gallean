using System.Reactive.Subjects;
using Gallean.Common;

namespace Gallean.Engine;

public sealed partial class GameSession
{
    private readonly Subject<BattleSession> battleStarted;
    private readonly WorldEventHandler worldEventHandler;

    public GameSession(IEngineContext engineContext, WorldState initialState)
    {
        worldEventHandler = new WorldEventHandler(this);
        battleStarted = new Subject<BattleSession>();

        WorldSession worldSession = new(engineContext, initialState);
        worldSession.Events.Subscribe(OnWorldEvent);

        EngineContext = engineContext;
        World = worldSession;
    }

    public IEngineContext EngineContext { get; }
    public WorldSession World { get; }
    public BattleSession? Battle { get; private set; }
    public IObservable<BattleSession> BattleStarted => battleStarted;

    public void ExecuteAiAction()
    {
        if (Battle is not null)
        {
            throw new InvalidOperationException("Cannot execute an AI action while a battle is active.");
        }

        WorldCommandResult worldResult = World.DispatchCommand(new WorldAiActionCommand());
        if (worldResult.Failed)
        {
            throw new WorldCommandException(worldResult.Error.Value);
        }

        if (Battle is not null && Battle.IsAttackerAi && Battle.IsDefenderAi)
        {
            BattleCommandResult battleResult = Battle.DispatchCommand(new BattleAutoResolveCommand());
            if (battleResult.Failed)
            {
                throw new BattleCommandException(battleResult.Error.Value);
            }
        }
    }

    internal void StartBattle(BattleSession battle)
    {
        Battle = battle;
        battleStarted.OnNext(battle);
    }

    private void OnWorldEvent(EngineStep<WorldState, WorldEvent> step)
    {
        step.Event.Accept(worldEventHandler, Empty.Value);
    }

    private void StartBattle(
        BattleState initialState,
        IBattleUnitUpgradeResolver upgradeResolver,
        bool isAttackerAi,
        bool isDefenderAi,
        bool canAutoResolve,
        WorldPosition position,
        Action<BattleState> resolveBattle)
    {
        BattleSession session = new(
            EngineContext,
            initialState,
            upgradeResolver,
            finalState =>
            {
                Battle = null;
                resolveBattle(finalState);
            },
            isAttackerAi,
            isDefenderAi,
            canAutoResolve,
            position);

        StartBattle(session);
    }
}
