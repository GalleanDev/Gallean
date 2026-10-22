using Avalonia;

namespace Gallean.Client.Tests;

public sealed class BattleRenderLayoutTests
{
    private static Point GetFootPoint(BattleSide side, GridPosition position, bool isLarge)
    {
        return BattleRenderLayout.GetFootPoint(new BattleGridFootprint(side, position, isLarge));
    }

    [Test]
    public void AttackerFootPointsTest()
    {
        Assert.That(GetFootPoint(BattleSide.Attacker, GridPosition.FrontBottom, isLarge: false), Is.EqualTo(new Point(290.841, 372.666)));
        Assert.That(GetFootPoint(BattleSide.Attacker, GridPosition.FrontCenter, isLarge: false), Is.EqualTo(new Point(432.741, 308.866)));
        Assert.That(GetFootPoint(BattleSide.Attacker, GridPosition.FrontTop, isLarge: false), Is.EqualTo(new Point(542.741, 253.866)));
        Assert.That(GetFootPoint(BattleSide.Attacker, GridPosition.BackBottom, isLarge: false), Is.EqualTo(new Point(185.241, 292.366)));
        Assert.That(GetFootPoint(BattleSide.Attacker, GridPosition.BackCenter, isLarge: false), Is.EqualTo(new Point(322.741, 232.966)));
        Assert.That(GetFootPoint(BattleSide.Attacker, GridPosition.BackTop, isLarge: false), Is.EqualTo(new Point(451.441, 196.666)));
    }

    [Test]
    public void DefenderFootPointsTest()
    {
        Assert.That(GetFootPoint(BattleSide.Defender, GridPosition.FrontBottom, isLarge: false), Is.EqualTo(new Point(385.441, 446.366)));
        Assert.That(GetFootPoint(BattleSide.Defender, GridPosition.FrontCenter, isLarge: false), Is.EqualTo(new Point(528.441, 381.466)));
        Assert.That(GetFootPoint(BattleSide.Defender, GridPosition.FrontTop, isLarge: false), Is.EqualTo(new Point(649.441, 314.366)));
        Assert.That(GetFootPoint(BattleSide.Defender, GridPosition.BackBottom, isLarge: false), Is.EqualTo(new Point(497.641, 540.966)));
        Assert.That(GetFootPoint(BattleSide.Defender, GridPosition.BackCenter, isLarge: false), Is.EqualTo(new Point(653.841, 477.166)));
        Assert.That(GetFootPoint(BattleSide.Defender, GridPosition.BackTop, isLarge: false), Is.EqualTo(new Point(759.441, 385.866)));
    }

    [Test]
    public void LargeFootPointsTest()
    {
        Assert.That(GetFootPoint(BattleSide.Attacker, GridPosition.FrontCenter, isLarge: true), Is.EqualTo(new Point(380.216, 271.301)));
        Assert.That(GetFootPoint(BattleSide.Defender, GridPosition.FrontCenter, isLarge: true), Is.EqualTo(new Point(580.966, 419.031)));
    }

    [Test]
    public void GroundPlaneTest()
    {
        Assert.That(BattleRenderLayout.GroundNormal, Is.EqualTo(new Vector3D(0.0, -0.8116564171172348, 0.5841351389468137)));
        Assert.That(BattleRenderLayout.GroundDepthPerPixel, Is.EqualTo(1.7119326219665265));
        Assert.That(BattleRenderLayout.SceneDepthPerPixel, Is.EqualTo(1.3895010982914644));
        Assert.That(BattleRenderLayout.HeightPerPixel, Is.EqualTo(1.2320484122477664));
        Assert.That(BattleRenderLayout.SceneToGroundTransform, Is.EqualTo(new Matrix(0.004584425518456239, 0.0038259480900732887, -0.006410141098280686, 0.007960656903205124, 0.0, -0.0)));
        Assert.That(BattleRenderLayout.MarkerTransform, Is.EqualTo(new Matrix(167.49720624535803, 0.0, 0.0, 83.09435695041454, 0.0, 0.0)));
    }

    [Test]
    public void BackgroundWidthViewTest()
    {
        Assert.That(BattleRenderLayout.GetViewMatrix(2880, 600), Is.EqualTo(new Matrix(2, 0, 0, 2, 490, 0)));
    }

