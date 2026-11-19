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

        int accuracy = attack.Accuracy > 0 ? attack.Accuracy + upgrade.Accuracy : attack.Accuracy;
        int damage = attack.Damage > 0 ? attack.Damage + upgrade.Damage : attack.Damage;
        int heal = attack.Heal > 0 ? attack.Heal + upgrade.Heal : 0;
        int drainPercent = 0;

        foreach (UnitModifier modifier in modifiers)
        {
            UnitModifierDefinition modifierDefinition = definitions.GetUnitModifier(modifier.DefinitionId);

            if (attack.Accuracy > 0)
            {
                accuracy = UnitStatsCalculator.ApplyStat(accuracy, modifierDefinition.Accuracy, globals.MinAttackAccuracy, globals.MaxAttackAccuracy);
            }

            if (attack.Damage > 0)
            {
                damage = UnitStatsCalculator.ApplyStat(damage, modifierDefinition.Damage, globals.MinAttackDamage, globals.MaxModifiedAttackDamage);
            }

            drainPercent += modifierDefinition.DrainPercent;
        }

        return WithGlobalLimits(attack, accuracy, damage, heal, drainPercent, globals);
    }

    public static UnitAttackStats CalculateForItem(UnitAttackDefinition attack, GlobalVariables globals)
    {
        return WithGlobalLimits(attack, attack.Accuracy, attack.Damage, attack.Heal, drainPercent: 0, globals);
    }

    private static UnitAttackStats WithGlobalLimits(
        UnitAttackDefinition attack,
        int accuracy,
        int damage,
        int heal,
        int drainPercent,
        GlobalVariables globals)
    {
        return new UnitAttackStats(
            Math.Min(accuracy, globals.MaxAttackAccuracy),
            Math.Min(damage, globals.MaxAttackDamage),
            heal,
            drainPercent,
            GetIncreaseDamagePercent(attack, globals));
    }

    private static int GetIncreaseDamagePercent(UnitAttackDefinition attack, GlobalVariables globals)
    {
        if (attack.Type is UnitAttackType.IncreaseDamage)
        {
            return attack.Level switch
            {
                1 => globals.IncreaseDamagePercentLevel1,
                2 => globals.IncreaseDamagePercentLevel2,
                3 => globals.IncreaseDamagePercentLevel3,
                4 => globals.IncreaseDamagePercentLevel4,
                _ => throw new ArgumentOutOfRangeException(nameof(attack), attack, null)
            };
        }
        else
        {
            return 0;
        }
    }
}
