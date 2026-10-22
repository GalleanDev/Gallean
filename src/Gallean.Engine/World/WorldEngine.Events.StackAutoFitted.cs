namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldStackAutoFittedEvent ev, WorldState state)
    {
        WorldStack stack = state.Stacks[ev.StackId];
        WorldPlan plan = WorldPlan.For(state);
        WorldFortification? fortification = plan.GetFortificationAt(stack.Position);

        if (fortification is not (WorldCity or WorldCapital))
        {
            throw new InvalidOperationException($"Stack '{stack.Id}' is not located at a garrison.");
        }

        WorldStackBuilder stackBuilder = new(Context.Definitions);

        foreach ((WorldUnit unit, GridPosition position) in ev.StackUnits.GetUnits())
        {
            stackBuilder.PlaceUnit(unit, position);
        }

        state = state with
        {
            Stacks = state.Stacks.SetItem(stack.Id, stack with
            {
                Units = stackBuilder.Build(),
                Inventory = ev.StackInventory,
                Equipment = ev.Equipment
            })
        };

        return state with
        {
            Fortifications = state.Fortifications.SetItem(fortification.Id, fortification with
            {
                Garrison = ev.GarrisonUnits,
                Inventory = ev.GarrisonInventory
            })
        };
    }
}
