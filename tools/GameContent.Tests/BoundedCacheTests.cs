namespace GameContent.Tests;

public sealed class BoundedCacheTests
{
    [Test]
    public void BuiltOnceTest()
    {
        BoundedCache<string, int> cache = new(2);
        int builds = 0;

        int first = cache.GetOrAdd("a", _ =>
        {
            builds++;

            return 1;
        });

        int second = cache.GetOrAdd("a", _ =>
        {
            builds++;

            return 1;
        });

        Assert.That(first, Is.EqualTo(1));
        Assert.That(second, Is.EqualTo(1));
        Assert.That(builds, Is.EqualTo(1));
    }

    [Test]
    public void EvictionTest()
    {
        BoundedCache<string, int> cache = new(2);
        int builds = 0;

        cache.GetOrAdd("a", _ =>
        {
            builds++;

            return 1;
        });

        cache.GetOrAdd("b", _ =>
        {
            builds++;

            return 2;
        });

        cache.GetOrAdd("c", _ =>
        {
            builds++;

            return 3;
        });

        int reloaded = cache.GetOrAdd("a", _ =>
        {
            builds++;

            return 1;
        });

        Assert.That(reloaded, Is.EqualTo(1));
        Assert.That(builds, Is.EqualTo(4));
    }

    [Test]
    public void RecentUseTest()
    {
        BoundedCache<string, int> cache = new(2);
        int builds = 0;

        cache.GetOrAdd("a", _ =>
        {
            builds++;

            return 1;
        });

        cache.GetOrAdd("b", _ =>
        {
            builds++;

            return 2;
        });

        cache.GetOrAdd("a", _ =>
        {
            builds++;

            return 1;
        });

        cache.GetOrAdd("c", _ =>
        {
            builds++;

            return 3;
        });

        int survivor = cache.GetOrAdd("a", _ =>
        {
            builds++;

            return 1;
        });

        Assert.That(survivor, Is.EqualTo(1));
        Assert.That(builds, Is.EqualTo(3));
    }
}
