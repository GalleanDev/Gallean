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
        WorldUnit leader,
        IGameDefinitionProvider definitions,
        IUnitStatsProvider stats)
    {
        UnitDefinition leaderDefinition = definitions.GetUnit(leader.DefinitionId);
        LeaderAbility abilities = stats.GetWorldUnitAbilities(leader);
        int leadership = stats.GetWorldUnitLeadership(leader);

        List<LeaderUpgradeDefinition> available = [];

        foreach (LeaderUpgradeDefinition upgrade in definitions.GetLeaderUpgrades(player.LordId))
        {
            if (CanApply(upgrade, leader, leaderDefinition, abilities, leadership, definitions))
            {
                available.Add(upgrade);
            }
        }

        return available;
    }

    private static bool CanApply(
        LeaderUpgradeDefinition upgrade,
        WorldUnit leader,
        UnitDefinition leaderDefinition,
        LeaderAbility abilities,
        int leadership,
        IGameDefinitionProvider definitions)
    {
        if (upgrade.MinLevel > leader.Level)
        {
            return false;
        }

        if (leader.Modifiers.Any(applied => applied.DefinitionId == upgrade.ModifierId))
        {
            return false;
        }

        UnitModifierDefinition modifier = definitions.GetUnitModifier(upgrade.ModifierId);

        if ((modifier.LeaderAbilities & abilities) != LeaderAbility.None)
        {
            return false;
        }

        if (modifier.MoveAbility != UnitMoveAbility.None && leaderDefinition.MoveAbility.HasFlag(UnitMoveAbility.Flying))
        {
            return false;
        }

        if (modifier.Leadership.Flat > 0 && leadership >= GridPosition.Count)
        {
            return false;
        }

        return true;
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
