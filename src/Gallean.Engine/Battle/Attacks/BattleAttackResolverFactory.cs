using Gallean.Common;

namespace Gallean.Engine;

internal static class BattleAttackResolverFactory
{
    public static IBattleAttackResolver Create(UnitAttackType type)
    {
        return type switch
        {
            UnitAttackType.Damage => DamageAttackResolver.Instance,
            UnitAttackType.DrainLife => DrainLifeAttackResolver.Instance,
            UnitAttackType.Paralyze => ParalyzeAttackResolver.Instance,
            UnitAttackType.Heal => HealAttackResolver.Instance,
            UnitAttackType.Fear => FearAttackResolver.Instance,
            UnitAttackType.IncreaseDamage => IncreaseDamageAttackResolver.Instance,
            UnitAttackType.Petrify => PetrifyAttackResolver.Instance,
            UnitAttackType.ReduceDamage => ReduceDamageAttackResolver.Instance,
            UnitAttackType.ReduceInitiative => ReduceInitiativeAttackResolver.Instance,
            UnitAttackType.Poison => PoisonAttackResolver.Instance,
            UnitAttackType.Frostbite => FrostbiteAttackResolver.Instance,
            UnitAttackType.Revive => ReviveAttackResolver.Instance,
            UnitAttackType.DrainLifeOverflow => DrainLifeOverflowAttackResolver.Instance,
            UnitAttackType.Cure => CureAttackResolver.Instance,
            UnitAttackType.Summon => SummonAttackResolver.Instance,
            UnitAttackType.ReduceLevel => ReduceLevelAttackResolver.Instance,
            UnitAttackType.GiveAdditionalAttack => GiveAdditionalAttackResolver.Instance,
            UnitAttackType.Doppelganger => DoppelgangerAttackResolver.Instance,
            UnitAttackType.TransformSelf => TransformSelfAttackResolver.Instance,
            UnitAttackType.TransformEnemy => TransformOtherAttackResolver.Instance,
            UnitAttackType.Blister => BlisterAttackResolver.Instance,
            UnitAttackType.GiveProtection => GiveProtectionAttackResolver.Instance,
            UnitAttackType.ReduceArmor => ShatterAttackResolver.Instance
        };
    }
}
