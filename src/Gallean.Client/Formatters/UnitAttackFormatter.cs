namespace Gallean.Client;

internal static class UnitAttackFormatter
{
    public static string FormatType(UnitAttackType type)
    {
        return type switch
        {
            UnitAttackType.Damage => Tr("Damage"),
            UnitAttackType.DrainLife => Tr("Drain life"),
            UnitAttackType.Paralyze => Tr("Paralyze"),
            UnitAttackType.Heal => Tr("Heal"),
            UnitAttackType.Fear => Tr("Fear"),
            UnitAttackType.IncreaseDamage => Tr("Increase damage"),
            UnitAttackType.Petrify => Tr("Petrify"),
            UnitAttackType.ReduceDamage => Tr("Reduce damage"),
            UnitAttackType.ReduceInitiative => Tr("Reduce initiative"),
            UnitAttackType.Poison => Tr("Poison"),
            UnitAttackType.Frostbite => Tr("Frostbite"),
            UnitAttackType.Revive => Tr("Revive"),
            UnitAttackType.DrainLifeOverflow => Tr("Drain life"),
            UnitAttackType.Cure => Tr("Cure"),
            UnitAttackType.Summon => Tr("Summon"),
            UnitAttackType.ReduceLevel => Tr("Reduce level"),
            UnitAttackType.GiveAdditionalAttack => Tr("Grant an extra attack"),
            UnitAttackType.Doppelganger => Tr("Doppelganger"),
            UnitAttackType.TransformSelf => Tr("Transform self"),
            UnitAttackType.TransformEnemy => Tr("Transform enemy"),
            UnitAttackType.Blister => Tr("Blister"),
            UnitAttackType.GiveProtection => Tr("Grant protection"),
            UnitAttackType.ReduceArmor => Tr("Reduce armor")
        };
    }

    public static string FormatSource(UnitAttackSource source)
    {
        return source switch
        {
            UnitAttackSource.Weapon => Tr("Weapon"),
            UnitAttackSource.Mind => Tr("Mind"),
            UnitAttackSource.Life => Tr("Life"),
            UnitAttackSource.Death => Tr("Death"),
            UnitAttackSource.Fire => Tr("Fire"),
            UnitAttackSource.Water => Tr("Water"),
            UnitAttackSource.Earth => Tr("Earth"),
            UnitAttackSource.Air => Tr("Air")
        };
    }
}
