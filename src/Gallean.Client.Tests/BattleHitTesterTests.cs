using Avalonia;

namespace Gallean.Client.Tests;

public sealed class BattleHitTesterTests
{
    [Test]
    public void PointInsideTargetTest()
    {
        Rect bounds = new(100, 100, 60, 120);
        BattleRenderState.HitTarget[] targets = [new(new BattleGridTile(BattleSide.Attacker, GridPosition.FrontBottom), bounds)];

        Assert.That(BattleHitTester.HitTest(targets, bounds.Center), Is.EqualTo(new BattleGridTile(BattleSide.Attacker, GridPosition.FrontBottom)));
        Assert.That(BattleHitTester.HitTest(targets, new Point(0, 0)), Is.Null);
    }

    [Test]
    public void TopmostTargetTest()
    {
        Rect bounds = new(100, 100, 60, 120);

        BattleRenderState.HitTarget[] targets =
        [
            new(new BattleGridTile(BattleSide.Attacker, GridPosition.FrontBottom), bounds),
            new(new BattleGridTile(BattleSide.Defender, GridPosition.FrontTop), bounds)
        ];

        Assert.That(BattleHitTester.HitTest(targets, bounds.Center), Is.EqualTo(new BattleGridTile(BattleSide.Defender, GridPosition.FrontTop)));
    }

    [Test]
    public void NoTargetTest()
    {
        Assert.That(BattleHitTester.HitTest([], new Point(100, 100)), Is.Null);
    }

    [Test]
    public void EnclosedTargetTest()
    {
        Rect outer = new(100, 100, 200, 400);
        Rect inner = new(140, 200, 60, 120);

        BattleRenderState.HitTarget[] targets =
        [
            new(new BattleGridTile(BattleSide.Attacker, GridPosition.FrontBottom), outer),
            new(new BattleGridTile(BattleSide.Defender, GridPosition.FrontTop), inner)
        ];

        Assert.That(BattleHitTester.HitTest(targets, inner.Center), Is.EqualTo(new BattleGridTile(BattleSide.Defender, GridPosition.FrontTop)));
    }

    [Test]
    public void LaterDrawnEncloserTest()
    {
        Rect inner = new(140, 200, 60, 120);
        Rect outer = new(100, 100, 200, 400);

        BattleRenderState.HitTarget[] targets =
        [
            new(new BattleGridTile(BattleSide.Attacker, GridPosition.FrontBottom), inner),
            new(new BattleGridTile(BattleSide.Defender, GridPosition.FrontTop), outer)
        ];

        Assert.That(BattleHitTester.HitTest(targets, inner.Center), Is.EqualTo(new BattleGridTile(BattleSide.Attacker, GridPosition.FrontBottom)));
    }

    [Test]
    public void OuterOnlyRegionTest()
    {
        Rect outer = new(100, 100, 200, 400);
        Rect inner = new(140, 200, 60, 120);

        BattleRenderState.HitTarget[] targets =
        [
            new(new BattleGridTile(BattleSide.Attacker, GridPosition.FrontBottom), outer),
            new(new BattleGridTile(BattleSide.Defender, GridPosition.FrontTop), inner)
        ];

        Assert.That(BattleHitTester.HitTest(targets, new Point(110, 110)), Is.EqualTo(new BattleGridTile(BattleSide.Attacker, GridPosition.FrontBottom)));
    }

    [Test]
    public void OverlappingSmallerTest()
    {
        Rect large = new(100, 100, 200, 400);
        Rect small = new(260, 300, 80, 120);

        BattleRenderState.HitTarget[] targets =
        [
            new(new BattleGridTile(BattleSide.Attacker, GridPosition.FrontBottom), small),
            new(new BattleGridTile(BattleSide.Defender, GridPosition.FrontTop), large)
        ];

        Assert.That(BattleHitTester.HitTest(targets, new Point(280, 350)), Is.EqualTo(new BattleGridTile(BattleSide.Attacker, GridPosition.FrontBottom)));
    }

    [Test]
    public void LargerRegionOnlyTest()
    {
        Rect large = new(100, 100, 200, 400);
        Rect small = new(260, 300, 80, 120);

        BattleRenderState.HitTarget[] targets =
        [
            new(new BattleGridTile(BattleSide.Attacker, GridPosition.FrontBottom), small),
            new(new BattleGridTile(BattleSide.Defender, GridPosition.FrontTop), large)
        ];

        Assert.That(BattleHitTester.HitTest(targets, new Point(150, 150)), Is.EqualTo(new BattleGridTile(BattleSide.Defender, GridPosition.FrontTop)));
    }
}
