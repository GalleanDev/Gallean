using Avalonia;

namespace Gallean.Client.Tests;

public sealed class WorldHitTesterTests
{
    [Test]
    public void PointInsideTargetTest()
    {
        WorldRenderContent.HitTarget target = new(new WorldPosition(3, 4), new Rect(100, 100, 40, 60));

        IReadOnlyList<WorldPosition> hits = WorldHitTester.HitTest([target], new Point(120, 130));

        Assert.That(hits, Is.EqualTo([new WorldPosition(3, 4)]));
    }

    [Test]
    public void OverlappingTargetsTest()
    {
        WorldRenderContent.HitTarget bottom = new(new WorldPosition(1, 1), new Rect(100, 100, 40, 60));
        WorldRenderContent.HitTarget top = new(new WorldPosition(2, 2), new Rect(110, 110, 40, 60));

        IReadOnlyList<WorldPosition> hits = WorldHitTester.HitTest([bottom, top], new Point(120, 130));

        Assert.That(hits, Is.EqualTo([new WorldPosition(2, 2), new WorldPosition(1, 1)]));
    }

    [Test]
    public void NoTargetTest()
    {
        IReadOnlyList<WorldPosition> hits = WorldHitTester.HitTest([], WorldTransform.ToWorldPoint(3, 2));

        Assert.That(hits, Is.Empty);
    }
}
