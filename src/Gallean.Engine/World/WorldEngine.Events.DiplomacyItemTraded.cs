namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldDiplomacyItemTradedEvent ev, WorldState state)
    {
        WorldPlan plan = WorldPlan.For(state);
        WorldCapital? capital = plan.GetCapital(ev.BuyerId);

        if (capital is null)
        {
            throw new InvalidOperationException($"Unexpected state of player '{ev.BuyerId}'.");
        }

        WorldFortification source = state.Fortifications[ev.SourceFortificationId];
        state = WorldUnitGroupCalculator.WithInventory(state, new WorldUnitGroup.Fortification(source.Id), source.Inventory.Remove(ev.Item));
        state = WorldUnitGroupCalculator.WithInventory(state, new WorldUnitGroup.Fortification(capital.Id), capital.Inventory.Add(ev.Item));

        return PayForTrade(state, ev.SellerId, ev.BuyerId, ev.Price);
    }

    private WorldState PayForTrade(WorldState state, WorldPlayerId sellerId, WorldPlayerId buyerId, int price)
    {
        WorldPlayer seller = state.Players[sellerId];
        WorldPlayer buyer = state.Players[buyerId];
        WorldResourceSet gold = new(price, 0, 0, 0, 0, 0);

        WorldResourceSet sellerResources = seller.Resources;
        sellerResources = ResourceSetCalculator.Add(sellerResources, gold);
        sellerResources = ResourceSetCalculator.Clamp(sellerResources, Context.Globals.MaxResourceAmount);

        WorldResourceSet buyerResources = buyer.Resources;
        buyerResources = ResourceSetCalculator.Subtract(buyerResources, gold);

        return state with
        {
            Players = state.Players
                .SetItem(sellerId, seller with { Resources = sellerResources })
                .SetItem(buyerId, buyer with { Resources = buyerResources })
        };
    }
}
