namespace Gallean.Common.Tests;

public sealed class ActionDisposableTests
{
    [Test]
    public void DisposeTest()
    {
        int callCount = 0;

        ActionDisposable disposable = new(() => callCount++);
        disposable.Dispose();

        Assert.That(callCount, Is.EqualTo(1));
    }

    [Test]
    public void DisposeTwiceTest()
    {
        int callCount = 0;

        ActionDisposable disposable = new(() => callCount++);
        disposable.Dispose();
        disposable.Dispose();

        Assert.That(callCount, Is.EqualTo(1));
    }

    [Test]
    public void DisposedCleanupListTest()
    {
        int callCount = 0;

        using (CleanupList cleanup = new())
        {
            cleanup.Add(new ActionDisposable(() => callCount++));
        }

        Assert.That(callCount, Is.EqualTo(1));
    }
}
