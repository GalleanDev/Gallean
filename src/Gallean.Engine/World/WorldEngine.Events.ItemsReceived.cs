namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldItemsReceivedEvent ev, WorldState state)
    {
        WorldInventory inventory = WorldUnitGroupCalculator.GetInventory(state, ev.Target);

        return WorldUnitGroupCalculator.WithInventory(state, ev.Target, inventory.AddRange(ev.Items));
    }
}
