using Avalonia;

namespace Gallean.Client.Tests;

public sealed class WorldTransformTests
{
    [Test]
    public void ClampToMapInsideTest()
    {
        Assert.That(WorldTransform.ClampToMap(new Point(8, 56), 4, 4), Is.EqualTo(new Point(8, 56)));
    }

    [Test]
    public void ClampToMapCornerTest()
    {
        Assert.That(WorldTransform.ClampToMap(new Point(1000, 0), 4, 4), Is.EqualTo(new Point(96, 48)));
    }

    [Test]
    public void ClampToMapEdgeTest()
    {
        Assert.That(WorldTransform.ClampToMap(new Point(112, 104), 4, 4), Is.EqualTo(new Point(48, 72)));
    }

    [Test]
    public void ClampToMapEmptyTest()
    {
        Assert.That(WorldTransform.ClampToMap(new Point(10000, 10000), 0, 5), Is.EqualTo(new Point(10000, 10000)));
        Assert.That(WorldTransform.ClampToMap(new Point(10000, 10000), 5, 0), Is.EqualTo(new Point(10000, 10000)));
    }
}
