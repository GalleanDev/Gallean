namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldRuinSpiedEvent ev, WorldState state)
    {
        WorldPlayer player = state.Players[ev.PlayerId];
        WorldPlayer updatedPlayer = player with { SpiedRuins = player.SpiedRuins.Add(ev.RuinId) };

        return state with { Players = state.Players.SetItem(ev.PlayerId, updatedPlayer) };
    }
}
