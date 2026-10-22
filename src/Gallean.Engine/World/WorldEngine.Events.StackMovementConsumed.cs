using System.Collections.Immutable;

namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldStackMovementConsumedEvent ev, WorldState state)
    {
        WorldStack stack = state.Stacks[ev.StackId];

        ImmutableSortedDictionary<WorldStackId, WorldStack> updatedStacks = state.Stacks.SetItem(
            ev.StackId,
            stack with
            {
                MovementPoints = ev.NewMovementPoints
            }
        );

        return state with
        {
            Stacks = updatedStacks
        };
    }
}
