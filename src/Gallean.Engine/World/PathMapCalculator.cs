using System.Buffers;
using Gallean.Common;

namespace Gallean.Engine;

internal static class PathMapCalculator
{
    public static WorldPathMap? Calculate(
        WorldMap map,
        WorldPosition start,
        ImmutableBitmap? fogOfWar,
        ImmutableBitmap blockedCells,
        ImmutableBitmap terminalCells,
        Func<WorldPosition, WorldPosition, int> stepCost)
    {
        if (blockedCells.Get(start.X, start.Y))
        {
            return null;
        }

        Span<WorldPosition> neighbors = stackalloc WorldPosition[8];

        int cellCount = map.Width * map.Height;
        int startIndex = GetIndex(map, start);

        int[] predecessors = new int[cellCount];
        Array.Fill(predecessors, -1);

        int[] distanceScore = new int[cellCount];
        Array.Fill(distanceScore, int.MaxValue);
        distanceScore[startIndex] = 0;

        int[] diagonalScore = ArrayPool<int>.Shared.Rent(cellCount);
        Array.Fill(diagonalScore, int.MaxValue, 0, cellCount);
        diagonalScore[startIndex] = 0;

        bool[] closedSet = ArrayPool<bool>.Shared.Rent(cellCount);
        Array.Clear(closedSet, 0, cellCount);

        PriorityQueue<WorldPosition, long> openSet = new();
        openSet.Enqueue(start, GetPriority(0, startIndex));

        while (openSet.Count > 0)
        {
            WorldPosition current = openSet.Dequeue();
            int currentIndex = GetIndex(map, current);

            if (closedSet[currentIndex])
            {
                continue;
            }

            closedSet[currentIndex] = true;

            if (current != start && terminalCells.Get(current.X, current.Y))
            {
                continue;
            }

            int neighborCount = GetNeighbors(map, current, fogOfWar, blockedCells, neighbors);
            for (int i = 0; i < neighborCount; i++)
            {
                WorldPosition neighbor = neighbors[i];
                int neighborIndex = GetIndex(map, neighbor);

                if (closedSet[neighborIndex])
                {
                    continue;
                }

                int moveCost = stepCost(current, neighbor);
                if (moveCost >= int.MaxValue)
                {
                    continue;
                }

                int tentativeDistance = distanceScore[currentIndex] + moveCost;
                int tentativeDiagonals = diagonalScore[currentIndex] + (IsDiagonal(current, neighbor) ? 1 : 0);

                if (tentativeDistance > distanceScore[neighborIndex])
                {
                    continue;
                }

                if (tentativeDistance == distanceScore[neighborIndex] &&
                    tentativeDiagonals >= diagonalScore[neighborIndex])
                {
                    continue;
                }

                predecessors[neighborIndex] = currentIndex;
                distanceScore[neighborIndex] = tentativeDistance;
                diagonalScore[neighborIndex] = tentativeDiagonals;
                openSet.Enqueue(neighbor, GetPriority(tentativeDistance, neighborIndex));
            }
        }

        ArrayPool<int>.Shared.Return(diagonalScore);
        ArrayPool<bool>.Shared.Return(closedSet);

        return new WorldPathMap(map, start, distanceScore, predecessors);
    }

    private static int GetNeighbors(
        WorldMap map,
        WorldPosition position,
        ImmutableBitmap? fogOfWar,
        ImmutableBitmap blockedCells,
        Span<WorldPosition> neighbors)
    {
        int count = 0;

        count = AddNeighbor(map, position with { Y = position.Y - 1 }, fogOfWar, blockedCells, neighbors, count);
        count = AddNeighbor(map, position with { Y = position.Y + 1 }, fogOfWar, blockedCells, neighbors, count);
        count = AddNeighbor(map, position with { X = position.X - 1 }, fogOfWar, blockedCells, neighbors, count);
        count = AddNeighbor(map, position with { X = position.X + 1 }, fogOfWar, blockedCells, neighbors, count);
        count = AddNeighbor(map, new WorldPosition(position.X - 1, position.Y - 1), fogOfWar, blockedCells, neighbors, count);
        count = AddNeighbor(map, new WorldPosition(position.X + 1, position.Y - 1), fogOfWar, blockedCells, neighbors, count);
        count = AddNeighbor(map, new WorldPosition(position.X - 1, position.Y + 1), fogOfWar, blockedCells, neighbors, count);
        count = AddNeighbor(map, new WorldPosition(position.X + 1, position.Y + 1), fogOfWar, blockedCells, neighbors, count);

        return count;
    }

    private static int AddNeighbor(
        WorldMap map,
        WorldPosition candidate,
        ImmutableBitmap? fogOfWar,
        ImmutableBitmap blockedCells,
        Span<WorldPosition> neighbors,
        int count)
    {
        if (!map.IsValidPosition(candidate))
        {
            return count;
        }

        if (fogOfWar is not null && !fogOfWar.Get(candidate.X, candidate.Y))
        {
            return count;
        }

        if (blockedCells.Get(candidate.X, candidate.Y))
        {
            return count;
        }

        neighbors[count] = candidate;

        return count + 1;
    }

    private static long GetPriority(int cost, int index)
    {
        return cost * (1L << 32) + index;
    }

    private static bool IsDiagonal(WorldPosition from, WorldPosition to)
    {
        return from.X != to.X && from.Y != to.Y;
    }

    private static int GetIndex(WorldMap map, WorldPosition position)
    {
        return position.Y * map.Width + position.X;
    }
}
