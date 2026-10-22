using System.Collections.Immutable;

namespace Gallean.Engine;

internal static class ScenarioMountainRemovalCalculator
{
    public static ImmutableArray<WorldMountainId> GetMountains(WorldState state, ScenarioLocationDefinition location)
    {
        ImmutableArray<WorldMountainId>.Builder result = ImmutableArray.CreateBuilder<WorldMountainId>();

        foreach (WorldMountain mountain in state.Mountains.Values)
        {
            if (mountain.Footprint.EnumerateTiles().Any(tile => ScenarioLocationCalculator.Contains(location, tile)))
            {
                result.Add(mountain.Id);
            }
        }

        return result.ToImmutable();
    }

    public static WorldState Remove(
        WorldState state,
        ScenarioLocationDefinition location,
        ImmutableArray<WorldMountainId> mountainIds)
    {
        HashSet<WorldPosition> clearedTiles = [];

        foreach (WorldPosition tile in ScenarioLocationCalculator.EnumerateTiles(location, state.Map))
        {
            clearedTiles.Add(tile);
        }

        foreach (WorldMountainId mountainId in mountainIds)
        {
            if (state.Mountains.TryGetValue(mountainId, out WorldMountain? mountain))
            {
                foreach (WorldPosition tile in mountain.Footprint.EnumerateTiles())
                {
                    if (state.Map.IsValidPosition(tile))
                    {
                        clearedTiles.Add(tile);
                    }
                }
            }
        }

        WorldMap map = state.Map.UpdateCells(cells =>
        {
            foreach (WorldPosition tile in clearedTiles)
            {
                int index = tile.Y * state.Map.Width + tile.X;
                WorldMapCell cell = cells[index];

                if (cell.Feature == WorldMapFeature.Mountain)
                {
                    cells[index] = cell with
                    {
                        Feature = WorldMapFeature.None,
                        FeatureVariant = default
                    };
                }
            }
        });

        return state with
        {
            Map = map,
            Mountains = state.Mountains.RemoveRange(mountainIds)
        };
    }
}
