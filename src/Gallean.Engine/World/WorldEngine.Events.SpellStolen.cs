namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldSpellStolenEvent ev, WorldState state)
    {
        WorldPlayerId playerId = state.Stacks[ev.StackId].OwnerId;
        WorldPlayer player = state.Players[playerId];

        WorldPlayer updatedPlayer = player with
        {
            Spellbook = player.Spellbook with { Acquired = player.Spellbook.Acquired.Add(ev.SpellId) }
        };

        return state with { Players = state.Players.SetItem(playerId, updatedPlayer) };
    }
}
