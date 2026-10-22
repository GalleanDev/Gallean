using Gallean.Common;

namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldStackCreatedEvent ev, WorldState state)
    {
        WorldPlan plan = WorldPlan.For(state);

        if (plan.GetStackAt(ev.Stack.Position) is not null)
        {
            throw new InvalidOperationException($"Position {ev.Stack.Position} is already occupied by a stack.");
        }

        WorldPlayer owner = state.Players[ev.Stack.OwnerId];

        state = state with
        {
            Stacks = state.Stacks.SetItem(ev.Stack.Id, ev.Stack),
            Players = state.Players.SetItem(
                owner.Id,
                owner with { Resources = ResourceSetCalculator.Subtract(owner.Resources, ev.Cost) })
        };

        WorldPlayer updatedOwner = state.Players[ev.Stack.OwnerId];
        ImmutableBitmap updatedFog = FogOfWarCalculator.Update(state, updatedOwner.Id, Context);

        return state with
        {
            Players = state.Players.SetItem(updatedOwner.Id, updatedOwner with { FogOfWar = updatedFog })
        };
    }
}
