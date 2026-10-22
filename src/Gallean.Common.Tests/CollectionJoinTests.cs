namespace Gallean.Common.Tests;

public sealed class CollectionJoinTests
{
    private sealed record TestItem(int Key, string Value);

    [Test]
    public void SimpleTest()
    {
        IEnumerable<CollectionJoin.Item<TestItem>> join = CollectionJoin.Make(
            [new TestItem(0, "a"), new TestItem(1, "b")],
            [new TestItem(0, "x"), new TestItem(1, "y")],
            item => item.Key);

        Assert.That(join, Is.EqualTo(
        [
            new CollectionJoin.Item<TestItem>(new TestItem(0, "a"), new TestItem(0, "x")),
            new CollectionJoin.Item<TestItem>(new TestItem(1, "b"), new TestItem(1, "y"))
        ]));
    }

    [Test]
    public void LeftOnlyTest()
    {
        IEnumerable<CollectionJoin.Item<TestItem>> join = CollectionJoin.Make(
            [new TestItem(0, "a"), new TestItem(1, "b")],
            [],
            item => item.Key);

        Assert.That(join, Is.EqualTo(
        [
            new CollectionJoin.Item<TestItem>(new TestItem(0, "a"), null),
            new CollectionJoin.Item<TestItem>(new TestItem(1, "b"), null)
        ]));
    }

    [Test]
    public void RightOnlyTest()
    {
        IEnumerable<CollectionJoin.Item<TestItem>> join = CollectionJoin.Make(
            [],
            [new TestItem(0, "a"), new TestItem(1, "b")],
            item => item.Key);

        Assert.That(join, Is.EqualTo(
        [
            new CollectionJoin.Item<TestItem>(null, new TestItem(0, "a")),
            new CollectionJoin.Item<TestItem>(null, new TestItem(1, "b"))
        ]));
    }

    [Test]
    public void InterleavedTest()
    {
        IEnumerable<CollectionJoin.Item<TestItem>> join = CollectionJoin.Make(
            [new TestItem(0, "a"), new TestItem(2, "c"), new TestItem(4, "e")],
            [new TestItem(1, "b"), new TestItem(2, "x"), new TestItem(3, "d")],
            item => item.Key);

        Assert.That(join, Is.EqualTo(
        [
            new CollectionJoin.Item<TestItem>(new TestItem(0, "a"), null),
            new CollectionJoin.Item<TestItem>(null, new TestItem(1, "b")),
            new CollectionJoin.Item<TestItem>(new TestItem(2, "c"), new TestItem(2, "x")),
            new CollectionJoin.Item<TestItem>(null, new TestItem(3, "d")),
            new CollectionJoin.Item<TestItem>(new TestItem(4, "e"), null)
        ]));
    }

    [Test]
    public void TrailingLeftTest()
    {
        IEnumerable<CollectionJoin.Item<TestItem>> join = CollectionJoin.Make(
            [new TestItem(0, "a"), new TestItem(1, "b"), new TestItem(2, "c")],
            [new TestItem(0, "x")],
            item => item.Key);

        Assert.That(join, Is.EqualTo(
        [
            new CollectionJoin.Item<TestItem>(new TestItem(0, "a"), new TestItem(0, "x")),
            new CollectionJoin.Item<TestItem>(new TestItem(1, "b"), null),
            new CollectionJoin.Item<TestItem>(new TestItem(2, "c"), null)
        ]));
    }

    [Test]
    public void TrailingRightTest()
    {
        IEnumerable<CollectionJoin.Item<TestItem>> join = CollectionJoin.Make(
            [new TestItem(0, "a")],
            [new TestItem(0, "x"), new TestItem(1, "y"), new TestItem(2, "z")],
            item => item.Key);

        Assert.That(join, Is.EqualTo(
        [
            new CollectionJoin.Item<TestItem>(new TestItem(0, "a"), new TestItem(0, "x")),
            new CollectionJoin.Item<TestItem>(null, new TestItem(1, "y")),
            new CollectionJoin.Item<TestItem>(null, new TestItem(2, "z"))
        ]));
    }

    [Test]
    public void UnsortedTest()
    {
        IEnumerable<CollectionJoin.Item<TestItem>> join = CollectionJoin.Make(
            [new TestItem(2, "c"), new TestItem(0, "a"), new TestItem(1, "b")],
            [new TestItem(1, "x"), new TestItem(2, "y"), new TestItem(0, "z")],
            item => item.Key);

        Assert.That(join, Is.EqualTo(
        [
            new CollectionJoin.Item<TestItem>(new TestItem(0, "a"), new TestItem(0, "z")),
            new CollectionJoin.Item<TestItem>(new TestItem(1, "b"), new TestItem(1, "x")),
            new CollectionJoin.Item<TestItem>(new TestItem(2, "c"), new TestItem(2, "y"))
        ]));
    }

    [Test]
    public void DuplicatesTest()
    {
        IEnumerable<CollectionJoin.Item<TestItem>> join = CollectionJoin.Make(
            [new TestItem(0, "a"), new TestItem(0, "b"), new TestItem(0, "c")],
            [new TestItem(0, "x"), new TestItem(0, "y")],
            item => item.Key);

        Assert.That(join, Is.EqualTo(
        [
            new CollectionJoin.Item<TestItem>(new TestItem(0, "a"), new TestItem(0, "x")),
            new CollectionJoin.Item<TestItem>(new TestItem(0, "b"), new TestItem(0, "y")),
            new CollectionJoin.Item<TestItem>(new TestItem(0, "c"), null)
        ]));
    }

    [Test]
    public void EmptyTest()
    {
        IEnumerable<CollectionJoin.Item<TestItem>> join = CollectionJoin.Make<TestItem, int>([], [], item => item.Key);

        Assert.That(join, Is.Empty);
    }

    [Test]
    public void StringKeyTest()
    {
        IEnumerable<CollectionJoin.Item<TestItem>> join = CollectionJoin.Make(
            [new TestItem(0, "a"), new TestItem(1, "c")],
            [new TestItem(2, "b"), new TestItem(3, "c")],
            item => item.Value);

        Assert.That(join, Is.EqualTo(
        [
            new CollectionJoin.Item<TestItem>(new TestItem(0, "a"), null),
            new CollectionJoin.Item<TestItem>(null, new TestItem(2, "b")),
            new CollectionJoin.Item<TestItem>(new TestItem(1, "c"), new TestItem(3, "c"))
        ]));
    }
}
