using Gallean.Common;

namespace Gallean.Engine;

internal sealed partial class WorldAiPathfinding(IEngineContext context)
{
    public WorldPathMap? GetPathMap(WorldState state, WorldStack stack)
    {
        WorldPlan plan = WorldPlan.For(state);
        WorldPlayer player = state.Players[stack.OwnerId];
        ImmutableBitmap? fogOfWar = stack.Order.Accept(WorldAiFogOfWarVisitor.Instance, player);

        return plan.GetPathMap(stack.Position, fogOfWar, GetMovementProfile(stack));
    }

    public WorldPosition? FindMoveTowardCell(WorldState state, WorldStack stack, WorldPathMap pathMap, WorldPosition target)
    {
        WorldMovementLimit movementLimit = new(
            stack.MovementPoints,
            context.UnitStats.GetWorldStackStats(stack).MaxMovementPoints,
            TurnLimit: 1);

        if (!pathMap.TryGetPath(target, movementLimit, out WorldPath? path))
        {
            return null;
        }

        for (int count = path.Segments.Length; count >= 1; count--)
        {
            WorldPosition candidate = path.Positions[count - 1];

            WorldCommandResult commandResult = new WorldAiMoveStackCommand(stack.Id, candidate).Execute(state, context);
            if (!commandResult.Failed)
            {
                PathTraversalCalculator.TraversalInfo traversal = GetTraversalInfo(state, stack, path.Truncate(count));

                if (traversal.Encounter is not PathTraversalCalculator.Encounter.Battle { CanAvoid: false })
                {
                    return candidate;
                }
            }
        }

        return null;
    }

    public PathTraversalCalculator.TraversalInfo GetTraversalInfo(WorldState state, WorldStack stack, WorldPath path)
    {
        return PathTraversalCalculator.Calculate(state, stack, path, GetMovementProfile(stack), context.Definitions);
    }

    public IReadOnlyList<TargetPath> GetTargetPaths(WorldState state, WorldPathMap pathMap, IEnumerable<WorldAiTarget> targets)
    {
        List<TargetPath> result = [];

        foreach (WorldAiTarget target in targets)
        {
            WorldPosition? position = target.Accept(WorldAiTargetPositionVisitor.Instance, state);

            if (position is not null && pathMap.TryGetPath(position.Value, WorldMovementLimit.Unlimited, out WorldPath? path))
            {
                result.Add(new TargetPath(target, position.Value, path));
            }
        }

        return result;
    }

    private WorldMovementCalculator.MovementProfile GetMovementProfile(WorldStack stack)
    {
        return WorldMovementCalculator.GetMovementProfile(stack, context.Definitions);
    }
}
