using System.Collections.Immutable;
using Gallean.Common;

namespace Gallean.Engine;

public sealed record WorldDropItemCommand(
    WorldStackId StackId,
    WorldPosition TargetPosition,
    WorldItemId ItemId) : IWorldCommand<Empty>
{
    public WorldCommandResult Execute(WorldState state, IEngineContext context)
    {
        if (!WorldCommandHelpers.TryGetCurrentPlayerStack(state, StackId, out WorldStack? stack))
        {
            return WorldCommandResult.Fail(WorldCommandError.StackNotFound);
        }

        WorldPlan plan = WorldPlan.For(state);
        WorldFortification? fortification = plan.GetFortificationAt(stack.Position);

        if (fortification is not null && FortificationCalculator.IsRioting(fortification))
        {
            return WorldCommandResult.Fail(WorldCommandError.CityRioting);
        }

        if (!stack.Inventory.TryGetItem(ItemId, out WorldItem? item))
        {
            return WorldCommandResult.Fail(WorldCommandError.ItemNotFound);
        }

        if (!state.Map.IsValidPosition(TargetPosition))
        {
            return WorldCommandResult.Fail(WorldCommandError.TargetOutOfBounds);
        }

        if (stack.Position.GetDistance(TargetPosition) > context.Globals.BagRange)
        {
            return WorldCommandResult.Fail(WorldCommandError.TargetTooFar);
        }

        WorldMovementCalculator.MovementProfile profile = WorldMovementCalculator.GetMovementProfile(stack, context.Definitions);

        if (!WorldMovementCalculator.IsPassable(state.Map, TargetPosition, profile))
        {
            return WorldCommandResult.Fail(WorldCommandError.TargetUnpassable);
        }

        WorldBag? bag = plan.GetBagAt(TargetPosition);
        ImmutableArray<WorldTomb> tombs = plan.GetTombsAt(TargetPosition);

        if (plan.HasStructureAt(TargetPosition) ||
            plan.GetResourceNodeAt(TargetPosition) is not null ||
            plan.GetRodAt(TargetPosition) is not null ||
            plan.GetStackAt(TargetPosition) is not null ||
            (bag is null && !tombs.IsEmpty))
        {
            return WorldCommandResult.Fail(WorldCommandError.TargetOccupied);
        }

        WorldBagId bagId;

        if (bag is null)
        {
            bagId = WorldCommandHelpers.GenerateBagId(state);
        }
        else
        {
            bagId = bag.Id;
        }

        return WorldCommandResult.Success(
            Empty.Value,
            [new WorldItemDroppedEvent(StackId, bagId, TargetPosition, item)]);
    }
}
