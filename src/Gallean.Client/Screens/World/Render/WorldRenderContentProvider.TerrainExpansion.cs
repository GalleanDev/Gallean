using Avalonia;
using Avalonia.Skia;
using SkiaSharp;
using static Gallean.Client.WorldRenderContent;
using static Gallean.Client.WorldRenderState;

namespace Gallean.Client;

internal sealed partial class WorldRenderContentProvider
{
    private TerrainExpansionLayer? BuildTerrainExpansion(
        WorldAnimation? animation,
        IReadOnlyDictionary<WorldMapTerrain, TerrainTexture> terrainTextures,
        IAssetProvider assets)
    {
        if (animation is not TerrainExpansion terrainExpansion)
        {
            return null;
        }

        SKPath clip = BuildTerrainExpansionClip(terrainExpansion.Tiles);
        SKRect bounds = clip.Bounds;

        return new TerrainExpansionLayer(
            BuildGroundLayers(terrainExpansion.OldTerrain, terrainTextures),
            BuildCoastAtlas(terrainExpansion.OldTerrain, assets),
            BuildBorderAtlas(terrainExpansion.OldTerrain, terrainTextures, assets, terrainExpansionLandOverlayPart),
            clip,
            BuildTerrainExpansionMask(terrainExpansion),
            new Rect(bounds.Left, bounds.Top, bounds.Width, bounds.Height),
            terrainExpansion.Fade,
            terrainExpansion.Duration);
    }

    private static SKPath BuildTerrainExpansionClip(IReadOnlyList<TerrainExpansionTile> tiles)
    {
        SKPathBuilder builder = new();

        foreach (TerrainExpansionTile tile in tiles)
        {
            Point top = WorldTransform.ToWorldPoint(tile.Position.X - 0.5, tile.Position.Y - 0.5);
            Point right = WorldTransform.ToWorldPoint(tile.Position.X + 0.5, tile.Position.Y - 0.5);
            Point bottom = WorldTransform.ToWorldPoint(tile.Position.X + 0.5, tile.Position.Y + 0.5);
            Point left = WorldTransform.ToWorldPoint(tile.Position.X - 0.5, tile.Position.Y + 0.5);

            builder.MoveTo(top.ToSKPoint());
            builder.LineTo(right.ToSKPoint());
            builder.LineTo(bottom.ToSKPoint());
            builder.LineTo(left.ToSKPoint());
            builder.Close();
        }

        return builder.Detach();
    }

    private static SKVertices BuildTerrainExpansionMask(TerrainExpansion terrainExpansion)
    {
        Dictionary<(int X, int Y), ushort> corners = [];
        List<SKPoint> positions = [];
        List<SKColor> colors = [];
        List<ushort> indexes = [];

        foreach (TerrainExpansionTile tile in terrainExpansion.Tiles)
        {
            ushort top = Corner(tile.Position.X, tile.Position.Y, tile.TopStart);
            ushort right = Corner(tile.Position.X + 1, tile.Position.Y, tile.RightStart);
            ushort bottom = Corner(tile.Position.X + 1, tile.Position.Y + 1, tile.BottomStart);
            ushort left = Corner(tile.Position.X, tile.Position.Y + 1, tile.LeftStart);

            indexes.AddRange([top, right, bottom, top, bottom, left]);
        }

        return SKVertices.CreateCopy(SKVertexMode.Triangles, [.. positions], null, [.. colors], [.. indexes]);

        ushort Corner(int x, int y, TimeSpan start)
        {
            if (!corners.TryGetValue((x, y), out ushort index))
            {
                Point point = WorldTransform.ToWorldPoint(x - 0.5, y - 0.5);
                index = (ushort)positions.Count;
                positions.Add(point.ToSKPoint());
                colors.Add(new SKColor((byte)Math.Round(255.0 * (start / terrainExpansion.Duration)), 0, 0, 255));
                corners[(x, y)] = index;
            }

            return index;
        }
    }
}
