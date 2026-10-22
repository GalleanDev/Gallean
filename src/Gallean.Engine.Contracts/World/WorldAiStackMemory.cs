using Gallean.Common;

namespace Gallean.Engine.Contracts;

public sealed record WorldAiStackMemory(WorldStackOrder Order, WorldAiTarget? Target)
{
    public static WorldAiStackMemory Empty { get; } = new(WorldStackOrder.Normal.Instance, null);
}
