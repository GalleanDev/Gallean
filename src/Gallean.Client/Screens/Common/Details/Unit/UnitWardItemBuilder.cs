using Avalonia.Media;

namespace Gallean.Client;

internal static class UnitWardItemBuilder
{
    public static UnitWardItemViewModel Build(UnitWard ward, bool isActive)
    {
        return ward switch
        {
            UnitAttackClassWard classWard => BuildClassWard(classWard, isActive),
            UnitAttackSourceWard sourceWard => BuildSourceWard(sourceWard, isActive)
        };
    }

    private static UnitWardItemViewModel BuildClassWard(UnitAttackClassWard ward, bool isActive)
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

    private static UnitWardItemViewModel BuildSourceWard(UnitAttackSourceWard ward, bool isActive)
    {
        UnitAttackSourceViewModel source = UnitAttackSourceViewModel.Create(ward.Source);

        return new UnitWardItemViewModel(source.Icon, source.Text, source.Color, isActive);
    }
}
