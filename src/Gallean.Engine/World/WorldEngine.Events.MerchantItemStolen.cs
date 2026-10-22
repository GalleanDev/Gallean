namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldMerchantItemStolenEvent ev, WorldState state)
    {
        WorldStack stack = state.Stacks[ev.StackId];
        WorldMerchantSite site = (WorldMerchantSite)state.Sites[ev.SiteId];

        WorldStack updatedStack = stack with { Inventory = stack.Inventory.Add(ev.Item) };
        WorldMerchantSite updatedSite = site.Take(ev.Item.DefinitionId);

        return state with
        {
            Stacks = state.Stacks.SetItem(updatedStack.Id, updatedStack),
            Sites = state.Sites.SetItem(updatedSite.Id, updatedSite)
        };
    }
}
