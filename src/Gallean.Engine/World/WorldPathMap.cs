using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Gallean.Engine;

internal sealed class WorldPathMap(
    WorldMap map,
    WorldPosition start,
    int[] costs,
    int[] predecessors)
{
    public bool TryGetPath(WorldPosition target, WorldMovementLimit limit, [NotNullWhen(true)] out WorldPath? path)
    {
        if (costs[GetIndex(target)] >= int.MaxValue)
        {
            path = null;
            return false;
        }

        Stack<(WorldPosition From, WorldPosition To, int Cost)> steps = new();
        WorldPosition current = target;

        while (current != start)
        {
            int predecessor = predecessors[GetIndex(current)];

            if (predecessor < 0)
            {
                path = null;
                return false;
            }

            WorldPosition previous = GetPosition(predecessor);
            int stepCost = costs[GetIndex(current)] - costs[GetIndex(previous)];
            steps.Push((previous, current, stepCost));
            current = previous;
        }

        path = PathSegmentCalculator.Calculate([.. steps], limit);
        return true;
    }

    public ImmutableArray<WorldPosition> GetReachablePositions(int movementPoints)
    {
        ImmutableArray<WorldPosition>.Builder result = ImmutableArray.CreateBuilder<WorldPosition>();

        for (int index = 0; index < costs.Length; index++)
        {
            if (predecessors[index] >= 0 && IsReachable(index, movementPoints))
            {
                result.Add(GetPosition(index));
            }
        }

        return result.ToImmutable();
    }

    private bool IsReachable(int index, int movementPoints)
    {
        int entry = index;

        while (predecessors[entry] >= 0 && costs[predecessors[entry]] == costs[entry])
        {
            entry = predecessors[entry];
        }

        int previous = predecessors[entry];

        if (previous >= 0)
        {
            return WorldMovementCalculator.CanBeginStep(movementPoints, costs[entry] - costs[previous], costs[previous]);
        }
        else
        {
            return true;
        }
    }

    private int GetIndex(WorldPosition position)
    {
        return position.Y * map.Width + position.X;
    }

    private WorldPosition GetPosition(int index)
    {
        return new WorldPosition(index % map.Width, index / map.Width);
    }
}
