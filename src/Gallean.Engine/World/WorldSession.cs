namespace Gallean.Engine;

public sealed class WorldSession
{
    private readonly WorldEngine engine;

    internal WorldSession(IEngineContext context, WorldState initialState)
    {
        engine = new WorldEngine(context, initialState);
    }

    public WorldState State => engine.State;
    public IObservable<EngineStep<WorldState, WorldEvent>> Events => engine.Events;

    public ICommandResult<WorldCommandError, WorldEvent, TResult> DispatchCommand<TResult>(
        IWorldCommand<TResult> command,
        bool dryRun = false)
    {
        return engine.DispatchCommand(command, dryRun);
    }
}
