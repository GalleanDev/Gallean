using System.Collections.Immutable;

namespace Gallean.Engine;

internal static class UnitAttackStatsCalculator
{
    public static UnitAttackStats CalculateForUnit(
        UnitDefinition definition,
        UnitAttackDefinition attack,
        int level,
        ImmutableArray<UnitModifier> modifiers,
        IGameDefinitionProvider definitions,
        GlobalVariables globals)
    {
        UnitDynamicUpgrade upgrade = UnitDynamicUpgradeCalculator.Calculate(definition, level, definitions);

        int accuracy = attack.Accuracy + upgrade.Accuracy;
        int damage = attack.Damage + upgrade.Damage;
        int drainPercent = 0;

        foreach (UnitModifier modifier in modifiers)
        {
            UnitModifierDefinition modifierDefinition = definitions.GetUnitModifier(modifier.DefinitionId);

            accuracy = UnitStatsCalculator.ApplyStat(accuracy, modifierDefinition.Accuracy, globals.MinAttackAccuracy, globals.MaxAttackAccuracy);
            damage = UnitStatsCalculator.ApplyStat(damage, modifierDefinition.Damage, globals.MinAttackDamage, globals.MaxModifiedAttackDamage);
            drainPercent += modifierDefinition.DrainPercent;
        }

        return WithGlobalLimits(accuracy, damage, drainPercent, globals);
    }

    public static UnitAttackStats CalculateForItem(UnitAttackDefinition attack, GlobalVariables globals)
    {
        return WithGlobalLimits(attack.Accuracy, attack.Damage, drainPercent: 0, globals);
    }

    private static UnitAttackStats WithGlobalLimits(int accuracy, int damage, int drainPercent, GlobalVariables globals)
    {
        return new UnitAttackStats(Math.Min(accuracy, globals.MaxAttackAccuracy), Math.Min(damage, globals.MaxAttackDamage), drainPercent);
    }
}
