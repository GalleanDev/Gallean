using Gallean.Engine;
using SkiaSharp;
using static Gallean.Client.WorldRenderContent;
using static Gallean.Client.WorldRenderState;

namespace Gallean.Client;

internal sealed partial class WorldRenderContentProvider
{
    private sealed record ReachContent(ReachLayer? Layer);

    private static class Reach
    {
        public static WorldReachShaderProgram BuildProgram()
        {
            WorldReachShaderProgram program = new();

            using WorldReachShaderProgram.Editor editor = program.Edit();

            editor.SetHalfTile(new SKPoint(WorldTransform.TileHalfWidth, WorldTransform.TileHalfHeight));
            editor.SetFillColor(Theme.World.Reach.FillColor.ToSKColorF());
            editor.SetFeatherColor(Theme.World.Reach.FeatherColor.ToSKColorF());
            editor.SetEdgeColor(Theme.World.Reach.EdgeColor.ToSKColorF());
            editor.SetOutlineColor(Theme.World.Reach.OutlineColor.ToSKColorF());

            return program;
        }

        public static ReachContent Build(ReachOverlay? overlay)
        {
            if (overlay is not null && overlay.Tiles.Count > 0)
            {
                return new ReachContent(BuildLayer(overlay.Tiles));
            }
            else
            {
                return new ReachContent(null);
            }
        }

        private static ReachLayer BuildLayer(IReadOnlyList<WorldPosition> tiles)
        {
            HashSet<WorldPosition> set = [.. tiles];
            List<WorldPosition> interior = [];
            List<WorldPosition> edge = [];

            foreach (WorldPosition tile in tiles)
            {
                if (tile.GetAdjacentPositions().All(set.Contains))
                {
                    interior.Add(tile);
                }
                else
                {
                    edge.Add(tile);
                }
            }

            SKVertices? interiorMesh = interior.Count > 0 ? BuildTileMesh(interior, 0) : null;

            return new ReachLayer(interiorMesh, BuildTileMesh(edge, 0), BuildTiles(tiles));
        }

        private static SKShader BuildTiles(IReadOnlyList<WorldPosition> tiles)
        {
            int left = tiles.Min(tile => tile.X);
            int top = tiles.Min(tile => tile.Y);
            int width = tiles.Max(tile => tile.X) - left + 1;
            int height = tiles.Max(tile => tile.Y) - top + 1;
            byte[] pixels = new byte[width * height];

            foreach (WorldPosition tile in tiles)
            {
                pixels[((tile.Y - top) * width) + tile.X - left] = byte.MaxValue;
            }

            SKImageInfo info = new(width, height, SKColorType.Alpha8, SKAlphaType.Premul);
            SKImage image = SKImage.FromPixelCopy(info, pixels);
            SKMatrix transform = SKMatrix.CreateTranslation(left, top);

            return image.ToShader(SKShaderTileMode.Decal, SKShaderTileMode.Decal, RenderSamplingOptions.Nearest, transform);
        }
    }
}
