namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldSpyAddedEvent ev, WorldState state)
    {
        WorldPlayer player = state.Players[ev.PlayerId];
        WorldPlayer updatedPlayer = player with { SpiedPlayers = player.SpiedPlayers.Add(ev.TargetPlayerId) };

        return state with { Players = state.Players.SetItem(ev.PlayerId, updatedPlayer) };
    }
}
