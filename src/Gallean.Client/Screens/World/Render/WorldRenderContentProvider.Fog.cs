using Avalonia;
using Avalonia.Media;
using Avalonia.Skia;
using SkiaSharp;
using static Gallean.Client.WorldRenderContent;
using static Gallean.Client.WorldRenderState;

namespace Gallean.Client;

internal sealed partial class WorldRenderContentProvider
{
    private static class Fog
    {
        private const int MapEdgeBand = 8;
        private const int SamplesPerTile = 4;
        private const int SearchRadius = 1;
        private const double ClearRadius = 0.7;
        private const double FadeDistance = 0.6;

        public static FogContent Build(IReadOnlyList<FogTile> fogTiles, WorldMap map)
        {
            if (fogTiles.Count > 0)
            {
                List<WorldPosition> positions = [];

                foreach (FogTile tile in fogTiles)
                {
                    positions.Add(tile.Position);
                }

                List<WorldPosition> mapEdgePositions = GetMapEdgePositions(fogTiles, map);
                SKVertices? mapEdgeMesh = null;

                if (mapEdgePositions.Count > 0)
                {
                    mapEdgeMesh = BuildMesh(mapEdgePositions);
                }

                return new FogContent(new FogLayer(BuildMesh(positions), mapEdgeMesh, BuildShader(fogTiles, map)));
            }
            else
            {
                return new FogContent(null);
            }
        }

        private static List<WorldPosition> GetMapEdgePositions(IReadOnlyList<FogTile> fogTiles, WorldMap map)
        {
            HashSet<WorldPosition> unexplored = [];

            foreach (FogTile tile in fogTiles)
            {
                unexplored.Add(tile.Position);
            }

            List<WorldPosition> positions = [];

            for (int y = -MapEdgeBand; y < map.Height + MapEdgeBand; y++)
            {
                for (int x = -MapEdgeBand; x < map.Width + MapEdgeBand; x++)
                {
                    WorldPosition position = new(x, y);
                    WorldPosition nearest = new(Math.Clamp(x, 0, map.Width - 1), Math.Clamp(y, 0, map.Height - 1));

                    if (position != nearest && unexplored.Contains(nearest))
                    {
                        positions.Add(position);
                    }
                }
            }

            return positions;
        }

        private static SKVertices BuildMesh(IReadOnlyList<WorldPosition> fogPositions)
        {
            Dictionary<(int X, int Y), ushort> corners = [];
            List<SKPoint> positions = [];
            List<ushort> indexes = [];

            foreach (WorldPosition fogPosition in fogPositions)
            {
                ushort top = Corner(fogPosition.X, fogPosition.Y);
                ushort right = Corner(fogPosition.X + 1, fogPosition.Y);
                ushort bottom = Corner(fogPosition.X + 1, fogPosition.Y + 1);
                ushort left = Corner(fogPosition.X, fogPosition.Y + 1);

                indexes.AddRange([top, right, bottom, top, bottom, left]);
            }

            return SKVertices.CreateCopy(SKVertexMode.Triangles, [.. positions], null, null, [.. indexes]);

            ushort Corner(int x, int y)
            {
                if (!corners.TryGetValue((x, y), out ushort index))
                {
                    Point point = WorldTransform.ToWorldPoint(x - 0.5, y - 0.5);
                    index = (ushort)positions.Count;
                    positions.Add(point.ToSKPoint());
                    corners[(x, y)] = index;
                }

                return index;
            }
        }

        private static SKShader BuildShader(IReadOnlyList<FogTile> fogTiles, WorldMap map)
        {
            SKImage mask = BuildMask(fogTiles, map);

            float halfWidth = (float)WorldTransform.TileHalfWidth / SamplesPerTile;
            float halfHeight = (float)WorldTransform.TileHalfHeight / SamplesPerTile;

            SKMatrix matrix = new(
                halfWidth, -halfWidth, 0,
                halfHeight, halfHeight, -WorldTransform.TileHalfHeight,
                0, 0, 1);

            return mask.ToShader(SKShaderTileMode.Clamp, SKShaderTileMode.Clamp, RenderSamplingOptions.Linear, matrix);
        }

        private static SKImage BuildMask(IReadOnlyList<FogTile> fogTiles, WorldMap map)
        {
            bool[] explored = new bool[map.Width * map.Height];
            Array.Fill(explored, true);

            foreach (FogTile tile in fogTiles)
            {
                explored[(tile.Position.Y * map.Width) + tile.Position.X] = false;
            }

            int width = map.Width * SamplesPerTile;
            int height = map.Height * SamplesPerTile;
            byte[] pixels = new byte[width * height * 4];

            foreach (FogTile tile in fogTiles)
            {
                WriteTile(pixels, map, explored, tile.Position);
            }

            SKImageInfo info = new(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);

            SKImage image = SKImage.FromPixelCopy(info, pixels);
            SkiaMemoryPressure.TrackImage(image);

            return image;
        }

        private static void WriteTile(byte[] pixels, WorldMap map, bool[] explored, WorldPosition position)
        {
            Color color = Theme.World.Tile.HiddenColor;
            int width = map.Width * SamplesPerTile;

            for (int sampleY = 0; sampleY < SamplesPerTile; sampleY++)
            {
                int pixelY = (position.Y * SamplesPerTile) + sampleY;
                double y = ((pixelY + 0.5) / SamplesPerTile) - 0.5;

                for (int sampleX = 0; sampleX < SamplesPerTile; sampleX++)
                {
                    int pixelX = (position.X * SamplesPerTile) + sampleX;
                    double x = ((pixelX + 0.5) / SamplesPerTile) - 0.5;

                    byte alpha = (byte)Math.Round(color.A * GetOpacity(map, explored, position, x, y));
                    int offset = ((pixelY * width) + pixelX) * 4;

                    pixels[offset + 0] = (byte)(color.B * alpha / 255);
                    pixels[offset + 1] = (byte)(color.G * alpha / 255);
                    pixels[offset + 2] = (byte)(color.R * alpha / 255);
                    pixels[offset + 3] = alpha;
                }
            }
        }

        private static double GetOpacity(WorldMap map, bool[] explored, WorldPosition position, double x, double y)
        {
            double distance = double.MaxValue;

            for (int offsetY = -SearchRadius; offsetY <= SearchRadius; offsetY++)
            {
                for (int offsetX = -SearchRadius; offsetX <= SearchRadius; offsetX++)
                {
                    WorldPosition neighbor = new(position.X + offsetX, position.Y + offsetY);

                    if (map.IsValidPosition(neighbor) && explored[(neighbor.Y * map.Width) + neighbor.X])
                    {
                        double deltaX = x - neighbor.X;
                        double deltaY = y - neighbor.Y;

                        distance = Math.Min(distance, Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY)));
                    }
                }
            }

            double fade = Math.Clamp((distance - ClearRadius) / FadeDistance, 0, 1);

            return fade * fade * (3 - (2 * fade));
        }
    }
}
