namespace Gallean.Engine.Tests;

public sealed class ResourceSetCalculatorTests
{
    [Test]
    public void AddTest()
    {
        WorldResourceSet a = new(100, 50, 10, 0, 0, 0);
        WorldResourceSet b = new(50, 20, 5, 0, 0, 0);

        WorldResourceSet result = ResourceSetCalculator.Add(a, b);

        Assert.That(result, Is.EqualTo(new WorldResourceSet(150, 70, 15, 0, 0, 0)));
    }

    [Test]
    public void SubtractTest()
    {
        WorldResourceSet a = new(100, 50, 10, 0, 0, 0);
        WorldResourceSet b = new(50, 20, 5, 0, 0, 0);

        WorldResourceSet result = ResourceSetCalculator.Subtract(a, b);

        Assert.That(result, Is.EqualTo(new WorldResourceSet(50, 30, 5, 0, 0, 0)));
    }

    [Test]
    public void SubtractNegativeTest()
    {
        WorldResourceSet a = new(100, 0, 0, 0, 0, 0);
        WorldResourceSet b = new(150, 0, 0, 0, 0, 0);

        Assert.Throws<OverflowException>(() => ResourceSetCalculator.Subtract(a, b));
    }

    [Test]
    public void ScalePercentTest()
    {
        WorldResourceSet set = new(100, 50, 10, 0, 0, 0);

        WorldResourceSet result = ResourceSetCalculator.ScalePercent(set, 130);

        Assert.That(result, Is.EqualTo(new WorldResourceSet(130, 65, 13, 0, 0, 0)));
    }

    [Test]
    public void ScalePercentRoundingTest()
    {
        WorldResourceSet set = new(25, 7, 1, 0, 0, 0);

        WorldResourceSet result = ResourceSetCalculator.ScalePercent(set, 70);

        Assert.That(result, Is.EqualTo(new WorldResourceSet(17, 4, 0, 0, 0, 0)));
    }

    [Test]
    public void CanAffordTest()
    {
        WorldResourceSet a = new(100, 50, 10, 0, 0, 0);
        WorldResourceSet b = new(50, 50, 5, 0, 0, 0);

        bool result = ResourceSetCalculator.CanAfford(a, b);

        Assert.That(result, Is.True);
    }

    [Test]
    public void CannotAffordTest()
    {
        WorldResourceSet a = new(100, 50, 10, 0, 0, 0);
        WorldResourceSet b = new(50, 60, 5, 0, 0, 0);

        bool result = ResourceSetCalculator.CanAfford(a, b);

        Assert.That(result, Is.False);
    }
}
