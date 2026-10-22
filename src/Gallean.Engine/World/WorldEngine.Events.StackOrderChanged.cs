using System.Collections.Immutable;

namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldStackOrderChangedEvent ev, WorldState state)
    {
        WorldStack stack = state.Stacks[ev.StackId];
        ImmutableSortedDictionary<WorldPlayerId, WorldAiMemory> memories = state.AiMemories;

        foreach ((WorldPlayerId playerId, WorldAiMemory memory) in state.AiMemories)
        {
            if (memory.Stacks.ContainsKey(ev.StackId))
            {
                memories = memories.SetItem(playerId, memory with { Stacks = memory.Stacks.Remove(ev.StackId) });
            }
        }

        return state with
        {
            Stacks = state.Stacks.SetItem(ev.StackId, stack with { Order = ev.Order }),
            AiMemories = memories
        };
    }
}
