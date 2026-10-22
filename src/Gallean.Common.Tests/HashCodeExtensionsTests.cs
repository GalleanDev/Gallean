namespace Gallean.Common.Tests;

public sealed class HashCodeExtensionsTests
{
    [Test]
    public void AddSequenceTest()
    {
        HashCode firstHash = new();
        firstHash.AddSequence([1, 2, 3]);

        HashCode secondHash = new();
        secondHash.AddSequence([1, 2, 3]);

        Assert.That(secondHash.ToHashCode(), Is.EqualTo(firstHash.ToHashCode()));
    }

    [Test]
    public void AddSequenceValueTest()
    {
        HashCode hash = new();
        hash.AddSequence([1, 2]);

        Assert.That(hash.ToHashCode(), Is.EqualTo(HashCode.Combine(1, 2)));
    }
}
