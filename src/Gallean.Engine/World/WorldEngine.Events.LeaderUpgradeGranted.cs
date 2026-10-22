namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldLeaderUpgradeGrantedEvent ev, WorldState state)
    {
        WorldStack stack = state.Stacks[ev.StackId];

        return state with
        {
            Stacks = state.Stacks.SetItem(
                ev.StackId,
                stack with { PendingLeaderUpgrades = stack.PendingLeaderUpgrades + 1 })
        };
    }
}
