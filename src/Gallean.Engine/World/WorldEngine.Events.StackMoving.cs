namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldStackMovingEvent ev, WorldState state)
    {
        WorldStack stack = state.Stacks[ev.StackId];
        WorldStack updatedStack = stack with
        {
            Position = ev.TargetPosition,
            Direction = ev.NewDirection,
            MovementPoints = ev.NewMovementPoints
        };

        WorldState updatedState = state with { Stacks = state.Stacks.SetItem(ev.StackId, updatedStack) };

        return FortificationCalculator.UpdateUnitModifiers(updatedState, Context.Definitions);
    }
}
