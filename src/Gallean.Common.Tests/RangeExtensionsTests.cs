namespace Gallean.Common.Tests;

public sealed class RangeExtensionsTests
{
    [Test]
    public void EnumerateTest()
    {
        List<int> values = [];

        foreach (int value in 2..5)
        {
            values.Add(value);
        }

        Assert.That(values, Is.EqualTo([2, 3, 4]));
    }

    [Test]
    public void EnumerateEmptyTest()
    {
        List<int> values = [];

        foreach (int value in 3..3)
        {
            values.Add(value);
        }

        Assert.That(values, Is.Empty);
    }

    [Test]
    public void EnumerateFromEndTest()
    {
        Range range = 2..^1;

        Assert.That(
            () =>
            {
                foreach (int value in range)
                {
                }
            },
            Throws.TypeOf<ArgumentOutOfRangeException>());
    }
}
