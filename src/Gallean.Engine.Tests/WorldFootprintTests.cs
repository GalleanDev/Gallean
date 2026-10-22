namespace Gallean.Engine.Tests;

public sealed class WorldFootprintTests
{
    [Test]
    public void EnumerateTilesTest()
    {
        WorldFootprint footprint = new(new WorldPosition(2, 1), 3, 2);

        Assert.That(footprint.EnumerateTiles(), Is.EqualTo([
            new WorldPosition(2, 1),
            new WorldPosition(3, 1),
            new WorldPosition(4, 1),
            new WorldPosition(2, 2),
            new WorldPosition(3, 2),
            new WorldPosition(4, 2)
        ]));
    }

    [Test]
    public void ContainsTest()
    {
        WorldFootprint footprint = new(new WorldPosition(5, 24), 4, 4);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(footprint.Contains(new WorldPosition(5, 24)), Is.True);
            Assert.That(footprint.Contains(new WorldPosition(8, 27)), Is.True);
            Assert.That(footprint.Contains(new WorldPosition(4, 24)), Is.False);
            Assert.That(footprint.Contains(new WorldPosition(9, 27)), Is.False);
            Assert.That(footprint.Contains(new WorldPosition(8, 28)), Is.False);
        }
    }
}
