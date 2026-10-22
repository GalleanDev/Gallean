namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldRodCreatedEvent ev, WorldState state)
    {
        WorldPlayer player = state.Players[ev.PlayerId];

        WorldPlayer updatedPlayer = player with
        {
            Resources = ResourceSetCalculator.Subtract(player.Resources, Context.Globals.RodCost)
        };

        WorldMapTerrain terrain = WorldHelpers.GetNativeTerrain(player.Race);
        WorldRod rod = new(ev.RodId, ev.PlayerId, ev.Position);

        WorldState plantedState = state with
        {
            Players = state.Players.SetItem(ev.PlayerId, updatedPlayer),
            Map = TerrainTransformCalculator.Transform(state.Map, state, [ev.Position], terrain),
            Rods = state.Rods.Add(ev.RodId, rod)
        };

        return plantedState with
        {
            Map = ExpansionCalculator.ExpandRodTerrain(plantedState, Context, rod, terrain)
        };
    }
}
