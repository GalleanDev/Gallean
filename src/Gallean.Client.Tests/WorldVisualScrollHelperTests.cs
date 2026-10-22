using Avalonia;

namespace Gallean.Client.Tests;

public sealed class WorldVisualScrollHelperTests
{
    [Test]
    public void FirstPointerPositionTest()
    {
        WorldVisual.ScrollHelper scrollHelper = new();

        scrollHelper.HandlePointerMoved(new Point(400, 1190));

        Assert.That(scrollHelper.GetDirection(new Size(800, 1200)), Is.EqualTo(new Vector(0, 0)));
    }

    [Test]
    public void PointerMovedTest()
    {
        WorldVisual.ScrollHelper scrollHelper = new();

        scrollHelper.HandlePointerMoved(new Point(400, 1100));
        scrollHelper.HandlePointerMoved(new Point(400, 1190));

        Assert.That(scrollHelper.GetDirection(new Size(800, 1200)), Is.EqualTo(new Vector(0, 1)));
    }

    [Test]
    public void StationaryPointerTest()
    {
        WorldVisual.ScrollHelper scrollHelper = new();

        scrollHelper.HandlePointerMoved(new Point(400, 1190));
        scrollHelper.HandlePointerMoved(new Point(400, 1190));

        Assert.That(scrollHelper.GetDirection(new Size(800, 1200)), Is.EqualTo(new Vector(0, 0)));
    }

    [Test]
    public void CornerTest()
    {
        WorldVisual.ScrollHelper scrollHelper = new();

        scrollHelper.HandlePointerMoved(new Point(400, 600));
        scrollHelper.HandlePointerMoved(new Point(10, 10));

        Assert.That(scrollHelper.GetDirection(new Size(800, 1200)), Is.EqualTo(new Vector(-1, -1)));
    }

    [Test]
    public void OutsideViewportTest()
    {
        WorldVisual.ScrollHelper scrollHelper = new();

        scrollHelper.HandlePointerMoved(new Point(400, 600));
        scrollHelper.HandlePointerMoved(new Point(400, 1300));

        Assert.That(scrollHelper.GetDirection(new Size(800, 1200)), Is.EqualTo(new Vector(0, 0)));
    }

    [Test]
    public void InputReleasedTest()
    {
        WorldVisual.ScrollHelper scrollHelper = new();

        scrollHelper.HandlePointerMoved(new Point(400, 1100));
        scrollHelper.HandlePointerMoved(new Point(400, 1190));
        scrollHelper.HandleInputReleased();
        scrollHelper.HandlePointerMoved(new Point(400, 1190));

        Assert.That(scrollHelper.GetDirection(new Size(800, 1200)), Is.EqualTo(new Vector(0, 0)));
    }

    [Test]
    public void InputReleasedThenPointerMovedTest()
    {
        WorldVisual.ScrollHelper scrollHelper = new();

        scrollHelper.HandleInputReleased();
        scrollHelper.HandlePointerMoved(new Point(400, 1190));
        scrollHelper.HandlePointerMoved(new Point(410, 1190));

        Assert.That(scrollHelper.GetDirection(new Size(800, 1200)), Is.EqualTo(new Vector(0, 1)));
    }

    [Test]
    public void PointerExitedTest()
    {
        WorldVisual.ScrollHelper scrollHelper = new();

        scrollHelper.HandlePointerMoved(new Point(400, 1100));
        scrollHelper.HandlePointerMoved(new Point(400, 1190));
        scrollHelper.HandlePointerExited();

        Assert.That(scrollHelper.GetDirection(new Size(800, 1200)), Is.EqualTo(new Vector(0, 0)));
    }

    [Test]
    public void PointerExitedThenMovedTest()
    {
        WorldVisual.ScrollHelper scrollHelper = new();

        scrollHelper.HandlePointerMoved(new Point(400, 1100));
        scrollHelper.HandlePointerMoved(new Point(400, 1190));
        scrollHelper.HandlePointerExited();
        scrollHelper.HandlePointerMoved(new Point(400, 1190));

        Assert.That(scrollHelper.GetDirection(new Size(800, 1200)), Is.EqualTo(new Vector(0, 1)));
    }
}
