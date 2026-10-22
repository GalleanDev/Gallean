using Gallean.Common;

namespace Gallean.Engine;

public sealed record WorldPickUpBagCommand(
    WorldStackId StackId,
    WorldBagId BagId) : IWorldCommand<Empty>
{
    public WorldCommandResult Execute(WorldState state, IEngineContext context)
    {
        if (!WorldCommandHelpers.TryGetCurrentPlayerStack(state, StackId, out WorldStack? stack))
        {
            return WorldCommandResult.Fail(WorldCommandError.StackNotFound);
        }

        if (!state.Bags.TryGetValue(BagId, out WorldBag? bag))
        {
            return WorldCommandResult.Fail(WorldCommandError.BagNotFound);
        }

        if (stack.Position.GetDistance(bag.Position) > context.Globals.BagRange)
        {
            return WorldCommandResult.Fail(WorldCommandError.TargetTooFar);
        }

        return WorldCommandResult.Success(
            Empty.Value,
            [new WorldBagPickedUpEvent(StackId, BagId, bag.Inventory)]);
    }
}
