namespace Gallean.Engine;

public interface ICommand<in TState, TError, TEvent, out TResult> where TError : struct
{
    ICommandResult<TError, TEvent, TResult> Execute(TState state, IEngineContext context);
}
