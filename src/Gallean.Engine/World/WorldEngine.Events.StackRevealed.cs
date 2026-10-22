namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldStackRevealedEvent ev, WorldState state)
    {
        WorldStack stack = state.Stacks[ev.StackId];

        return state with { Stacks = state.Stacks.SetItem(ev.StackId, stack with { Concealed = false }) };
    }
}
