namespace Gallean.Engine;

public interface IWorldCommand<TResult> : ICommand<WorldState, WorldCommandError, WorldEvent, TResult>;
