using Gallean.Engine;

namespace Gallean.Client;

internal sealed class BattleModel(GameModel gameModel, BattleSession session, ILog log)
{
    public BattleState State => session.State;
    public IObservable<EngineStep<BattleState, BattleEvent>> Events => session.Events;
    public bool IsAiTurn => session.IsAiTurn;
    public bool IsAttackerAi => session.IsAttackerAi;
    public bool IsDefenderAi => session.IsDefenderAi;
    public bool CanAutoResolve => session.CanAutoResolve;
    public WorldPosition Position => session.Position;
    public BattleHistory History { get; } = new(session.Events);

    public BattleViewerSide ViewerSide
    {
        get
        {
            if (!session.IsAttackerAi)
            {
                return BattleViewerSide.Attacker;
            }
            else if (!session.IsDefenderAi)
            {
                return BattleViewerSide.Defender;
            }
            else
            {
                return BattleViewerSide.None;
            }
        }
    }

    public Task<ICommandResult<BattleCommandError, BattleEvent, TResult>> DispatchCommandAsync<TResult>(IBattleCommand<TResult> command)
    {
        return gameModel.RunAsync(() => ExecuteCommand(command, dryRun: false));
    }

    public ICommandResult<BattleCommandError, BattleEvent, TResult> DryRunCommand<TResult>(IBattleCommand<TResult> command)
    {
        return ExecuteCommand(command, dryRun: true);
    }

    private ICommandResult<BattleCommandError, BattleEvent, TResult> ExecuteCommand<TResult>(
        IBattleCommand<TResult> command,
        bool dryRun)
    {
        ICommandResult<BattleCommandError, BattleEvent, TResult> result = session.DispatchCommand(command, dryRun);
        log.LogBattleCommand(command, result, dryRun);

        return result;
    }
}
