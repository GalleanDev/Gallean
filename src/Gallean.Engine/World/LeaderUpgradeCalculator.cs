using System.Collections.Immutable;

namespace Gallean.Engine;

internal static class LeaderUpgradeCalculator
{
    public static IEnumerable<WorldEvent> CreateUpgradeEvents(
        WorldStack stack,
        UnitGrid<WorldUnit> survivingUnits,
        IGameDefinitionProvider definitions,
        GlobalVariables globals)
    {
        (WorldUnit leader, GridPosition leaderPosition, UnitDefinition _) = stack.GetLeader(definitions);

        WorldUnit? survivingLeader = survivingUnits[leaderPosition];

        if (survivingLeader is null)
        {
            yield break;
        }

        int limit = globals.MaxLeaderAbilities;
        int rewardedLevels = Math.Min(survivingLeader.Level, limit) - leader.Level;
        int remainingUpgrades = limit - stack.PendingLeaderUpgrades;
        int grantedUpgrades = Math.Min(rewardedLevels, remainingUpgrades);

        for (int i = 0; i < grantedUpgrades; i++)
        {
            yield return new WorldLeaderUpgradeGrantedEvent(stack.Id);
        }
    }

    public static IReadOnlyList<LeaderUpgradeDefinition> GetAvailableUpgrades(
        WorldPlayer player,
        int leaderLevel,
        ImmutableArray<UnitModifier> leaderModifiers,
        IGameDefinitionProvider definitions)
    {
        List<LeaderUpgradeDefinition> available = [];

        foreach (LeaderUpgradeDefinition upgrade in definitions.GetLeaderUpgrades(player.LordId))
        {
            if (upgrade.MinLevel <= leaderLevel &&
                !leaderModifiers.Any(modifier => modifier.DefinitionId == upgrade.ModifierId))
            {
                available.Add(upgrade);
            }
        }

        return available;
    }

    public static LeaderUpgradeDefinition? GetPreferredUpgrade(IReadOnlyList<LeaderUpgradeDefinition> upgrades)
    {
        LeaderUpgradeDefinition? preferred = null;

        foreach (LeaderUpgradeDefinition upgrade in upgrades)
        {
            if (preferred is null || upgrade.Priority > preferred.Priority)
            {
                preferred = upgrade;
            }
        }

        return preferred;
    }
}
