namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldSiteVisitedEvent ev, WorldState state)
    {
        WorldStack stack = state.Stacks[ev.StackId];
        WorldSite site = state.Sites[ev.SiteId];
        WorldSite updatedSite = site with { VisitorIds = site.VisitorIds.Add(stack.OwnerId) };

        return state with { Sites = state.Sites.SetItem(ev.SiteId, updatedSite) };
    }
}
