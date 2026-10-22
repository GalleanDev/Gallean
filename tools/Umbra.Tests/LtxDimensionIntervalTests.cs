namespace Umbra.Tests;

public sealed class LtxDimensionIntervalTests
{
    [Test]
    public void SplitBySizeSimpleTest()
    {
        Assert.That(LtxDimensionInterval.SplitBySize(8, 8, 2), Is.EqualTo([
            new LtxDimensionInterval(0, 8, 0, 0)
        ]));

        Assert.That(LtxDimensionInterval.SplitBySize(8, 16, 2), Is.EqualTo([
            new LtxDimensionInterval(0, 8, 0, 0)
        ]));

        Assert.That(LtxDimensionInterval.SplitBySize(8, 4, 2), Is.EqualTo([
            new LtxDimensionInterval(0, 4, 0, 2),
            new LtxDimensionInterval(2, 6, 2, 2),
            new LtxDimensionInterval(4, 8, 2, 0)
        ]));
    }

    [Test]
    public void SplitBySizeNoSpaceTest()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => LtxDimensionInterval.SplitBySize(8, 4, 4));
    }
}
