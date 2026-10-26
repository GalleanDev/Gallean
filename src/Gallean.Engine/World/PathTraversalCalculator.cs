namespace Gallean.Engine;

internal static partial class PathTraversalCalculator
{
    public sealed record TraversalInfo(WorldPath Path, WorldPath Remainder, Encounter? Encounter);

    public static TraversalInfo Calculate(
        WorldState state,
        WorldStack stack,
        WorldPath path,
        WorldMovementCalculator.MovementProfile profile,
        IGameDefinitionProvider definitions)
    {
        WorldPlan plan = WorldPlan.For(state);

        for (int i = 0; i < path.Positions.Length; i++)
        {
            WorldPosition position = path.Positions[i];
            WorldPath approachPath = path.Truncate(i);
            bool canBeginBattle = WorldMovementCalculator.CanBeginBattle(stack.MovementPoints, approachPath.Cost);

            WorldBag? traverseBag = plan.GetBagAt(position);
            if (traverseBag is not null)
            {
                return new TraversalInfo(approachPath, path.Skip(i), new Encounter.Bag(traverseBag));
            }

            WorldStack? traverseStack = plan.GetStackAt(position);
            if (traverseStack is not null && DiplomacyCalculator.IsHostile(state, stack.OwnerId, traverseStack.OwnerId))
            {
                return new TraversalInfo(approachPath, path.Skip(i), new Encounter.StackBattle(traverseStack, canBeginBattle));
            }

            WorldFortification? traverseFortification = plan.GetFortificationAt(position);
            if (traverseFortification is not null && traverseFortification.Garrison.HasAliveUnits())
            {
                WorldPlayerId? fortificationOwner = FortificationCalculator.GetOwner(traverseFortification);

                if (fortificationOwner is null || DiplomacyCalculator.IsHostile(state, stack.OwnerId, fortificationOwner.Value))
                {
                    return new TraversalInfo(approachPath, path.Skip(i), new Encounter.FortificationBattle(traverseFortification, canBeginBattle));
                }
            }

            WorldStack? interceptingStack = FindInterceptingStack(state, plan, stack, profile, position, definitions);
            if (interceptingStack is not null)
            {
                return new TraversalInfo(
                    path.Truncate(i + 1),
                    path.Skip(i + 1),
                    new Encounter.StackBattle(interceptingStack, canBeginBattle));
            }
        }

        return new TraversalInfo(path, new WorldPath([]), null);
    }

    private static WorldStack? FindInterceptingStack(
        WorldState state,
        WorldPlan plan,
        WorldStack stack,
        WorldMovementCalculator.MovementProfile profile,
        WorldPosition position,
        IGameDefinitionProvider definitions)
    {
        foreach (WorldPosition adjacentPosition in position.GetAdjacentPositions())
        {
            WorldStack? adjacentStack = plan.GetStackAt(adjacentPosition);

            if (adjacentStack is not null &&
                plan.GetFortificationAt(adjacentPosition) is null &&
                DiplomacyCalculator.IsHostile(state, stack.OwnerId, adjacentStack.OwnerId))
            {
                (_, _, UnitDefinition leaderDefinition) = adjacentStack.GetLeader(definitions);

                if (!profile.IsThief || leaderDefinition.Category is UnitCategory.Illusion)
                {
                    return adjacentStack;
                }
            }
        }

        return null;
    }
}
