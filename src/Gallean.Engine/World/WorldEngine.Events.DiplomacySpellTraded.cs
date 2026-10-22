namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldDiplomacySpellTradedEvent ev, WorldState state)
    {
        WorldPlayer buyer = state.Players[ev.BuyerId];

        WorldPlayer updatedBuyer = buyer with
        {
            Spellbook = buyer.Spellbook with { Acquired = buyer.Spellbook.Acquired.Add(ev.SpellId) }
        };

        state = state with { Players = state.Players.SetItem(buyer.Id, updatedBuyer) };

        return PayForTrade(state, ev.SellerId, ev.BuyerId, ev.Price);
    }
}
