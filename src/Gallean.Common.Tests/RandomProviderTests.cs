namespace Gallean.Common.Tests;

public sealed class RandomProviderTests
{
    [Test]
    public void ShuffleTest()
    {
        RandomProvider random = new(0);
        int[] items = [1, 2, 3, 4, 5, 6, 7, 8];

        random.Shuffle(items);

        Assert.That(items, Is.EqualTo([6, 7, 2, 1, 5, 3, 8, 4]));
    }

    [Test]
    public void ShuffleSpanTest()
    {
        RandomProvider random = new(0);
        int[] items = [1, 2, 3, 4, 5, 6, 7, 8];

        random.Shuffle(items.AsSpan(0, 4));

        Assert.That(items, Is.EqualTo([3, 4, 2, 1, 5, 6, 7, 8]));
    }

    [Test]
    public void ShuffleSingleItemTest()
    {
        RandomProvider random = new(0);
        int[] items = [1];

        random.Shuffle(items);

        Assert.That(items, Is.EqualTo([1]));
    }

    [Test]
    public void ShuffleEmptyTest()
    {
        RandomProvider random = new(0);
        int[] items = [];

        random.Shuffle(items);

        Assert.That(items, Is.Empty);
    }
}
