using Gallean.Engine;
using static Gallean.Client.WorldRenderState;

namespace Gallean.Client;

internal sealed partial class WorldRenderComposer
{
    private sealed class TerrainExpansionBuilder(WorldMap oldMap, WorldMap map)
    {
        private readonly record struct TileDiff(WorldMapCell OldCell, WorldMapCell NewCell)
        {
            public bool Changed => OldCell.Terrain != NewCell.Terrain;
        }

        private readonly record struct Corner(int X, int Y)
        {
            public static Corner[] GetCorners(WorldPosition position)
            {
                return
                [
                    new Corner(position.X, position.Y),
                    new Corner(position.X + 1, position.Y),
                    new Corner(position.X + 1, position.Y + 1),
                    new Corner(position.X, position.Y + 1)
                ];
            }

            public Corner[] GetNeighbors()
            {
                return
                [
                    this with { Y = Y - 1 },
                    this with { X = X + 1 },
                    this with { Y = Y + 1 },
                    this with { X = X - 1 }
                ];
            }

            public WorldPosition[] GetTiles()
            {
                return
                [
                    new WorldPosition(X, Y),
                    new WorldPosition(X - 1, Y),
                    new WorldPosition(X - 1, Y - 1),
                    new WorldPosition(X, Y - 1)
                ];
            }
        }

        private static readonly TimeSpan StartStep = TimeSpan.FromSeconds(0.3);
        private static readonly TimeSpan MaxStart = TimeSpan.FromSeconds(1.8);
        private static readonly TimeSpan StartVariation = TimeSpan.FromMilliseconds(200);
        private static readonly TimeSpan FadeDuration = TimeSpan.FromSeconds(0.5);

        public TerrainExpansion Build()
        {
            Dictionary<WorldPosition, TileDiff> diff = ComputeDiff();
            Dictionary<Corner, TimeSpan> starts = ComputeStarts(diff);
            MapTerrain oldTerrain = Terrain.Build(new Terrain.Input(oldMap));

            List<TerrainExpansionTile> tiles = [];
            List<TerrainExpansionTree> trees = [];
            HashSet<WorldMapTerrain> oldTerrains = [];

            foreach ((WorldPosition position, TileDiff tile) in diff)
            {
                Corner[] corners = Corner.GetCorners(position);
                TimeSpan topStart = starts[corners[0]];
                TimeSpan rightStart = starts[corners[1]];
                TimeSpan bottomStart = starts[corners[2]];
                TimeSpan leftStart = starts[corners[3]];

                tiles.Add(new TerrainExpansionTile(position, topStart, rightStart, bottomStart, leftStart));
                oldTerrains.Add(tile.OldCell.Terrain);

                if (tile is { Changed: true, OldCell.Feature: WorldMapFeature.Forest })
                {
                    trees.Add(new TerrainExpansionTree(
                        position,
                        GetDepth(position, Layer.Trees),
                        new AssetSpriteKey.Tree(tile.OldCell.Terrain, tile.OldCell.FeatureVariant.Value),
                        (topStart + rightStart + bottomStart + leftStart) / 4.0));
                }
            }

            List<MapTile> oldTiles = [];
            List<WorldMapTerrain> oldGroundTerrains = [];
            List<MapTileBorder> oldBorders = [];

            foreach (MapTile oldTile in oldTerrain.MapTiles)
            {
                if (diff.ContainsKey(oldTile.Position))
                {
                    oldTiles.Add(oldTile);
                }
            }

            foreach (WorldMapTerrain terrain in oldTerrain.GroundTerrains)
            {
                if (oldTerrains.Contains(terrain))
                {
                    oldGroundTerrains.Add(terrain);
                }
            }

            foreach (MapTileBorder border in oldTerrain.Borders)
            {
                if (diff.ContainsKey(border.Position))
                {
                    oldBorders.Add(border);
                }
            }

            TimeSpan lastStart = TimeSpan.Zero;

            foreach (TimeSpan start in starts.Values)
            {
                if (start > lastStart)
                {
                    lastStart = start;
                }
            }

            return new TerrainExpansion(
                new MapTerrain(oldMap, oldTiles, oldGroundTerrains, oldBorders),
                tiles,
                trees,
                FadeDuration,
                lastStart + FadeDuration);
        }

        private Dictionary<WorldPosition, TileDiff> ComputeDiff()
        {
            Dictionary<WorldPosition, TileDiff> diff = [];

            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    WorldPosition position = new(x, y);

                    if (IsChanged(position) || HasChangedNeighbor(position))
                    {
                        diff[position] = new TileDiff(oldMap.GetCellAt(position), map.GetCellAt(position));
                    }
                }
            }

