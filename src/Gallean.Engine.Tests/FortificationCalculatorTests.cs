namespace Gallean.Engine.Tests;

public sealed class FortificationCalculatorTests
{
    [Test]
    public void CreateFootprintTest()
    {
        Assert.That(
            FortificationCalculator.CreateFootprint(new WorldPosition(8, 27), 4),
            Is.EqualTo(new WorldFootprint(new WorldPosition(5, 24), 4, 4)));
    }

    [Test]
    public void GetEntranceTest()
    {
        Assert.That(
            FortificationCalculator.GetEntrance(new WorldFootprint(new WorldPosition(5, 24), 4, 4)),
            Is.EqualTo(new WorldPosition(8, 27)));
    }

    [Test]
    public void GetEntranceNonSquareFootprintTest()
    {
        Assert.That(
            FortificationCalculator.GetEntrance(new WorldFootprint(new WorldPosition(2, 1), 3, 2)),
            Is.EqualTo(new WorldPosition(4, 2)));
    }
}
