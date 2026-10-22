namespace Gallean.Engine.Tests;

public sealed class HitCalculatorTests
{
    [Test]
    public void ZeroAccuracyTest()
    {
        TestSeedRandomProvider random = new(42);
        bool hit = HitCalculator.RollHit(0, random);

        Assert.That(hit, Is.False);
    }

    [Test]
    public void HundredAccuracyTest()
    {
        TestSeedRandomProvider random = new(42);
        bool hit = HitCalculator.RollHit(100, random);

        Assert.That(hit, Is.True);
    }

    [Test]
    public void EightyAccuracyTest()
    {
        TestSeedRandomProvider random = new(42);
        bool hit = HitCalculator.RollHit(80, random);

        Assert.That(hit, Is.True);
    }

    [Test]
    public void OneAccuracyTest()
    {
        TestSeedRandomProvider random = new(42);
        bool hit = HitCalculator.RollHit(1, random);

        Assert.That(hit, Is.False);
    }

    [Test]
    public void TriangularHitDistributionTest()
    {
        int hits = 0;
        Random seedRandom = new(12345);

        for (int i = 0; i < 10000; i++)
        {
            if (HitCalculator.RollHit(80, new TestSeedRandomProvider(seedRandom.Next())))
            {
                hits++;
            }
        }

        Assert.That(hits, Is.EqualTo(9279));
    }

    [Test]
    public void FiftyAccuracyTest()
    {
        int hits = 0;
        Random seedRandom = new(54321);

        for (int i = 0; i < 10000; i++)
        {
            if (HitCalculator.RollHit(50, new TestSeedRandomProvider(seedRandom.Next())))
            {
                hits++;
            }
        }

        Assert.That(hits, Is.EqualTo(5228));
    }
}
