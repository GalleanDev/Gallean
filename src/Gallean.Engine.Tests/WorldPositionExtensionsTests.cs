namespace Gallean.Engine.Tests;

public sealed class WorldPositionExtensionsTests
{
    [Test]
    public void IsAdjacentToTest()
    {
        WorldPosition position = new(2, 2);

        Assert.That(position.IsAdjacentTo(new WorldPosition(1, 1)), Is.True);
        Assert.That(position.IsAdjacentTo(new WorldPosition(2, 1)), Is.True);
        Assert.That(position.IsAdjacentTo(new WorldPosition(3, 1)), Is.True);
        Assert.That(position.IsAdjacentTo(new WorldPosition(1, 2)), Is.True);
        Assert.That(position.IsAdjacentTo(new WorldPosition(3, 2)), Is.True);
        Assert.That(position.IsAdjacentTo(new WorldPosition(1, 3)), Is.True);
        Assert.That(position.IsAdjacentTo(new WorldPosition(2, 3)), Is.True);
        Assert.That(position.IsAdjacentTo(new WorldPosition(3, 3)), Is.True);

        Assert.That(position.IsAdjacentTo(position), Is.False);
        Assert.That(position.IsAdjacentTo(new WorldPosition(0, 2)), Is.False);
        Assert.That(position.IsAdjacentTo(new WorldPosition(2, 0)), Is.False);
        Assert.That(position.IsAdjacentTo(new WorldPosition(4, 4)), Is.False);
        Assert.That(position.IsAdjacentTo(new WorldPosition(2, 4)), Is.False);
    }

    [Test]
    public void DistanceTest()
    {
        WorldPosition position = new(2, 2);

        Assert.That(position.GetDistance(position), Is.EqualTo(0));
        Assert.That(position.GetDistance(new WorldPosition(1, 1)), Is.EqualTo(1));
        Assert.That(position.GetDistance(new WorldPosition(2, 1)), Is.EqualTo(1));
        Assert.That(position.GetDistance(new WorldPosition(3, 1)), Is.EqualTo(1));
        Assert.That(position.GetDistance(new WorldPosition(1, 2)), Is.EqualTo(1));
        Assert.That(position.GetDistance(new WorldPosition(3, 2)), Is.EqualTo(1));
        Assert.That(position.GetDistance(new WorldPosition(1, 3)), Is.EqualTo(1));
        Assert.That(position.GetDistance(new WorldPosition(2, 3)), Is.EqualTo(1));
        Assert.That(position.GetDistance(new WorldPosition(3, 3)), Is.EqualTo(1));
        Assert.That(position.GetDistance(new WorldPosition(0, 2)), Is.EqualTo(2));
        Assert.That(position.GetDistance(new WorldPosition(2, 0)), Is.EqualTo(2));
        Assert.That(position.GetDistance(new WorldPosition(4, 4)), Is.EqualTo(2));
        Assert.That(position.GetDistance(new WorldPosition(2, 4)), Is.EqualTo(2));
        Assert.That(position.GetDistance(new WorldPosition(7, 4)), Is.EqualTo(5));
    }

    [Test]
    public void SymmetryTest()
    {
        Assert.That(new WorldPosition(2, 2).IsAdjacentTo(new WorldPosition(1, 1)), Is.True);
        Assert.That(new WorldPosition(1, 1).IsAdjacentTo(new WorldPosition(2, 2)), Is.True);

        Assert.That(new WorldPosition(2, 2).IsAdjacentTo(new WorldPosition(4, 4)), Is.False);
        Assert.That(new WorldPosition(4, 4).IsAdjacentTo(new WorldPosition(2, 2)), Is.False);
    }

    [Test]
    public void AdjacentPositionsTest()
    {
        WorldPosition position = new(4, 6);

        Assert.That(position.GetAdjacentPositions(), Is.EqualTo([
            new WorldPosition(3, 5),
            new WorldPosition(4, 5),
            new WorldPosition(5, 5),
            new WorldPosition(3, 6),
            new WorldPosition(5, 6),
            new WorldPosition(3, 7),
            new WorldPosition(4, 7),
            new WorldPosition(5, 7)
        ]));
    }
}
