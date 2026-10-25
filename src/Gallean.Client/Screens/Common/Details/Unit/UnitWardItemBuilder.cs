using Avalonia.Media;
using Gallean.Common;
using Gallean.Engine;

namespace Gallean.Client;

internal static class UnitWardItemBuilder
{
    private sealed class WardVisitor : IUnitWardVisitor<bool, UnitWardItemViewModel>, IStaticInstance<WardVisitor>
    {
        public UnitWardItemViewModel Visit(UnitAttackClassWard ward, bool isActive)
        {
            (IconMoniker icon, Color color) = ward.AttackType switch
            {
                UnitAttackType.Damage => (IconMoniker.Damage, Theme.Common.PrimaryTextColor),
                UnitAttackType.DrainLife => (IconMoniker.Hitpoint, Theme.Common.PrimaryTextColor),
                UnitAttackType.Paralyze => (IconMoniker.Paralyze, Theme.Battle.StatusEffect.ParalyzeColor),
                UnitAttackType.Heal => (IconMoniker.Heal, Theme.Battle.StatusEffect.CureColor),
                UnitAttackType.Fear => (IconMoniker.Retreat, Theme.Battle.StatusEffect.RetreatColor),
                UnitAttackType.IncreaseDamage => (IconMoniker.IncreaseDamage, Theme.Battle.StatusEffect.IncreaseDamageColor),
                UnitAttackType.Petrify => (IconMoniker.Petrify, Theme.Battle.StatusEffect.PetrifyColor),
                UnitAttackType.ReduceDamage => (IconMoniker.ReduceDamage, Theme.Battle.StatusEffect.ReduceDamageColor),
                UnitAttackType.ReduceInitiative => (IconMoniker.ReduceInitiative, Theme.Battle.StatusEffect.ReduceInitiativeColor),
                UnitAttackType.Poison => (IconMoniker.Poison, Theme.Battle.StatusEffect.PoisonColor),
                UnitAttackType.Frostbite => (IconMoniker.Frostbite, Theme.Battle.StatusEffect.FrostbiteColor),
                UnitAttackType.Revive => (IconMoniker.Resurrect, Theme.Battle.StatusEffect.CureColor),
                UnitAttackType.DrainLifeOverflow => (IconMoniker.Hitpoint, Theme.Common.PrimaryTextColor),
                UnitAttackType.Cure => (IconMoniker.Cure, Theme.Battle.StatusEffect.CureColor),
                UnitAttackType.Summon => (IconMoniker.Human, Theme.Common.PrimaryTextColor),
                UnitAttackType.ReduceLevel => (IconMoniker.ReduceLevel, Theme.Battle.StatusEffect.ReduceLevelColor),
                UnitAttackType.GiveAdditionalAttack => (IconMoniker.Initiative, Theme.Common.PrimaryTextColor),
                UnitAttackType.Doppelganger => (IconMoniker.Transform, Theme.Battle.StatusEffect.TransformColor),
                UnitAttackType.TransformSelf => (IconMoniker.Transform, Theme.Battle.StatusEffect.TransformColor),
                UnitAttackType.TransformEnemy => (IconMoniker.Transform, Theme.Battle.StatusEffect.TransformColor),
                UnitAttackType.Blister => (IconMoniker.Blister, Theme.Battle.StatusEffect.BlisterColor),
                UnitAttackType.GiveProtection => (IconMoniker.Defend, Theme.Battle.StatusEffect.DefendColor),
                UnitAttackType.ReduceArmor => (IconMoniker.ShatterArmor, Theme.Battle.StatusEffect.ShatterArmorColor)
            };

            return new UnitWardItemViewModel(icon, UnitAttackFormatter.FormatType(ward.AttackType), color, isActive);
        }

        public UnitWardItemViewModel Visit(UnitAttackSourceWard ward, bool isActive)
        {
            (IconMoniker icon, Color color) = ward.Source switch
            {
                UnitAttackSource.Weapon => (IconMoniker.AttackSourceWeapon, Theme.AttackSource.WeaponColor),
                UnitAttackSource.Mind => (IconMoniker.AttackSourceMind, Theme.AttackSource.MindColor),
                UnitAttackSource.Life => (IconMoniker.AttackSourceLife, Theme.AttackSource.LifeColor),
                UnitAttackSource.Death => (IconMoniker.AttackSourceDeath, Theme.AttackSource.DeathColor),
                UnitAttackSource.Fire => (IconMoniker.AttackSourceFire, Theme.AttackSource.FireColor),
                UnitAttackSource.Water => (IconMoniker.AttackSourceWater, Theme.AttackSource.WaterColor),
                UnitAttackSource.Earth => (IconMoniker.AttackSourceEarth, Theme.AttackSource.EarthColor),
                UnitAttackSource.Air => (IconMoniker.AttackSourceAir, Theme.AttackSource.AirColor)
            };

            return new UnitWardItemViewModel(icon, UnitAttackFormatter.FormatSource(ward.Source), color, isActive);
        }
    }

    public static UnitWardItemViewModel Build(UnitWard ward, bool isActive)
    {
        return ward.Accept(WardVisitor.Instance, isActive);
    }
}
