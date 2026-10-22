namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldSpyRemovedEvent ev, WorldState state)
    {
        WorldPlayer player = state.Players[ev.PlayerId];
        WorldPlayer updatedPlayer = player with { SpiedPlayers = player.SpiedPlayers.Remove(ev.TargetPlayerId) };

        return state with { Players = state.Players.SetItem(ev.PlayerId, updatedPlayer) };
    }
}
