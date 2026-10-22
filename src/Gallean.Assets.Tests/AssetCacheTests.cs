using Gallean.Engine.Contracts;
using Gallean.Tests.Common;

namespace Gallean.Assets.Tests;

public sealed class AssetCacheTests
{
    private sealed class TestContent
    {
        public required long Size { get; init; }
        public bool Disposed { get; set; }
    }

    private sealed class TestLoader(long contentSize)
    {
        private readonly Dictionary<AssetKey, int> loadCounts = [];

        public TestContent? Load(AssetKey key)
        {
            loadCounts[key] = loadCounts.GetValueOrDefault(key) + 1;

            if (key is AssetSpriteKey.Tree)
            {
                return null;
            }

            return new TestContent { Size = contentSize };
        }

        public int LoadCount(AssetKey key)
        {
            return loadCounts.GetValueOrDefault(key);
        }
    }

    private static long Measure(TestContent content)
    {
        return content.Size;
    }

    private static void Dispose(TestContent content)
    {
        content.Disposed = true;
    }

    private static AssetKey Key(int variant)
    {
        return new AssetSpriteKey.Bag(IsWater: false, variant);
    }

    private static void AcquireAndRelease(AssetCache<TestContent> cache, int variant)
    {
        using AssetCache<TestContent>.Lease? lease = cache.Acquire(Key(variant));
    }

    [Test]
    public void EvictTest()
    {
        TestLoader loader = new(contentSize: 100);
        using AssetCache<TestContent> cache = new(sizeLimit: 250, loader.Load, Measure, Dispose);

        AcquireAndRelease(cache, 1);
        AcquireAndRelease(cache, 2);
        AcquireAndRelease(cache, 3);

        Assert.That(cache.Count, Is.EqualTo(2));

        AcquireAndRelease(cache, 2);
        AcquireAndRelease(cache, 3);

        Assert.That(loader.LoadCount(Key(2)), Is.EqualTo(1));
        Assert.That(loader.LoadCount(Key(3)), Is.EqualTo(1));

        AcquireAndRelease(cache, 1);

        Assert.That(loader.LoadCount(Key(1)), Is.EqualTo(2));
    }

    [Test]
    public void ActiveLeaseTest()
    {
        TestLoader loader = new(contentSize: 100);
        using AssetCache<TestContent> cache = new(sizeLimit: 200, loader.Load, Measure, Dispose);

        AssetCache<TestContent>.Lease held = Assert.NotNull(cache.Acquire(Key(1)));

        AcquireAndRelease(cache, 2);
        AcquireAndRelease(cache, 3);

        Assert.That(cache.Count, Is.EqualTo(3));

        AcquireAndRelease(cache, 2);
        AcquireAndRelease(cache, 3);

        Assert.That(loader.LoadCount(Key(2)), Is.EqualTo(1));
        Assert.That(loader.LoadCount(Key(3)), Is.EqualTo(1));

        held.Dispose();
    }

    [Test]
    public void EvictWithLeaseTest()
    {
        TestLoader loader = new(contentSize: 100);
        using AssetCache<TestContent> cache = new(sizeLimit: 200, loader.Load, Measure, Dispose);

        AssetCache<TestContent>.Lease held = Assert.NotNull(cache.Acquire(Key(1)));

        AcquireAndRelease(cache, 2);
        AcquireAndRelease(cache, 3);

        held.Dispose();

        Assert.That(cache.Count, Is.EqualTo(2));

        AcquireAndRelease(cache, 1);

        Assert.That(loader.LoadCount(Key(1)), Is.EqualTo(2));
    }

    [Test]
    public void EvictOverLimitLeasesTest()
    {
        TestLoader loader = new(contentSize: 100);
        using AssetCache<TestContent> cache = new(sizeLimit: 100, loader.Load, Measure, Dispose);

        AssetCache<TestContent>.Lease first = Assert.NotNull(cache.Acquire(Key(1)));

        AssetCache<TestContent>.Lease second = Assert.NotNull(cache.Acquire(Key(2)));

        AcquireAndRelease(cache, 3);

        Assert.That(cache.Count, Is.EqualTo(3));

        first.Dispose();
        second.Dispose();

        Assert.That(cache.Count, Is.EqualTo(1));

        Assert.That(loader.LoadCount(Key(3)), Is.EqualTo(1));
    }

