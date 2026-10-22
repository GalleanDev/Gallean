using System.Runtime.ExceptionServices;

namespace Gallean.Common;

public readonly record struct Result<T>
{
    private readonly ExceptionDispatchInfo? error;

    private Result(T value)
    {
        Value = value;
    }

    private Result(ExceptionDispatchInfo error)
    {
        Value = default!;
        this.error = error;
    }

    public bool IsSuccessful => error is null;

    public T Value
    {
        get
        {
            error?.Throw();
            return field;
        }
    }

    public static Result<T> FromValue(T value)
    {
        return new Result<T>(value);
    }

    public static Result<T> FromException(Exception error)
    {
        return new Result<T>(ExceptionDispatchInfo.Capture(error));
    }
}
