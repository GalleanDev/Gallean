namespace Gallean.Common.Tests;

public sealed class CleanupListTests
{
    private sealed class TestDisposable : IDisposable
    {
        public Action? OnDispose { get; init; }
        public int DisposeCount { get; private set; }

        public void Dispose()
        {
            DisposeCount++;
            OnDispose?.Invoke();
        }
    }

    [Test]
    public void AddItemTest()
    {
        using CleanupList cleanup = new();
        TestDisposable item = new();

        TestDisposable returned = cleanup.Add(item);

        Assert.That(returned, Is.SameAs(item));
    }

    [Test]
    public void DisposeTest()
    {
        TestDisposable first = new();
        TestDisposable second = new();

        using (CleanupList cleanup = new())
        {
            cleanup.Add(first);
            cleanup.Add(second);
        }

        Assert.That(first.DisposeCount, Is.EqualTo(1));
        Assert.That(second.DisposeCount, Is.EqualTo(1));
    }

    [Test]
    public void DisposeExceptionTest()
    {
        TestDisposable first = new() { OnDispose = () => throw new InvalidOperationException("error") };
        TestDisposable second = new();

        CleanupList cleanup = new();
        cleanup.Add(first);
        cleanup.Add(second);

        Assert.That(cleanup.Dispose, Throws.InvalidOperationException);
        Assert.That(second.DisposeCount, Is.EqualTo(1));
    }

    [Test]
    public void AddAfterDisposeTest()
    {
        TestDisposable item = new();

        CleanupList cleanup = new();
        cleanup.Dispose();
        cleanup.Add(item);

        Assert.That(item.DisposeCount, Is.EqualTo(1));
    }
}
