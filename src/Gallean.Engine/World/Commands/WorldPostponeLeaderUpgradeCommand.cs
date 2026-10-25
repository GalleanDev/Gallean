using Gallean.Common;

namespace Gallean.Engine;

public sealed record WorldPostponeLeaderUpgradeCommand(WorldStackId StackId) : IWorldCommand<Empty>
{
    public WorldCommandResult Execute(WorldState state, IEngineContext context)
    {
        if (!WorldCommandHelpers.TryGetCurrentPlayerStack(state, StackId, out WorldStack? stack))
        {
            return WorldCommandResult.Fail(WorldCommandError.StackNotFound);
        }

        if (stack.PendingLeaderUpgrades <= 0)
        {
            return WorldCommandResult.Fail(WorldCommandError.LeaderUpgradeNotPending);
        }

        return WorldCommandResult.Success(Empty.Value, [new WorldLeaderUpgradePostponedEvent(StackId)]);
    }
}
