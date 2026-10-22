namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldStackPoisonedEvent ev, WorldState state)
    {
        WorldStack stack = state.Stacks[ev.StackId];
        WorldStack updatedStack = stack with { Units = ev.PoisonedUnits };

        return state with { Stacks = state.Stacks.SetItem(ev.StackId, updatedStack) };
    }
}
