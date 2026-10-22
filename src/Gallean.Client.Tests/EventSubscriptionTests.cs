using System.Reactive.Subjects;

namespace Gallean.Client.Tests;

public sealed class EventSubscriptionTests
{
    private sealed record PostedCallback(SendOrPostCallback Callback, object? State);

    private sealed class TestSynchronizationContext : SynchronizationContext
    {
        private readonly Queue<PostedCallback> callbacks = [];

        public int PostCount { get; private set; }

        public override void Post(SendOrPostCallback d, object? state)
        {
            callbacks.Enqueue(new PostedCallback(d, state));
            PostCount++;
        }

        public void RunPostedCallbacks()
        {
            while (callbacks.TryDequeue(out PostedCallback? callback))
            {
                SynchronizationContext? previous = Current;
                SetSynchronizationContext(this);

                try
                {
                    callback.Callback(callback.State);
                }
                finally
                {
                    SetSynchronizationContext(previous);
                }
            }
        }
    }

    private static void WithSynchronizationContext(SynchronizationContext? context, Action action)
    {
        SynchronizationContext? previous = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(context);

        try
        {
            action();
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }
    }

    [Test]
    public void NoContextTest()
    {
        WithSynchronizationContext(null, static () =>
        {
            Subject<int> source = new();
            List<int> received = [];

            using EventSubscription<int> subscription = new(source, received.Add);

            source.OnNext(7);

            Assert.That(received, Is.EqualTo([7]));
        });
    }

    [Test]
    public void CapturedContextTest()
    {
        TestSynchronizationContext context = new();
        Subject<int> source = new();
        List<int> received = [];
        List<bool> handledOnCapturedContext = [];

        WithSynchronizationContext(context, () =>
        {
            using EventSubscription<int> subscription = new(source, value =>
            {
                received.Add(value);
                handledOnCapturedContext.Add(ReferenceEquals(SynchronizationContext.Current, context));
            });

            WithSynchronizationContext(null, () => source.OnNext(7));

            Assert.That(received, Is.EqualTo(Array.Empty<int>()));
            Assert.That(context.PostCount, Is.EqualTo(1));

            context.RunPostedCallbacks();

            Assert.That(received, Is.EqualTo([7]));
            Assert.That(handledOnCapturedContext, Is.EqualTo([true]));
        });
    }

    [Test]
    public void DisposeTest()
    {
        Subject<int> source = new();
        List<int> received = [];
        EventSubscription<int> subscription = new(source, received.Add);

        subscription.Dispose();
        source.OnNext(7);

        Assert.That(received, Is.EqualTo(Array.Empty<int>()));
    }

    [Test]
    public void DisposePostedCallbackTest()
    {
        TestSynchronizationContext context = new();
        Subject<int> source = new();
        List<int> received = [];

        WithSynchronizationContext(context, () =>
        {
            EventSubscription<int> subscription = new(source, received.Add);

            WithSynchronizationContext(null, () => source.OnNext(7));

            Assert.That(context.PostCount, Is.EqualTo(1));

            subscription.Dispose();
            context.RunPostedCallbacks();

            Assert.That(received, Is.EqualTo(Array.Empty<int>()));
        });
    }
}
