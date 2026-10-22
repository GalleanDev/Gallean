using Gallean.Common;

namespace Gallean.Engine;

public readonly record struct WorldAiActionPlan(WorldAiActionScore Score, IWorldCommand<Empty> Command, WorldAiMemory Memory)
{
    public static WorldAiActionPlan EndTurn(WorldAiMemory memory)
    {
        return new WorldAiActionPlan(default, new WorldEndTurnCommand(), memory);
    }
}
