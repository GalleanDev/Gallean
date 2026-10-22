using static Gallean.Assets.AssetSpriteKey.RoadConnections;

namespace GameContent.Tests;

public sealed class GameTerrainSourceTests
{
    private static int EdgeIndex(AssetSpriteKey.GroundBorderEdges edges)
    {
        return GameTerrainSource.GetBorderImageIndex(new AssetSpriteKey.GroundBorderShape.Edges(edges));
    }

    private static int CornerIndex(AssetSpriteKey.GroundBorderCorners corners)
    {
        return GameTerrainSource.GetBorderImageIndex(new AssetSpriteKey.GroundBorderShape.Corners(corners));
    }

    [Test]
    public void RoadImageIndexTest()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(GameTerrainSource.GetRoadImageIndex(None), Is.Zero);
            Assert.That(GameTerrainSource.GetRoadImageIndex(All), Is.EqualTo(1));
            Assert.That(GameTerrainSource.GetRoadImageIndex(East | West), Is.EqualTo(2));
            Assert.That(GameTerrainSource.GetRoadImageIndex(North | South), Is.EqualTo(3));
            Assert.That(GameTerrainSource.GetRoadImageIndex(North | South | West), Is.EqualTo(4));
            Assert.That(GameTerrainSource.GetRoadImageIndex(North | East | West), Is.EqualTo(5));
            Assert.That(GameTerrainSource.GetRoadImageIndex(East | South | West), Is.EqualTo(6));
            Assert.That(GameTerrainSource.GetRoadImageIndex(North | East | South), Is.EqualTo(7));
            Assert.That(GameTerrainSource.GetRoadImageIndex(North | East), Is.EqualTo(8));
            Assert.That(GameTerrainSource.GetRoadImageIndex(East | South), Is.EqualTo(9));
            Assert.That(GameTerrainSource.GetRoadImageIndex(South | West), Is.EqualTo(10));
            Assert.That(GameTerrainSource.GetRoadImageIndex(North | West), Is.EqualTo(11));
            Assert.That(GameTerrainSource.GetRoadImageIndex(East), Is.EqualTo(12));
            Assert.That(GameTerrainSource.GetRoadImageIndex(South), Is.EqualTo(13));
            Assert.That(GameTerrainSource.GetRoadImageIndex(North), Is.EqualTo(14));
            Assert.That(GameTerrainSource.GetRoadImageIndex(West), Is.EqualTo(15));
        }
    }

    [Test]
    public void BorderImageIndexTest()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(EdgeIndex(AssetSpriteKey.GroundBorderEdges.West), Is.EqualTo(1));
            Assert.That(EdgeIndex(AssetSpriteKey.GroundBorderEdges.North), Is.EqualTo(2));
            Assert.That(EdgeIndex(AssetSpriteKey.GroundBorderEdges.East), Is.EqualTo(4));
            Assert.That(EdgeIndex(AssetSpriteKey.GroundBorderEdges.South), Is.EqualTo(8));
            Assert.That(EdgeIndex(AssetSpriteKey.GroundBorderEdges.North | AssetSpriteKey.GroundBorderEdges.West), Is.EqualTo(3));
            Assert.That(EdgeIndex(AssetSpriteKey.GroundBorderEdges.East | AssetSpriteKey.GroundBorderEdges.South), Is.EqualTo(12));
            Assert.That(EdgeIndex(AssetSpriteKey.GroundBorderEdges.All), Is.EqualTo(15));
            Assert.That(CornerIndex(AssetSpriteKey.GroundBorderCorners.NorthWest), Is.EqualTo(17));
            Assert.That(CornerIndex(AssetSpriteKey.GroundBorderCorners.NorthEast), Is.EqualTo(18));
            Assert.That(CornerIndex(AssetSpriteKey.GroundBorderCorners.SouthEast), Is.EqualTo(20));
            Assert.That(CornerIndex(AssetSpriteKey.GroundBorderCorners.SouthWest), Is.EqualTo(24));
            Assert.That(CornerIndex(AssetSpriteKey.GroundBorderCorners.All), Is.EqualTo(31));
        }
    }
}
