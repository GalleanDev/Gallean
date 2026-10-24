namespace Gallean.Engine;

internal static class WorldAiTargetEnumerator
{
    [Flags]
    public enum TargetType
    {
        None = 0,
        EnemyStack = 1 << 0,
        EnemyFortification = 1 << 1,
        Ruin = 1 << 2,
        Bag = 1 << 3,
        Enemy = EnemyStack | EnemyFortification
    }

    public readonly record struct TargetArea(WorldPosition Center, int Radius);

    public static IEnumerable<WorldAiTarget> Enumerate(WorldState state, WorldPlayerId playerId, TargetType types, TargetArea? area)
    {
        WorldPlayer player = state.Players[playerId];

        if (types.HasFlag(TargetType.EnemyStack))
        {
            foreach (WorldAiTarget target in EnumerateEnemyStacks(state, player, area))
            {
                yield return target;
            }
        }

        if (types.HasFlag(TargetType.EnemyFortification))
        {
            foreach (WorldAiTarget target in EnumerateEnemyFortifications(state, player, area))
            {
                yield return target;
            }
        }

        if (types.HasFlag(TargetType.Ruin))
        {
            foreach (WorldAiTarget target in EnumerateRuins(state, player, area))
            {
                yield return target;
            }
        }

        if (types.HasFlag(TargetType.Bag))
        {
            foreach (WorldAiTarget target in EnumerateBags(state, player, area))
            {
                yield return target;
            }
        }
    }

    public static IEnumerable<WorldAiTarget> EnumerateFreeTiles(WorldState state, WorldStack stack, TargetArea area, IGameDefinitionProvider definitions)
    {
        WorldPlan plan = WorldPlan.For(state);
        WorldMovementCalculator.MovementProfile profile = WorldMovementCalculator.GetMovementProfile(stack, definitions);

        foreach (WorldPosition position in ScenarioLocationCalculator.EnumerateTiles(area.Center, area.Radius, state.Map))
        {
            if (plan.CanPlaceStackAt(position, profile))
            {
                yield return new WorldAiTarget.Location(position);
            }
        }
    }

    private static IEnumerable<WorldAiTarget> EnumerateEnemyStacks(WorldState state, WorldPlayer player, TargetArea? area)
    {
        foreach (WorldStack stack in state.Stacks.Values)
        {
            if (!stack.Concealed &&
                DiplomacyCalculator.IsHostile(state, player.Id, stack.OwnerId) &&
                IsExplored(player, stack.Position) &&
                IsWithinArea(stack.Position, area))
            {
                yield return new WorldAiTarget.Stack(stack.Id);
            }
        }
    }

    private static IEnumerable<WorldAiTarget> EnumerateEnemyFortifications(WorldState state, WorldPlayer player, TargetArea? area)
    {
        foreach (WorldFortification fortification in state.Fortifications.Values)
        {
            WorldPlayerId? fortificationOwner = FortificationCalculator.GetOwner(fortification);

            if (fortificationOwner is not null &&
                DiplomacyCalculator.IsHostile(state, player.Id, fortificationOwner.Value) &&
                IsExplored(player, fortification.Entrance) &&
                IsWithinArea(fortification.Entrance, area))
            {
                yield return new WorldAiTarget.Fortification(fortification.Id);
            }
        }
    }

    private static IEnumerable<WorldAiTarget> EnumerateRuins(WorldState state, WorldPlayer player, TargetArea? area)
    {
        foreach (WorldFortification fortification in state.Fortifications.Values)
        {
            if (fortification is WorldRuin { LooterId: null } &&
                IsExplored(player, fortification.Entrance) &&
                IsWithinArea(fortification.Entrance, area))
            {
                yield return new WorldAiTarget.Fortification(fortification.Id);
            }
        }
    }

    private static IEnumerable<WorldAiTarget> EnumerateBags(WorldState state, WorldPlayer player, TargetArea? area)
    {
        foreach (WorldBag bag in state.Bags.Values)
        {
            if (IsExplored(player, bag.Position) && IsWithinArea(bag.Position, area))
            {
                yield return new WorldAiTarget.Bag(bag.Id);
            }
        }
    }

    private static bool IsExplored(WorldPlayer player, WorldPosition position)
    {
        return player.Race == WorldRace.Neutral || player.FogOfWar.Get(position.X, position.Y);
    }

    private static bool IsWithinArea(WorldPosition position, TargetArea? area)
    {
        return area is null || position.GetDistance(area.Value.Center) <= area.Value.Radius;
    }
}
