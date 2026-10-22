namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldInventoryItemTransferredEvent ev, WorldState state)
    {
        WorldInventory sourceInventory = WorldUnitGroupCalculator.GetInventory(state, ev.Source);
        WorldInventory targetInventory = WorldUnitGroupCalculator.GetInventory(state, ev.Target);

        WorldInventory updatedSourceInventory = sourceInventory.Remove(ev.Item);
        WorldInventory updatedTargetInventory = targetInventory.Add(ev.Item);

        state = WorldUnitGroupCalculator.WithInventory(state, ev.Source, updatedSourceInventory);
        state = WorldUnitGroupCalculator.WithInventory(state, ev.Target, updatedTargetInventory);

        return state;
    }
}
