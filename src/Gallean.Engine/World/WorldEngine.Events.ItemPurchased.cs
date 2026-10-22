namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldItemPurchasedEvent ev, WorldState state)
    {
        WorldStack stack = state.Stacks[ev.StackId];
        WorldMerchantSite site = (WorldMerchantSite)state.Sites[ev.SiteId];
        WorldPlayer player = state.Players[stack.OwnerId];

        WorldResourceSet updatedResources = ResourceSetCalculator.Subtract(player.Resources, ev.Cost);

        WorldStack updatedStack = stack with { Inventory = stack.Inventory.Add(ev.Item) };
        WorldMerchantSite updatedSite = site.Take(ev.Item.DefinitionId);

        return state with
        {
            Players = state.Players.SetItem(player.Id, player with { Resources = updatedResources }),
            Stacks = state.Stacks.SetItem(updatedStack.Id, updatedStack),
            Sites = state.Sites.SetItem(updatedSite.Id, updatedSite)
        };
    }
}