    [Test]
    public void NullLoadResultTest()
    {
        TestLoader loader = new(contentSize: 0);
        using AssetCache<TestContent> cache = new(sizeLimit: long.MaxValue, loader.Load, Measure, Dispose);

        AssetKey missing = new AssetSpriteKey.Tree(WorldMapTerrain.Human, 0);

        Assert.That(cache.Acquire(missing), Is.Null);
        Assert.That(cache.Acquire(missing), Is.Null);

        Assert.That(loader.LoadCount(missing), Is.EqualTo(1));
    }

    [Test]
    public void ReuseWithLeaseTest()
    {
        TestLoader loader = new(contentSize: 0);
        using AssetCache<TestContent> cache = new(sizeLimit: long.MaxValue, loader.Load, Measure, Dispose);

        AssetCache<TestContent>.Lease first = Assert.NotNull(cache.Acquire(Key(1)));

        AssetCache<TestContent>.Lease second = Assert.NotNull(cache.Acquire(Key(1)));

        Assert.That(loader.LoadCount(Key(1)), Is.EqualTo(1));
        Assert.That(ReferenceEquals(first.Value, second.Value), Is.True);

        first.Dispose();
        second.Dispose();
    }

    [Test]
    public async Task DisposeSlowLoadTest()
    {
        AssetKey key = Key(1);
        int startedCount = 0;
        List<TestContent> contents = [];
        Lock contentsLocker = new();
        using ManualResetEventSlim release = new();

        TestContent LoadSlow(AssetKey loadedKey)
        {
            Interlocked.Increment(ref startedCount);
            release.Wait();

            TestContent created = new() { Size = 0 };
            lock (contentsLocker)
            {
                contents.Add(created);
            }

            return created;
        }

        using AssetCache<TestContent> cache = new(sizeLimit: long.MaxValue, LoadSlow, Measure, Dispose);

        Task<AssetCache<TestContent>.Lease?> firstTask = Task.Run(() => cache.Acquire(key));
        Task<AssetCache<TestContent>.Lease?> secondTask = Task.Run(() => cache.Acquire(key));

        while (Volatile.Read(ref startedCount) < 2)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(1));
        }

        release.Set();

        AssetCache<TestContent>.Lease first = Assert.NotNull(await firstTask);

        AssetCache<TestContent>.Lease second = Assert.NotNull(await secondTask);

        List<bool> disposed = [];

        foreach (TestContent content in contents)
        {
            disposed.Add(content.Disposed);
        }

        Assert.That(ReferenceEquals(first.Value, second.Value), Is.True);
        Assert.That(disposed, Is.EquivalentTo([true, false]));

        first.Dispose();
        second.Dispose();
    }

    [Test]
    public void DisposeTwiceTest()
    {
        TestLoader loader = new(contentSize: 0);
        using AssetCache<TestContent> cache = new(sizeLimit: long.MaxValue, loader.Load, Measure, Dispose);

        AssetCache<TestContent>.Lease first = Assert.NotNull(cache.Acquire(Key(1)));

        AssetCache<TestContent>.Lease second = Assert.NotNull(cache.Acquire(Key(1)));

        first.Dispose();
        first.Dispose();

        AcquireAndRelease(cache, 2);

        Assert.That(second.Value.Disposed, Is.False);

        AcquireAndRelease(cache, 1);

        Assert.That(loader.LoadCount(Key(1)), Is.EqualTo(1));

        second.Dispose();
    }

    [Test]
    public void DisposeWithLeaseTest()
    {
        TestLoader loader = new(contentSize: 0);
        AssetCache<TestContent> cache = new(sizeLimit: long.MaxValue, loader.Load, Measure, Dispose);

        AssetCache<TestContent>.Lease lease = Assert.NotNull(cache.Acquire(Key(1)));

        TestContent content = lease.Value;

        cache.Dispose();

        Assert.That(content.Disposed, Is.False);
        Assert.That(cache.Acquire(Key(2)), Is.Null);

        lease.Dispose();

        Assert.That(content.Disposed, Is.True);
    }
}
