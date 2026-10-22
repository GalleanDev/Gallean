using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Gallean.Engine;

public interface ICommandResult<TError, TEvent, out TResult> where TError : struct
{
    TError? Error { get; }
    ImmutableList<TEvent> Events { get; }
    TResult? Data { get; }

    [MemberNotNullWhen(false, nameof(Data))]
    [MemberNotNullWhen(true, nameof(Error))]
    bool Failed => Error is not null;

    static ICommandResult<TError, TEvent, TResult> Success(TResult data, IEnumerable<TEvent> events)
    {
        return new CommandResult<TError, TEvent, TResult>(null, data, [.. events]);
    }

    static ICommandResult<TError, TEvent, TResult> Fail(TError error)
    {
        return new CommandResult<TError, TEvent, TResult>(error, default, []);
    }
}
