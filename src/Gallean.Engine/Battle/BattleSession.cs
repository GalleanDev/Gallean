namespace Gallean.Engine;

public sealed class BattleSession
{
    private readonly BattleEngine engine;
    private readonly Action<BattleState> onResolved;
    private bool resolved;

    internal BattleSession(
        IEngineContext context,
        BattleState initialState,
        IBattleUnitUpgradeResolver upgradeResolver,
        Action<BattleState> onResolved,
        bool isAttackerAi,
        bool isDefenderAi,
        bool canAutoResolve,
        WorldPosition position)
    {
        this.onResolved = onResolved;

        IsAttackerAi = isAttackerAi;
        IsDefenderAi = isDefenderAi;
        CanAutoResolve = canAutoResolve;
        Position = position;

        engine = new BattleEngine(context, initialState, upgradeResolver);
        engine.DispatchCommand(new BattleStartCommand());
    }

    public BattleState State => engine.State;
    public IObservable<EngineStep<BattleState, BattleEvent>> Events => engine.Events;
    public bool IsAttackerAi { get; }
    public bool IsDefenderAi { get; }
    public bool CanAutoResolve { get; }
    public WorldPosition Position { get; }

    public bool IsAiTurn => State.Outcome is null && IsAiControlled(State.CurrentUnit.Side);

    public ICommandResult<BattleCommandError, BattleEvent, TResult> DispatchCommand<TResult>(
        IBattleCommand<TResult> command,
        bool dryRun = false)
    {
        ICommandResult<BattleCommandError, BattleEvent, TResult> result = engine.DispatchCommand(command, dryRun);

        if (!dryRun && !resolved && State.Outcome is not null)
        {
            resolved = true;
            onResolved(State);
        }

        return result;
    }

    private bool IsAiControlled(BattleSide side)
    {
        return side switch
        {
            BattleSide.Attacker => IsAttackerAi,
            BattleSide.Defender => IsDefenderAi
        };
    }
}
