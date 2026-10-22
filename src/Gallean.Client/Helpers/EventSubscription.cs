namespace Gallean.Client;

internal sealed class EventSubscription<TEvent> : IDisposable
{
    private readonly Action<TEvent> handler;
    private readonly Lock locker = new();
    private readonly IDisposable subscription;
    private readonly SynchronizationContext? synchronizationContext;
    private bool disposed;

    public EventSubscription(IObservable<TEvent> source, Action<TEvent> handler)
    {
        this.handler = handler;
        synchronizationContext = SynchronizationContext.Current;
        subscription = source.Subscribe(OnEvent);
    }

    public void Dispose()
    {
        lock (locker)
        {
            if (!disposed)
            {
                disposed = true;
                subscription.Dispose();
            }
        }
    }

    private void OnEvent(TEvent ev)
    {
        if (synchronizationContext is not null &&
            !ReferenceEquals(SynchronizationContext.Current, synchronizationContext))
        {
            synchronizationContext.Post(_ => Notify(ev), null);
        }
        else
        {
            Notify(ev);
        }
    }

    private void Notify(TEvent ev)
    {
        lock (locker)
        {
            if (!disposed)
            {
                handler(ev);
            }
        }
    }
}
