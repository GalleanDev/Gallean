namespace Gallean.Common;

public sealed class ActionDisposable(Action action) : IDisposable
{
    private Action? action = action;

    public void Dispose()
    {
        Action? pending = Interlocked.Exchange(ref action, null);

        pending?.Invoke();
    }
}
