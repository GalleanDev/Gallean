namespace Gallean.Engine.Tests;

public sealed class GridPositionTests
{
    [Test]
    public void FromIndexTest()
    {
        Assert.That(GridPosition.FromIndex(0), Is.EqualTo(new GridPosition(0, 0)));
        Assert.That(GridPosition.FromIndex(1), Is.EqualTo(new GridPosition(0, 1)));
        Assert.That(GridPosition.FromIndex(2), Is.EqualTo(new GridPosition(0, 2)));
        Assert.That(GridPosition.FromIndex(3), Is.EqualTo(new GridPosition(1, 0)));
        Assert.That(GridPosition.FromIndex(4), Is.EqualTo(new GridPosition(1, 1)));
        Assert.That(GridPosition.FromIndex(5), Is.EqualTo(new GridPosition(1, 2)));
    }

    [Test]
    public void PropertiesTest()
    {
        GridPosition position = new(1, 2);

        Assert.That(position.Line, Is.EqualTo(1));
        Assert.That(position.Flank, Is.EqualTo(2));
    }

    [Test]
    public void IsBackLineTest()
    {
        Assert.That(new GridPosition(0, 0).IsBackLine, Is.False);
        Assert.That(new GridPosition(0, 1).IsBackLine, Is.False);
        Assert.That(new GridPosition(0, 2).IsBackLine, Is.False);

        Assert.That(new GridPosition(1, 0).IsBackLine, Is.True);
        Assert.That(new GridPosition(1, 1).IsBackLine, Is.True);
        Assert.That(new GridPosition(1, 2).IsBackLine, Is.True);
    }

    [Test]
    public void EqualityTest()
    {
        GridPosition position1 = new(1, 2);
        GridPosition position2 = new(1, 2);
        GridPosition position3 = new(0, 2);

        Assert.That(position1, Is.EqualTo(position2));
        Assert.That(position1, Is.Not.EqualTo(position3));

        Assert.That(position1.GetHashCode(), Is.EqualTo(position2.GetHashCode()));
        Assert.That(position1.GetHashCode(), Is.Not.EqualTo(position3.GetHashCode()));
    }
}
