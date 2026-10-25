using System.Collections.Immutable;
using Gallean.Common;
using CommandResult = Gallean.Engine.ICommandResult<Gallean.Engine.WorldCommandError, Gallean.Engine.WorldEvent, Gallean.Engine.WorldGetLeaderUpgradeInfoCommand.Result>;

namespace Gallean.Engine;

public sealed record WorldGetLeaderUpgradeInfoCommand(WorldStackId StackId) : IWorldCommand<WorldGetLeaderUpgradeInfoCommand.Result>
{
    public sealed record Upgrade(
        LeaderUpgradeDefinition Definition,
        bool IsPreferred,
        WorldCommandError? Error);

    public sealed record Result(ImmutableArray<Upgrade> Upgrades)
    {
        public bool Equals(Result? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Upgrades.SequenceEqual(other.Upgrades);
        }

        public override int GetHashCode()
        {
            HashCode hash = new();

            hash.AddSequence(Upgrades);

            return hash.ToHashCode();
        }
    }

    public CommandResult Execute(WorldState state, IEngineContext context)
    {
        if (!WorldCommandHelpers.TryGetCurrentPlayerStack(state, StackId, out WorldStack? stack))
        {
            return CommandResult.Fail(WorldCommandError.StackNotFound);
        }

        (WorldUnit leader, GridPosition _, UnitDefinition _) = stack.GetLeader(context.Definitions);

        IReadOnlyList<LeaderUpgradeDefinition> availableUpgrades = LeaderUpgradeCalculator.GetAvailableUpgrades(
            state.Players[stack.OwnerId],
            leader.Level,
            leader.Modifiers,
            context.Definitions);

        LeaderUpgradeDefinition? preferredUpgrade = LeaderUpgradeCalculator.GetPreferredUpgrade(availableUpgrades);

        ImmutableArray<Upgrade>.Builder result = ImmutableArray.CreateBuilder<Upgrade>();

        foreach (LeaderUpgradeDefinition upgrade in availableUpgrades)
        {
            WorldSelectLeaderUpgradeCommand command = new(StackId, upgrade.ModifierId);
            WorldCommandResult commandResult = command.Execute(state, context);

            result.Add(new Upgrade(upgrade, upgrade == preferredUpgrade, commandResult.Error));
        }

        return CommandResult.Success(new Result(result.ToImmutable()), []);
    }
}
