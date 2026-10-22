using Gallean.Common;

namespace Gallean.Engine;

public sealed record WorldSelectLeaderUpgradeCommand(
    WorldStackId StackId,
    UnitModifierDefinitionId ModifierId) : IWorldCommand<Empty>
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

        (WorldUnit leader, GridPosition _, UnitDefinition _) = stack.GetLeader(context.Definitions);

        IReadOnlyList<LeaderUpgradeDefinition> available = LeaderUpgradeCalculator.GetAvailableUpgrades(
            state.Players[stack.OwnerId],
            leader.Level,
            leader.Modifiers,
            context.Definitions);

        bool isAvailable = false;

        foreach (LeaderUpgradeDefinition upgrade in available)
        {
            if (upgrade.ModifierId == ModifierId)
            {
                isAvailable = true;
                break;
            }
        }

        if (!isAvailable)
        {
            return WorldCommandResult.Fail(WorldCommandError.LeaderUpgradeNotAvailable);
        }

        return WorldCommandResult.Success(
            Empty.Value,
            [
                new WorldLeaderUpgradeSelectedEvent(StackId, ModifierId),
                new WorldUnitModifierAppliedEvent(
                    new WorldUnitGroup.Stack(StackId),
                    leader.Id,
                    UnitModifier.Permanent(ModifierId))
            ]);
    }
}
