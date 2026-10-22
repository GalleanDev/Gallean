namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldStackUnitTrainedEvent ev, WorldState state)
    {
        WorldStack stack = state.Stacks[ev.StackId];
        WorldPlayer player = state.Players[stack.OwnerId];
        WorldUnit target = stack.Units.GetUnit(ev.UnitId);

        UnitGrid<WorldUnit> updatedUnits = stack.Units.Replace(target, target with { Experience = ev.Experience });
        WorldStack updatedStack = stack with { Units = updatedUnits };
        WorldPlayer updatedPlayer = player with { Resources = ResourceSetCalculator.Subtract(player.Resources, ev.Cost) };

        return state with
        {
            Stacks = state.Stacks.SetItem(ev.StackId, updatedStack),
            Players = state.Players.SetItem(updatedPlayer.Id, updatedPlayer)
        };
    }
}
