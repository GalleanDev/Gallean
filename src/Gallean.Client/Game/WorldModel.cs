using Gallean.Engine;

namespace Gallean.Client;

internal sealed class WorldModel(GameModel gameModel, WorldSession session, ILog log)
{
    public WorldState State => session.State;
    public IObservable<EngineStep<WorldState, WorldEvent>> Events => session.Events;

    public Task<ICommandResult<WorldCommandError, WorldEvent, TResult>> DispatchCommandAsync<TResult>(IWorldCommand<TResult> command)
    {
        return gameModel.RunAsync(() => ExecuteCommand(command, dryRun: false));
    }

    public ICommandResult<WorldCommandError, WorldEvent, TResult> DryRunCommand<TResult>(IWorldCommand<TResult> command)
    {
        return ExecuteCommand(command, dryRun: true);
    }

    private ICommandResult<WorldCommandError, WorldEvent, TResult> ExecuteCommand<TResult>(
        IWorldCommand<TResult> command,
        bool dryRun)
    {
        ICommandResult<WorldCommandError, WorldEvent, TResult> result = session.DispatchCommand(command, dryRun);
        log.LogWorldCommand(command, result, dryRun);

        return result;
    }
}
