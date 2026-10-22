namespace Gallean.Client.Tests;

public sealed class StableHashTests
{
    [Test]
    public void ZeroTest()
    {
        Assert.That(StableHash.Combine(0, 0), Is.EqualTo(2017529470));
    }

    [Test]
    public void ValueTest()
    {
        Assert.That(StableHash.Combine(1, 2), Is.EqualTo(1470949347));
        Assert.That(StableHash.Combine(54, 22), Is.EqualTo(1010155096));
    }

    [Test]
    public void OrderSensitiveTest()
    {
        Assert.That(StableHash.Combine(2, 1), Is.EqualTo(135493727));
    }

    [Test]
    public void NegativeInputTest()
    {
        Assert.That(StableHash.Combine(-1, -1), Is.EqualTo(1589292328));
    }

    [Test]
    public void NonNegativeTest()
    {
        Assert.That(StableHash.Combine(int.MinValue, int.MaxValue), Is.EqualTo(1550708354));
    }
}
