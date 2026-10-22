namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldSpellResearchedEvent ev, WorldState state)
    {
        WorldPlayer player = state.Players[ev.PlayerId];

        WorldPlayer updatedPlayer = player with
        {
            Resources = ResourceSetCalculator.Subtract(player.Resources, ev.Cost),
            Spellbook = player.Spellbook with { Acquired = player.Spellbook.Acquired.Add(ev.SpellId) },
            TurnActions = player.TurnActions with { SpellResearched = true }
        };

        return state with { Players = state.Players.SetItem(ev.PlayerId, updatedPlayer) };
    }
}