            return diff;
        }

        private bool HasChangedNeighbor(WorldPosition position)
        {
            foreach (WorldPosition neighbor in position.GetAdjacentPositions())
            {
                if (map.IsValidPosition(neighbor) && IsChanged(neighbor))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsChanged(WorldPosition position)
        {
            return oldMap.GetCellAt(position).Terrain != map.GetCellAt(position).Terrain;
        }

        private static Dictionary<Corner, TimeSpan> ComputeStarts(Dictionary<WorldPosition, TileDiff> diff)
        {
            Dictionary<Corner, int> distances = ComputeDistances(diff);
            int maxDistance = 0;

            foreach (int distance in distances.Values)
            {
                maxDistance = Math.Max(maxDistance, distance);
            }

            TimeSpan startStep = StartStep;

            if (StartStep * maxDistance > MaxStart)
            {
                startStep = MaxStart / maxDistance;
            }

            Dictionary<Corner, TimeSpan> starts = [];

            foreach (WorldPosition position in diff.Keys)
            {
                foreach (Corner corner in Corner.GetCorners(position))
                {
                    int distance = distances.GetValueOrDefault(corner, maxDistance);
                    TimeSpan variation = TimeSpan.FromMilliseconds(StableHash.Combine(corner.X, corner.Y) % StartVariation.TotalMilliseconds);

                    starts[corner] = (startStep * distance) + variation;
                }
            }

            return starts;
        }

        private static Dictionary<Corner, int> ComputeDistances(Dictionary<WorldPosition, TileDiff> diff)
        {
            List<WorldPosition> changes = [];
            HashSet<Corner> corners = [];

            foreach ((WorldPosition position, TileDiff tile) in diff)
            {
                if (tile.Changed)
                {
                    changes.Add(position);
                    corners.UnionWith(Corner.GetCorners(position));
                }
            }

            Dictionary<Corner, int> distances = [];
            Queue<Corner> queue = new();

            foreach (Corner corner in FindStartCorners(diff, changes))
            {
                if (distances.TryAdd(corner, 0))
                {
                    queue.Enqueue(corner);
                }
            }

            while (queue.Count > 0)
            {
                Corner corner = queue.Dequeue();
                int distance = distances[corner];

                foreach (Corner neighbor in corner.GetNeighbors())
                {
                    if (corners.Contains(neighbor))
                    {
                        if (distances.TryAdd(neighbor, distance + 1))
                        {
                            queue.Enqueue(neighbor);
                        }
                    }
                }
            }

            int maxDistance = 0;

            foreach (int distance in distances.Values)
            {
                maxDistance = Math.Max(maxDistance, distance);
            }

            foreach (Corner corner in corners)
            {
                distances.TryAdd(corner, maxDistance + 1);
            }

            return distances;
        }

        private static List<Corner> FindStartCorners(Dictionary<WorldPosition, TileDiff> diff, List<WorldPosition> changes)
        {
            List<Corner> result = [];

            foreach (WorldPosition change in changes)
            {
                WorldMapTerrain terrain = diff[change].NewCell.Terrain;

                foreach (Corner corner in Corner.GetCorners(change))
                {
                    if (HasUnchangedTile(diff, corner, terrain))
                    {
                        result.Add(corner);
                    }
                }
            }

            if (result.Count == 0 && changes.Count > 0)
            {
                WorldPosition center = FindCenter(changes);
                result.AddRange(Corner.GetCorners(center));
            }

            return result;
        }

        private static bool HasUnchangedTile(Dictionary<WorldPosition, TileDiff> diff, Corner corner, WorldMapTerrain terrain)
        {
            foreach (WorldPosition position in corner.GetTiles())
            {
                if (diff.TryGetValue(position, out TileDiff tile))
                {
                    if (!tile.Changed && tile.NewCell.Terrain == terrain)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static WorldPosition FindCenter(List<WorldPosition> changes)
        {
            double centerX = 0;
            double centerY = 0;

            foreach (WorldPosition change in changes)
            {
                centerX += change.X;
                centerY += change.Y;
            }

            centerX /= changes.Count;
            centerY /= changes.Count;

            WorldPosition center = changes[0];
            double centerDistance = double.MaxValue;

            foreach (WorldPosition change in changes)
            {
                double distance = Math.Pow(change.X - centerX, 2) + Math.Pow(change.Y - centerY, 2);

                if (distance < centerDistance)
                {
                    center = change;
                    centerDistance = distance;
                }
            }

            return center;
        }
    }
}
