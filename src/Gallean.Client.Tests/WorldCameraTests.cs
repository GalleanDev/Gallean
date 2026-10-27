using Avalonia;

namespace Gallean.Client.Tests;

public sealed class WorldCameraTests
{
    [Test]
    public void PanByTest()
    {
        WorldCamera camera = new() { MapWidth = 4, MapHeight = 4 };
        camera.LookAt(new Point(0, 48));

        camera.PanBy(new Vector(8, 0));

        Assert.That(camera.Target, Is.EqualTo(new Point(8, 48)));
    }

    [Test]
    public void PanByBoundTest()
    {
        WorldCamera camera = new() { MapWidth = 4, MapHeight = 4 };
        camera.LookAt(new Point(0, 48));

        camera.PanBy(new Vector(1000, 0));

        Assert.That(camera.Target, Is.EqualTo(new Point(96, 48)));
    }

    [Test]
    public void PanToTest()
    {
        WorldCamera camera = new();
        camera.LookAt(new Point(0, 0));

        camera.PanTo(new Point(100, 0));
        camera.Animate(TimeSpan.FromSeconds(0.2));

        Assert.That(camera.Center, Is.EqualTo(new Point(98.77044761785129, 0)));
    }

    [Test]
    public void FocusOnTest()
    {
        WorldCamera camera = new();
        camera.LookAt(new Point(0, 0));

        camera.FocusOn(new Point(100, 0), TimeSpan.Zero);
        camera.Animate(TimeSpan.FromSeconds(0.2));

        Assert.That(camera.Center, Is.EqualTo(new Point(54.25510048511551, 0)));
    }

    [Test]
    public void FocusOnInterruptTest()
    {
        WorldCamera camera = new();
        camera.LookAt(new Point(0, 0));

        camera.FocusOn(new Point(100, 0), TimeSpan.Zero);
        camera.Animate(TimeSpan.FromSeconds(0.2));
        camera.FocusOn(new Point(54, 0), TimeSpan.Zero);
        camera.Animate(TimeSpan.FromSeconds(0.016));

        Assert.That(camera.Center, Is.EqualTo(new Point(57.96430971117457, 0)));
    }

    [Test]
    public void FocusOnEndTest()
    {
        WorldCamera camera = new();
        camera.LookAt(new Point(0, 0));

        camera.FocusOn(new Point(100, 0), TimeSpan.Zero);

        for (int i = 0; i < 1000; i++)
        {
            camera.Animate(TimeSpan.FromSeconds(0.016));
        }

        Assert.That(camera.Center, Is.EqualTo(new Point(100, 0)));
    }

    [Test]
    public void FocusOnDurationTest()
    {
        WorldCamera camera = new();
        camera.LookAt(new Point(0, 0));

        camera.FocusOn(new Point(100, 0), TimeSpan.FromSeconds(0.4));
        camera.Animate(TimeSpan.FromSeconds(0.1));

        Assert.That(camera.Center, Is.EqualTo(new Point(25, 0)));
    }

    [Test]
    public void FocusOnDurationInterruptTest()
    {
        WorldCamera camera = new();
        camera.LookAt(new Point(0, 0));

        camera.FocusOn(new Point(100, 0), TimeSpan.FromSeconds(0.4));
        camera.Animate(TimeSpan.FromSeconds(0.3));
        camera.FocusOn(new Point(75, 100), TimeSpan.FromSeconds(0.2));
        camera.Animate(TimeSpan.FromSeconds(0.1));

        Assert.That(camera.Center, Is.EqualTo(new Point(75, 50)));
    }

    [Test]
    public void FocusOnDurationEndTest()
    {
        WorldCamera camera = new();
        camera.LookAt(new Point(0, 0));

        camera.FocusOn(new Point(100, 0), TimeSpan.FromSeconds(0.4));
        camera.Animate(TimeSpan.FromSeconds(0.3));
        camera.Animate(TimeSpan.FromSeconds(0.3));

        Assert.That(camera.Center, Is.EqualTo(new Point(100, 0)));
    }

