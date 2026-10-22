namespace Gallean.Engine.Tests;

public sealed class DrainOverflowCalculatorTests
{
    [Test]
    public void DistributeTest()
    {
        Assert.That(DrainOverflowCalculator.Distribute(40, [100, 100]), Is.EqualTo([20, 20]));
        Assert.That(DrainOverflowCalculator.Distribute(5, [9, 5]), Is.EqualTo([3, 2]));
        Assert.That(DrainOverflowCalculator.Distribute(5, [9, 9, 9]), Is.EqualTo([2, 2, 1]));
        Assert.That(DrainOverflowCalculator.Distribute(20, [1, 50]), Is.EqualTo([1, 19]));
        Assert.That(DrainOverflowCalculator.Distribute(20, [50, 1]), Is.EqualTo([19, 1]));
        Assert.That(DrainOverflowCalculator.Distribute(30, [100, 1, 100]), Is.EqualTo([15, 1, 14]));
        Assert.That(DrainOverflowCalculator.Distribute(7, [100, 100, 1]), Is.EqualTo([3, 3, 1]));
        Assert.That(DrainOverflowCalculator.Distribute(10, [2, 3]), Is.EqualTo([2, 3]));
        Assert.That(DrainOverflowCalculator.Distribute(6, [1, 2, 3]), Is.EqualTo([1, 2, 3]));
        Assert.That(DrainOverflowCalculator.Distribute(20, [50]), Is.EqualTo([20]));
    }

    [Test]
    public void DistributeEmptyTest()
    {
        Assert.That(DrainOverflowCalculator.Distribute(20, []), Is.Empty);
    }

    [Test]
    public void DistributeZeroTest()
    {
        Assert.That(DrainOverflowCalculator.Distribute(0, [10, 10]), Is.EqualTo([0, 0]));
    }
}
