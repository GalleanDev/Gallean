using Gallean.Engine;
using static Gallean.Client.BattleUnitState;

namespace Gallean.Client;

internal sealed record UnitSecondaryAttackViewModel(
    string Text,
    UnitAttackSourceViewModel Source,
    UnitDamageOverTimeViewModel? DamageOverTime)
{
    public static UnitSecondaryAttackViewModel Create(UnitAttackDefinition attack, int damage)
    {
        UnitDamageOverTimeViewModel? damageOverTime = attack.Type switch
        {
            UnitAttackType.Poison => new UnitDamageOverTimeViewModel(new BattleStatusEffectViewModel(new StatusEffect(StatusEffectType.Poison)), damage),
            UnitAttackType.Frostbite => new UnitDamageOverTimeViewModel(new BattleStatusEffectViewModel(new StatusEffect(StatusEffectType.Frostbite)), damage),
            UnitAttackType.Blister => new UnitDamageOverTimeViewModel(new BattleStatusEffectViewModel(new StatusEffect(StatusEffectType.Blister)), damage),
            UnitAttackType.Damage => null,
            UnitAttackType.DrainLife => null,
            UnitAttackType.Paralyze => null,
            UnitAttackType.Heal => null,
            UnitAttackType.Fear => null,
            UnitAttackType.IncreaseDamage => null,
            UnitAttackType.Petrify => null,
            UnitAttackType.ReduceDamage => null,
            UnitAttackType.ReduceInitiative => null,
            UnitAttackType.Revive => null,
            UnitAttackType.DrainLifeOverflow => null,
            UnitAttackType.Cure => null,
            UnitAttackType.Summon => null,
            UnitAttackType.ReduceLevel => null,
            UnitAttackType.GiveAdditionalAttack => null,
            UnitAttackType.Doppelganger => null,
            UnitAttackType.TransformSelf => null,
            UnitAttackType.TransformEnemy => null,
            UnitAttackType.GiveProtection => null,
            UnitAttackType.ReduceArmor => null
        };

        return new UnitSecondaryAttackViewModel(
            UnitAttackFormatter.FormatType(attack.Type),
            UnitAttackSourceViewModel.Create(attack.Source),
            damageOverTime);
    }
}
