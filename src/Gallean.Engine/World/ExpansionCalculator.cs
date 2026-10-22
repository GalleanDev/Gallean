namespace Gallean.Engine;

internal static class ExpansionCalculator
{
    private readonly record struct ExpansionSource(WorldFortificationId? FortificationId, WorldRodId? RodId);

    private static readonly (int X, int Y)[] Directions = [(0, 1), (0, -1), (1, 0), (-1, 0)];

    public static WorldMap ExpandPlayerTerrain(WorldState state, WorldPlayerId playerId, IEngineContext engine)
    {
        WorldPlayer player = state.Players[playerId];

        if (!WorldHelpers.IsRacePlayable(player.Race))
        {
            return state.Map;
        }

        WorldMapTerrain nativeTerrain = WorldHelpers.GetNativeTerrain(player.Race);
        WorldMap map = state.Map;

        foreach (WorldFortification fortification in state.Fortifications.Values)
        {
            if (FortificationCalculator.GetOwner(fortification) == playerId)
            {
                int growth = FortificationCalculator.GetTerrainGrowth(fortification, engine.Globals);

                map = ExpandAround(
                    map,
                    state,
                    engine,
                    new ExpansionSource(fortification.Id, null),
                    FortificationCalculator.GetCenter(fortification.Footprint),
                    nativeTerrain,
                    growth);
            }
        }

        foreach (WorldRod rod in state.Rods.Values)
        {
            if (rod.OwnerId == playerId)
            {
                map = ExpandAround(
                    map,
                    state,
                    engine,
                    new ExpansionSource(null, rod.Id),
                    rod.Position,
                    nativeTerrain,
                    engine.Globals.RodTerrainTilesPerTurn);
            }
        }

        return map;
    }

    public static WorldMap ExpandFortificationTerrain(
        WorldState state,
        IEngineContext engine,
        WorldFortification fortification,
        WorldMapTerrain terrain,
        int tileBudget)
    {
        return ExpandAround(
            state.Map,
            state,
            engine,
            new ExpansionSource(fortification.Id, null),
            FortificationCalculator.GetCenter(fortification.Footprint),
            terrain,
            tileBudget);
    }

    public static WorldMap ExpandRodTerrain(WorldState state, IEngineContext engine, WorldRod rod, WorldMapTerrain terrain)
    {
        return ExpandAround(
            state.Map,
            state,
            engine,
            new ExpansionSource(null, rod.Id),
            rod.Position,
            terrain,
            engine.Globals.RodTerrainTilesPerTurn);
    }

    private static WorldMap ExpandAround(
        WorldMap map,
        WorldState state,
        IEngineContext engine,
        ExpansionSource source,
        WorldPosition anchor,
        WorldMapTerrain terrain,
        int cellLimit)
    {
        bool[] closedCells = BuildBlockedCells(map, state, engine, source);
        Queue<WorldPosition> queue = new();
        List<WorldPosition> positions = [];

        closedCells[GetIndex(map, anchor)] = true;
        EnqueueNeighbors(anchor);

        while (queue.Count > 0)
        {
            WorldPosition position = queue.Dequeue();

            if (map.GetCellAt(position).Terrain != terrain)
            {
                if (positions.Count < cellLimit)
                {
                    positions.Add(position);
                }
                else
                {
                    break;
                }
            }

            EnqueueNeighbors(position);
        }

        return TerrainTransformCalculator.Transform(map, state, positions, terrain);

        void EnqueueNeighbors(WorldPosition position)
        {
            foreach ((int offsetX, int offsetY) in Directions)
            {
                WorldPosition neighbor = new(position.X + offsetX, position.Y + offsetY);

                if (map.IsValidPosition(neighbor))
                {
                    int index = GetIndex(map, neighbor);

                    if (!closedCells[index])
                    {
                        closedCells[index] = true;
                        queue.Enqueue(neighbor);
                    }
                }
            }
        }
    }

    private static bool[] BuildBlockedCells(WorldMap map, WorldState state, IEngineContext engine, ExpansionSource source)
    {
        bool[] cells = new bool[map.Width * map.Height];

        for (int index = 0; index < cells.Length; index++)
        {
            if (!TerrainTransformCalculator.IsTransformable(map.Cells[index]))
            {
                cells[index] = true;
            }
        }

        foreach (WorldRod rod in state.Rods.Values)
        {
            if (rod.Id != source.RodId)
            {
                BlockCell(rod.Position);
            }
        }

        foreach (WorldResourceNode node in state.ResourceNodes.Values)
        {
            BlockCell(node.Position);

            if (HasBlockingRod(state, source, node))
            {
                foreach (WorldPosition position in node.Position.GetAdjacentPositions())
                {
                    BlockCell(position);
                }
            }
        }

        foreach (WorldFortification fortification in state.Fortifications.Values)
        {
            if (fortification.Id != source.FortificationId && HasPlayableOwner(state, fortification))
            {
                foreach (WorldPosition position in fortification.Footprint.EnumerateTiles())
                {
                    BlockCell(position);
                }
            }
        }

        foreach (WorldLandmark landmark in state.Landmarks.Values)
        {
            if (engine.Definitions.GetLandmark(landmark.DefinitionId).Mountain)
            {
                foreach (WorldPosition position in landmark.Footprint.EnumerateTiles())
                {
                    BlockCell(position);
                }
            }
        }

        return cells;

        void BlockCell(WorldPosition position)
        {
            if (map.IsValidPosition(position))
            {
                cells[GetIndex(map, position)] = true;
            }
        }
    }

    private static bool HasBlockingRod(WorldState state, ExpansionSource source, WorldResourceNode node)
    {
        foreach (WorldRod rod in state.Rods.Values)
        {
            if (rod.Id != source.RodId && rod.Position.GetDistance(node.Position) <= 1)
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasPlayableOwner(WorldState state, WorldFortification fortification)
    {
        WorldPlayerId? ownerId = FortificationCalculator.GetOwner(fortification);

        if (ownerId is not null)
        {
            return WorldHelpers.IsRacePlayable(state.Players[ownerId.Value].Race);
        }
        else
        {
            return false;
        }
    }

    private static int GetIndex(WorldMap map, WorldPosition position)
    {
        return position.Y * map.Width + position.X;
    }
}
