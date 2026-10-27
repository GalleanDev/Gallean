using System.Collections.Immutable;
using Gallean.Common;

namespace Gallean.Engine;

public sealed record WorldTransferInventoryCommand(
    WorldUnitGroup Source,
    WorldUnitGroup Target) : IWorldCommand<Empty>
{
    public WorldCommandResult Execute(WorldState state, IEngineContext context)
    {
        if (!InventoryTransferCalculator.TryTransferInventory(
            state,
            Source,
            Target,
            out ImmutableArray<WorldItem> items,
            out WorldCommandError? error))
        {
            return WorldCommandResult.Fail(error.Value);
        }

        List<WorldEvent> events = [];

        foreach (WorldItem item in items)
        {
            events.Add(new WorldInventoryItemTransferredEvent(Source, Target, item));
        }

        return WorldCommandResult.Success(Empty.Value, events);
    }
}
