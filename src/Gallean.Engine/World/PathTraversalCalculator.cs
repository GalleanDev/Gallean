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
                return new TraversalInfo(approachPath, path.Skip(i), new Encounter.StackBattle(traverseStack, canBeginBattle, CanAvoid: false));
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

            WorldStack? adjacentEnemyStack = FindAdjacentEnemyStack(state, plan, stack, position);
            if (adjacentEnemyStack is not null)
            {
                (_, _, UnitDefinition leaderDefinition) = adjacentEnemyStack.GetLeader(definitions);

                bool canAvoid = profile.IsThief;

                if (leaderDefinition.Category is UnitCategory.Illusion)
                {
                    canAvoid = false;
                }

                return new TraversalInfo(
                    path.Truncate(i + 1),
                    path.Skip(i + 1),
                    new Encounter.StackBattle(adjacentEnemyStack, canBeginBattle, canAvoid));
            }
        }

        return new TraversalInfo(path, new WorldPath([]), null);
    }

    private static WorldStack? FindAdjacentEnemyStack(WorldState state, WorldPlan plan, WorldStack stack, WorldPosition position)
    {
        foreach (WorldPosition adjacentPosition in position.GetAdjacentPositions())
        {
            WorldStack? adjacentStack = plan.GetStackAt(adjacentPosition);

            if (adjacentStack is not null && DiplomacyCalculator.IsHostile(state, stack.OwnerId, adjacentStack.OwnerId))
            {
                return adjacentStack;
            }
        }

        return null;
    }
}
