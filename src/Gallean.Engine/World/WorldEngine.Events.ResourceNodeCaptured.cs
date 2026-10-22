namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldResourceNodeCapturedEvent ev, WorldState state)
    {
        WorldResourceNode node = state.ResourceNodes[ev.NodeId];
        WorldResourceNode updatedNode = node with { OwnerId = ev.NewOwnerId };

        return state with { ResourceNodes = state.ResourceNodes.SetItem(ev.NodeId, updatedNode) };
    }
}
