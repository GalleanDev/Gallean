using System.Runtime.ExceptionServices;

namespace Gallean.Common;

public sealed class CleanupList : IDisposable
{
    private readonly Queue<IDisposable> items = new();
    private bool disposed;

    public void Dispose()
    {
        disposed = true;

        ExceptionDispatchInfo? lastException = null;

        while (items.Count > 0)
        {
            IDisposable item = items.Dequeue();

            try
            {
                item.Dispose();
            }
            catch (Exception ex)
            {
                lastException = ExceptionDispatchInfo.Capture(ex);
            }
        }

        if (lastException is not null)
        {
            lastException.Throw();
        }
    }

    public T Add<T>(T item) where T : IDisposable
    {
        if (disposed)
        {
            item.Dispose();
        }
        else
        {
            items.Enqueue(item);
        }

        return item;
    }
}
