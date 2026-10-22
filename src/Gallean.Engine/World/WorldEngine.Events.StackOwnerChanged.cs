using System.Collections.Immutable;
using Gallean.Common;

namespace Gallean.Engine;

internal partial class WorldEngine
{
    private static WorldCanonicalStackOrder GetOwnerChangedOrder(WorldPlayer owner)
    {
        if (WorldHelpers.IsRacePlayable(owner.Race))
        {
            return WorldStackOrder.Normal.Instance;
        }
        else
        {
            return WorldStackOrder.Stand.Instance;
        }
    }

    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldStackOwnerChangedEvent ev, WorldState state)
    {
        WorldStack stack = state.Stacks[ev.StackId];
        WorldStack updatedStack = stack with
        {
            OwnerId = ev.OwnerId,
            SubraceId = ev.SubraceId
        };

        ImmutableSortedDictionary<WorldPlayerId, WorldAiMemory> memories = state.AiMemories;

        if (stack.OwnerId != ev.OwnerId)
        {
            updatedStack = updatedStack with { Order = GetOwnerChangedOrder(state.Players[ev.OwnerId]) };

            foreach ((WorldPlayerId playerId, WorldAiMemory memory) in state.AiMemories)
            {
                if (memory.Stacks.ContainsKey(ev.StackId))
                {
                    memories = memories.SetItem(playerId, memory with { Stacks = memory.Stacks.Remove(ev.StackId) });
                }
            }
        }

        state = state with
        {
            Stacks = state.Stacks.SetItem(ev.StackId, updatedStack),
            AiMemories = memories
        };

        WorldPlayer owner = state.Players[ev.OwnerId];
        ImmutableBitmap updatedFog = FogOfWarCalculator.Update(state, owner.Id, Context);

        return state with
        {
            Players = state.Players.SetItem(owner.Id, owner with { FogOfWar = updatedFog })
        };
    }
}
