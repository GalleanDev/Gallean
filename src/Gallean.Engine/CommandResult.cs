using System.Collections.Immutable;

namespace Gallean.Engine;

internal sealed record CommandResult<TError, TEvent, TResult>(TError? Error, TResult? Data, ImmutableList<TEvent> Events)
    : ICommandResult<TError, TEvent, TResult> where TError : struct;
