namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldMapFeatureChangedEvent ev, WorldState state)
    {
        int width = state.Map.Width;

        WorldMap map = state.Map.UpdateCells(cells =>
        {
            foreach (WorldPosition position in ev.Positions)
            {
                int index = position.Y * width + position.X;

                cells[index] = cells[index] with { Feature = ev.Feature };
            }
        });

        return state with { Map = map };
    }
}