    [Test]
    public void SceneHeightWideScreenViewTest()
    {
        Assert.That(BattleRenderLayout.GetViewMatrix(1920, 1080), Is.EqualTo(new Matrix(1.8, 0, 0, 1.8, 105, 0)));
    }

    [Test]
    public void SceneHeightWideViewTest()
    {
        Assert.That(BattleRenderLayout.GetViewMatrix(1600, 1200), Is.EqualTo(new Matrix(2, 0, 0, 2, -150, 0)));
    }

    [Test]
    public void SceneHeightViewTest()
    {
        Assert.That(BattleRenderLayout.GetViewMatrix(500, 600), Is.EqualTo(new Matrix(1, 0, 0, 1, -225, 0)));
    }

    [Test]
    public void ScreenToSceneTest()
    {
        Point scene = BattleRenderLayout.ScreenToScene(new Point(-150, 240), 1600, 1200);

        Assert.That(scene, Is.EqualTo(new Point(0, 120)));
    }

    [Test]
    public void SmallMarkerRadiusTest()
    {
        BattleGridFootprint footprint = new(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false);

        Assert.That(BattleRenderLayout.GetMarkerRadius(footprint), Is.EqualTo(0.3));
    }

    [Test]
    public void LargeMarkerRadiusTest()
    {
        BattleGridFootprint footprint = new(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: true);

        Assert.That(BattleRenderLayout.GetMarkerRadius(footprint), Is.EqualTo(0.556));
    }

    [Test]
    public void SmallUnitBoundsTest()
    {
        BattleGridFootprint footprint = new(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false);

        Assert.That(
            BattleRenderLayout.GetContentBounds(BattleRenderLayout.GetUnitPlacement(footprint), new Rect(365, 410, 100, 200)),
            Is.EqualTo(new Rect(391.461, 232.498, 103.2, 206.4)));
    }

    [Test]
    public void LargeUnitBoundsTest()
    {
        BattleGridFootprint footprint = new(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: true);

        Assert.That(
            BattleRenderLayout.GetContentBounds(BattleRenderLayout.GetUnitPlacement(footprint), new Rect(365, 410, 100, 200)),
            Is.EqualTo(new Rect(357.521, 215.354, 102, 204)));
    }

    [Test]
    public void CorpseBoundsTest()
    {
        BattleGridFootprint footprint = new(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false);

        Assert.That(
            BattleRenderLayout.GetContentBounds(BattleRenderLayout.GetCorpsePlacement(footprint), new Rect(-20, -10, 40, 20)),
            Is.EqualTo(new Rect(412.101, 298.546, 41.28, 20.64)));
    }

    [Test]
    public void PlaceholderBoundsTest()
    {
        BattleGridFootprint footprint = new(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false);

        Assert.That(
            BattleRenderLayout.GetPlaceholderBounds(footprint),
            Is.EqualTo(new Rect(391.461, 232.498, 82.56, 82.56)));
    }

    [Test]
    public void FrameBoundsTest()
    {
        BattleGridFootprint footprint = new(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false);

        Assert.That(
            BattleRenderLayout.GetFrameBounds(BattleRenderLayout.GetUnitPlacement(footprint), new Rect(365, 410, 100, 200)),
            Is.EqualTo(new Rect(391.461, 232.498, 103.2, 206.4)));
    }

    [Test]
    public void TopAnchoredFrameBoundsTest()
    {
        BattleGridFootprint footprint = new(BattleSide.Defender, GridPosition.BackBottom, IsLarge: true);

        Assert.That(
            BattleRenderLayout.GetFrameBounds(BattleRenderLayout.GetUnitPlacement(footprint), new Rect(300, 0, 200, 500)),
            Is.EqualTo(new Rect(418.639, 0, 223.4, 586.182)));
    }

    [Test]
    public void AttackerGridPlacementTest()
    {
        Assert.That(
            BattleRenderLayout.GetGridPlacement(BattleSide.Attacker),
            Is.EqualTo(new AnimationPlacement(new Point(-27.259, -213.084), 1.02, new Point(377.741, 270.916))));
    }

    [Test]
    public void DefenderGridPlacementTest()
    {
        Assert.That(
            BattleRenderLayout.GetGridPlacement(BattleSide.Defender),
            Is.EqualTo(new AnimationPlacement(new Point(186.141, -54.684), 1.07, new Point(591.141, 429.316))));
    }
}
