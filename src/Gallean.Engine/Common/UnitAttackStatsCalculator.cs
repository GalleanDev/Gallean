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

        return WithGlobalLimits(definition, attack, accuracy, damage, heal, drainPercent, definitions, globals);
    }

    public static UnitAttackStats CalculateForItem(
        UnitDefinition definition,
        UnitAttackDefinition attack,
        IGameDefinitionProvider definitions,
        GlobalVariables globals)
    {
        return WithGlobalLimits(definition, attack, attack.Accuracy, attack.Damage, attack.Heal, drainPercent: 0, definitions, globals);
    }

    private static int GetAccuracyReductionPerTarget(UnitDefinition definition, IGameDefinitionProvider definitions, GlobalVariables globals)
    {
        if (definition is { Category: UnitCategory.Leader, Attack.Reach: UnitAttackReach.All })
        {
            WorldRaceDefinition race = definitions.GetRace(definition.Race);

            if (race.Leaders.Contains(definition.Id))
            {
                return globals.AccuracyReductionPerTarget;
            }
            else
            {
                return 0;
            }
        }
        else
        {
            return 0;
        }
    }

    private static UnitAttackStats WithGlobalLimits(
        UnitDefinition definition,
        UnitAttackDefinition attack,
        int accuracy,
        int damage,
        int heal,
        int drainPercent,
        IGameDefinitionProvider definitions,
        GlobalVariables globals)
    {
        return new UnitAttackStats(
            Math.Min(accuracy, globals.MaxAttackAccuracy),
            GetAccuracyReductionPerTarget(definition, definitions, globals),
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
