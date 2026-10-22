namespace Umbra.Tests;

public sealed class SurfaceResolutionTests
{
    [Test]
    public void PaddingTest()
    {
        Assert.That(
            SurfaceResolution.Calculate(600, 950, 16 * 1024 * 1024),
            Is.EqualTo(new SurfaceResolution(608, 960, 600, 950)));
    }

    [Test]
    public void MinimumTest()
    {
        Assert.That(
            SurfaceResolution.Calculate(1, 1, 16 * 1024 * 1024),
            Is.EqualTo(new SurfaceResolution(16, 16, 1, 1)));
    }

    [Test]
    public void BudgetTest()
    {
        Assert.That(
            SurfaceResolution.Calculate(4320, 7680, 16 * 1024 * 1024 / 14),
            Is.EqualTo(new SurfaceResolution(832, 1472, 821, 1460)));
    }
}
