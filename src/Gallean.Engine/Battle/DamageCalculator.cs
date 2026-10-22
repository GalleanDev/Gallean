using Gallean.Common;

namespace Gallean.Engine;

internal static class DamageCalculator
{
    public static int GetAttackDamage(
        BattleState state,
        IEngineContext context,
        BattleUnitId attackerId,
        BattleUnitId targetId,
        UnitAttackDefinition attack,
        UnitAttackStats attackStats)
    {
        UnitStats targetStats = context.UnitStats.GetBattleUnitStats(state, targetId);
        BattleUnit attacker = state.GetUnit(attackerId);
        BattleSideModifiers attackerModifiers = state.Modifiers.GetModifiers(attacker.Side);
        int damageVariance = context.Globals.BattleDamageVariance + attackerModifiers.RandomDamageBonus;
        int rawDamage = Math.Min(RollDamage(attackStats.Damage, damageVariance, context.Random), context.Globals.MaxAttackDamage);
        int damage = ApplyArmor(rawDamage, targetStats.Armor, context.Globals.MaxArmor, context.Globals.MinAttackDamage);

        if (!HasCriticalHit(state, context, attackerId, attack))
        {
            return damage;
        }

        if (!HitCalculator.RollHit(context.Globals.CriticalHitAccuracy, context.Random))
        {
            return damage;
        }

        int criticalDamage = rawDamage * context.Globals.CriticalHitDamagePercent / 100;

        return damage + criticalDamage;
    }

    private static int RollDamage(int baseDamage, int damageBonus, IRandomProvider random)
    {
        if (damageBonus > 0)
        {
            return baseDamage + random.Next(0, damageBonus);
        }
        else
        {
            return baseDamage;
        }
    }

    private static int ApplyArmor(int rawDamage, int armor, int maxArmor, int minDamage)
    {
        int clampedArmor = Math.Clamp(armor, 0, maxArmor);
        int damage = rawDamage - (rawDamage * clampedArmor / 100);

        return Math.Max(minDamage, damage);
    }

    private static bool HasCriticalHit(BattleState state, IEngineContext context, BattleUnitId attackerId, UnitAttackDefinition attack)
    {
        if (attack.Type is UnitAttackType.Damage)
        {
            LeaderAbility abilities = context.UnitStats.GetBattleUnitAbilities(state, attackerId);

            return attack.HasCriticalHit || abilities.HasFlag(LeaderAbility.CriticalHit);
        }
        else
        {
            return false;
        }
    }
}
