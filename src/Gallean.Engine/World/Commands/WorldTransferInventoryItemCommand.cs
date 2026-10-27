using Gallean.Common;

namespace Gallean.Engine;

public sealed record WorldTransferInventoryItemCommand(
    WorldUnitGroup Source,
    WorldUnitGroup Target,
    WorldItemId ItemId) : IWorldCommand<Empty>
{
    public WorldCommandResult Execute(WorldState state, IEngineContext context)
    {
        if (!InventoryTransferCalculator.TryTransferItem(
            state,
            Source,
            Target,
            ItemId,
            out WorldItem? item,
            out WorldCommandError? error))
        {
            return WorldCommandResult.Fail(error.Value);
        }

        return WorldCommandResult.Success(
            Empty.Value,
            [new WorldInventoryItemTransferredEvent(Source, Target, item)]);
    }
}