    [Test]
    public void LookAtBoundTest()
    {
        WorldCamera camera = new() { MapWidth = 4, MapHeight = 4 };

        camera.LookAt(new Point(0, 10000));

        Assert.That(camera.Center, Is.EqualTo(new Point(0, 96)));
        Assert.That(camera.Target, Is.EqualTo(new Point(0, 96)));
    }

    [Test]
    public void MoveBoundTest()
    {
        WorldCamera camera = new() { MapWidth = 4, MapHeight = 4 };
        camera.LookAt(new Point(0, 48));

        camera.Move(new Vector(-1000, 0));

        Assert.That(camera.Center, Is.EqualTo(new Point(-96, 48)));
        Assert.That(camera.Target, Is.EqualTo(new Point(-96, 48)));
    }

    [Test]
    public void AnimateBoundTest()
    {
        WorldCamera camera = new() { MapWidth = 4, MapHeight = 4 };
        camera.LookAt(new Point(0, 48));

        camera.PanBy(new Vector(0, 1000));

        for (int i = 0; i < 1000; i++)
        {
            camera.Animate(TimeSpan.FromSeconds(0.016));
        }

        Assert.That(camera.Center, Is.EqualTo(new Point(0, 96)));
    }

    [Test]
    public void ZoomAnimateTest()
    {
        WorldCamera camera = new() { MapWidth = 4, MapHeight = 4, ViewportSize = new Size(200, 100) };
        camera.LookAt(new Point(0, 48));

        camera.ZoomTo(1.25, new Point(100, 50));

        Assert.That(camera.Zoom, Is.EqualTo(1.0));
        Assert.That(camera.TargetZoom, Is.EqualTo(1.25));

        camera.Animate(TimeSpan.FromSeconds(0.016));

        Assert.That(camera.Zoom, Is.EqualTo(1.0684627407315772));

        for (int i = 0; i < 1000; i++)
        {
            camera.Animate(TimeSpan.FromSeconds(0.016));
        }

        Assert.That(camera.Zoom, Is.EqualTo(1.25));
    }

    [Test]
    public void ZoomAnchorTest()
    {
        WorldCamera camera = new() { ViewportSize = new Size(200, 100) };
        camera.LookAt(new Point(0, 0));

        Point anchor = new(150, 25);

        Assert.That(camera.ScreenToWorld(anchor), Is.EqualTo(new Point(50, -25)));

        camera.ZoomTo(1.25, anchor);

        for (int i = 0; i < 1000; i++)
        {
            camera.Animate(TimeSpan.FromSeconds(0.016));
        }

        Point zoomedAnchor = camera.ScreenToWorld(anchor);

        Assert.That(zoomedAnchor, Is.EqualTo(new Point(50.0, -25.000003814697237)));
    }

    [Test]
    public void UiScaleTest()
    {
        WorldCamera camera = new() { ViewportSize = new Size(200, 100), UiScale = 2.0 };
        camera.LookAt(new Point(0, 0));

        Assert.That(camera.ScreenToWorld(new Point(150, 25)), Is.EqualTo(new Point(100, -50)));
    }

    [Test]
    public void UiScaleZoomAnchorTest()
    {
        WorldCamera camera = new() { ViewportSize = new Size(200, 100), UiScale = 2.0 };
        camera.LookAt(new Point(0, 0));

        Point anchor = new(150, 25);
        camera.ZoomTo(1.25, anchor);

        for (int i = 0; i < 1000; i++)
        {
            camera.Animate(TimeSpan.FromSeconds(0.016));
        }

        Assert.That(camera.Zoom, Is.EqualTo(1.25));
        Assert.That(camera.ScreenToWorld(anchor), Is.EqualTo(new Point(100, -50.000007629394474)));
    }

    [Test]
    public void ZoomClampTest()
    {
        WorldCamera camera = new();

        camera.SetZoom(10.0);

        Assert.That(camera.Zoom, Is.EqualTo(1.75));

        camera.SetZoom(0.1);

        Assert.That(camera.Zoom, Is.EqualTo(0.75));
    }

    [Test]
    public void NoBoundsTest()
    {
        WorldCamera camera = new();

        camera.PanBy(new Vector(10000, 10000));

        Assert.That(camera.Target, Is.EqualTo(new Point(10000, 10000)));
    }
}
