namespace Gallean.Engine;

internal static class TerrainTransformCalculator
{
    private const int PercentageMax = 100;

    public static bool IsTransformable(WorldMapCell cell)
    {
        return cell.Terrain != WorldMapTerrain.Water &&
               cell.Feature != WorldMapFeature.Mountain;
    }

    public static WorldMap Transform(WorldMap map, WorldState state, IReadOnlyList<WorldPosition> positions, WorldMapTerrain terrain)
    {
        return TransformCells(map, state, positions, terrain);
    }

    public static WorldMap TransformArea(WorldState state, WorldPosition center, int areaSize, WorldMapTerrain terrain)
    {
        int left = center.X - areaSize / 2;
        int top = center.Y - areaSize / 2;
        int right = left + areaSize;
        int bottom = top + areaSize;
        List<WorldPosition> positions = [];

        for (int y = top; y < bottom; y++)
        {
            for (int x = left; x < right; x++)
            {
                positions.Add(new WorldPosition(x, y));
            }
        }

        return TransformCells(state.Map, state, positions, terrain);
    }

    private static WorldMap TransformCells(
        WorldMap map,
        WorldState state,
        IReadOnlyList<WorldPosition> positions,
        WorldMapTerrain terrain)
    {
        bool[] visitedCells = new bool[map.Width * map.Height];
        Queue<WorldPosition> queue = new(positions);
        List<int> indexes = [];

        while (queue.Count > 0)
        {
            WorldPosition position = queue.Dequeue();

            if (map.IsValidPosition(position))
            {
                int index = GetIndex(map, position);

                if (!visitedCells[index])
                {
                    visitedCells[index] = true;

                    if (IsTransformable(map.GetCellAt(position)))
                    {
                        indexes.Add(index);
                    }

                    EnqueueResourceNode(state, queue, position);
                }
            }
        }

        if (indexes.Count > 0)
        {
            map = map.UpdateCells(cells =>
            {
                foreach (int index in indexes)
                {
                    cells[index] = cells[index] with { Terrain = terrain };
                }
            });
        }

        return map;
    }

    private static void EnqueueResourceNode(WorldState state, Queue<WorldPosition> queue, WorldPosition position)
    {
        foreach (WorldResourceNode node in state.ResourceNodes.Values)
        {
            if (node.Position.GetDistance(position) <= 1)
            {
                queue.Enqueue(node.Position);

                foreach (WorldPosition adjacent in node.Position.GetAdjacentPositions())
                {
                    queue.Enqueue(adjacent);
                }
            }
        }
    }

    private static int GetIndex(WorldMap map, WorldPosition position)
    {
        return position.Y * map.Width + position.X;
    }

    public static int GetTransformedPercentage(WorldMap map, WorldMapTerrain terrain)
    {
        int transformable = 0;
        int transformed = 0;

        foreach (WorldMapCell cell in map.Cells)
        {
            if (IsTransformable(cell))
            {
                transformable++;

                if (cell.Terrain == terrain)
                {
                    transformed++;
                }
            }
        }

        if (transformable > 0)
        {
            return transformed * PercentageMax / transformable;
        }
        else
        {
            return 0;
        }
    }
}
