namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldSpellsReceivedEvent ev, WorldState state)
    {
        WorldPlayer player = state.Players[ev.PlayerId];

        WorldPlayer updatedPlayer = player with
        {
            Spellbook = player.Spellbook with { Acquired = player.Spellbook.Acquired.Union(ev.Spells) }
        };

        return state with { Players = state.Players.SetItem(ev.PlayerId, updatedPlayer) };
    }
}
