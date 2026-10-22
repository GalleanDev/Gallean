namespace Gallean.Engine.Tests;

internal static class TestWorldStateValidator
{
    public static void Validate(WorldState state, IGameDefinitionProvider definitions)
    {
        ValidateOwners(state);
        ValidatePositions(state);
        ValidateLeaders(state, definitions);
        ValidateUnitIds(state);
    }

    private static void ValidateOwners(WorldState state)
    {
        foreach (WorldStack stack in state.Stacks.Values)
        {
            RequireOwner(state, stack.OwnerId, $"Stack '{stack.Id}'");
        }

        foreach (WorldFortification fortification in state.Fortifications.Values)
        {
            if (fortification is WorldCity city)
            {
                RequireOwner(state, city.OwnerId, $"City '{city.Id.Value}'");
            }
            else if (fortification is WorldCapital capital)
            {
                RequireOwner(state, capital.OwnerId, $"Capital '{capital.Id.Value}'");
            }
        }

        foreach (WorldResourceNode resourceNode in state.ResourceNodes.Values)
        {
            RequireOwner(state, resourceNode.OwnerId, $"Resource node '{resourceNode.Id.Value}'");
        }
    }

    private static void ValidatePositions(WorldState state)
    {
        Dictionary<WorldPosition, string> stackPositions = [];
        Dictionary<WorldPosition, string> structurePositions = [];

        foreach (WorldStack stack in state.Stacks.Values)
        {
            CheckMapPosition(state, stack.Position, $"Stack '{stack.Id}'");
            AddUnique(stackPositions, stack.Position, $"Stack '{stack.Id}'");
        }

        foreach (WorldFortification fortification in state.Fortifications.Values)
        {
            CheckMapFootprint(state, fortification.Footprint, $"Fortification '{fortification.Id.Value}'");

            if (fortification is not WorldRuin)
            {
                AddUnique(structurePositions, fortification.Entrance, $"Fortification '{fortification.Id.Value}'");
            }
        }

        foreach (WorldResourceNode resourceNode in state.ResourceNodes.Values)
        {
            CheckMapPosition(state, resourceNode.Position, $"Resource node '{resourceNode.Id.Value}'");
            AddUnique(structurePositions, resourceNode.Position, $"Resource node '{resourceNode.Id.Value}'");

            if (stackPositions.TryGetValue(resourceNode.Position, out string? stackDescription))
            {
                Assert.Fail(
                    $"{stackDescription} occupies the same position ({resourceNode.Position.X}, {resourceNode.Position.Y}) as resource node '{resourceNode.Id.Value}'.");
            }
        }
    }

    private static void ValidateLeaders(WorldState state, IGameDefinitionProvider definitions)
    {
        foreach (WorldStack stack in state.Stacks.Values)
        {
            _ = stack.GetLeader(definitions);
        }
    }

    private static void ValidateUnitIds(WorldState state)
    {
        Dictionary<WorldUnitId, string> unitIds = [];

        foreach (WorldStack stack in state.Stacks.Values)
        {
            foreach ((WorldUnit unit, GridPosition _) in stack.Units.GetUnits())
            {
                AddUniqueUnitId(unitIds, unit.Id, $"Stack '{stack.Id}'");
            }
        }

        foreach (WorldFortification fortification in state.Fortifications.Values)
        {
            foreach ((WorldUnit unit, GridPosition _) in fortification.Garrison.GetUnits())
            {
                AddUniqueUnitId(unitIds, unit.Id, $"Fortification '{fortification.Id.Value}'");
            }
        }
    }

    private static void AddUniqueUnitId(Dictionary<WorldUnitId, string> unitIds, WorldUnitId id, string description)
    {
        if (!unitIds.TryAdd(id, description))
        {
            Assert.Fail($"{description} holds unit '{id}' already held by {unitIds[id]}.");
        }
    }

    private static void AddUnique(Dictionary<WorldPosition, string> occupied, WorldPosition position, string description)
    {
        if (!occupied.TryAdd(position, description))
        {
            Assert.Fail(
                $"{description} occupies position ({position.X}, {position.Y}) already occupied by {occupied[position]}.");
        }
    }

    private static void CheckMapPosition(WorldState state, WorldPosition position, string description)
    {
        if (!state.Map.IsValidPosition(position))
        {
            Assert.Fail(
                $"{description} position ({position.X}, {position.Y}) is outside the map.");
        }
    }

    private static void CheckMapFootprint(WorldState state, WorldFootprint footprint, string description)
    {
        foreach (WorldPosition tile in footprint.EnumerateTiles())
        {
            if (!state.Map.IsValidPosition(tile))
            {
                Assert.Fail(
                    $"{description} footprint tile ({tile.X}, {tile.Y}) is outside the map.");
            }
        }
    }

    private static void RequireOwner(WorldState state, WorldPlayerId ownerId, string description)
    {
        if (!state.Players.ContainsKey(ownerId))
        {
            Assert.Fail($"{description} is owned by missing player {ownerId.Value}.");
        }
    }
}
