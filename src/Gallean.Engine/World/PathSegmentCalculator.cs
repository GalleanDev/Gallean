using System.Collections.Immutable;

namespace Gallean.Engine;

internal static class PathSegmentCalculator
{
    public static WorldPath Calculate(
        IReadOnlyList<(WorldPosition From, WorldPosition To, int Cost)> steps,
        WorldMovementLimit limit)
    {
        ImmutableArray<WorldPathSegment>.Builder segments = ImmutableArray.CreateBuilder<WorldPathSegment>(steps.Count);
        int turn = 0;
        int turnMovementPoints = limit.MovementPoints;
        int turnCost = 0;

        foreach ((WorldPosition from, WorldPosition to, int stepCost) in steps)
        {
            if (!WorldMovementCalculator.CanBeginStep(turnMovementPoints, stepCost, turnCost))
            {
                turn++;
                turnMovementPoints = limit.MaxMovementPoints;
                turnCost = 0;
            }

            if (turn < limit.TurnLimit)
            {
                turnCost += stepCost;
                segments.Add(new WorldPathSegment(from, to, stepCost, turn));
            }
            else
            {
                break;
            }
        }

        return new WorldPath(segments.ToImmutable());
    }
}
