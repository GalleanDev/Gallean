namespace Gallean.Engine;

internal static class UnitAttackTypeExtensions
{
    extension(UnitAttackType type)
    {
        public UnitAttackTypeInfo GetInfo()
        {
            return type switch
            {
                UnitAttackType.Damage => new UnitAttackTypeInfo(UnitAttackTargetType.Enemy, CanMiss: true, IsWardable: true, TargetsDead: false, IsRecovery: false),
                UnitAttackType.DrainLife => new UnitAttackTypeInfo(UnitAttackTargetType.Enemy, CanMiss: true, IsWardable: true, TargetsDead: false, IsRecovery: false),
                UnitAttackType.Paralyze => new UnitAttackTypeInfo(UnitAttackTargetType.Enemy, CanMiss: true, IsWardable: true, TargetsDead: false, IsRecovery: false),
                UnitAttackType.Heal => new UnitAttackTypeInfo(UnitAttackTargetType.Ally, CanMiss: false, IsWardable: false, TargetsDead: false, IsRecovery: true),
                UnitAttackType.Fear => new UnitAttackTypeInfo(UnitAttackTargetType.Enemy, CanMiss: true, IsWardable: true, TargetsDead: false, IsRecovery: false),
                UnitAttackType.IncreaseDamage => new UnitAttackTypeInfo(UnitAttackTargetType.Ally, CanMiss: false, IsWardable: false, TargetsDead: false, IsRecovery: false),
                UnitAttackType.Petrify => new UnitAttackTypeInfo(UnitAttackTargetType.Enemy, CanMiss: true, IsWardable: true, TargetsDead: false, IsRecovery: false),
                UnitAttackType.ReduceDamage => new UnitAttackTypeInfo(UnitAttackTargetType.Enemy, CanMiss: true, IsWardable: true, TargetsDead: false, IsRecovery: false),
                UnitAttackType.ReduceInitiative => new UnitAttackTypeInfo(UnitAttackTargetType.Enemy, CanMiss: true, IsWardable: true, TargetsDead: false, IsRecovery: false),
                UnitAttackType.Poison => new UnitAttackTypeInfo(UnitAttackTargetType.Enemy, CanMiss: true, IsWardable: true, TargetsDead: false, IsRecovery: false),
                UnitAttackType.Frostbite => new UnitAttackTypeInfo(UnitAttackTargetType.Enemy, CanMiss: true, IsWardable: true, TargetsDead: false, IsRecovery: false),
                UnitAttackType.Revive => new UnitAttackTypeInfo(UnitAttackTargetType.Ally, CanMiss: false, IsWardable: false, TargetsDead: true, IsRecovery: true),
                UnitAttackType.DrainLifeOverflow => new UnitAttackTypeInfo(UnitAttackTargetType.Enemy, CanMiss: true, IsWardable: true, TargetsDead: false, IsRecovery: false),
                UnitAttackType.Cure => new UnitAttackTypeInfo(UnitAttackTargetType.Ally, CanMiss: false, IsWardable: false, TargetsDead: false, IsRecovery: true),
                UnitAttackType.Summon => new UnitAttackTypeInfo(UnitAttackTargetType.Tile, CanMiss: false, IsWardable: false, TargetsDead: false, IsRecovery: false),
                UnitAttackType.ReduceLevel => new UnitAttackTypeInfo(UnitAttackTargetType.Enemy, CanMiss: true, IsWardable: true, TargetsDead: false, IsRecovery: false),
                UnitAttackType.GiveAdditionalAttack => new UnitAttackTypeInfo(UnitAttackTargetType.Ally, CanMiss: false, IsWardable: false, TargetsDead: false, IsRecovery: false),
                UnitAttackType.Doppelganger => new UnitAttackTypeInfo(UnitAttackTargetType.Any, CanMiss: false, IsWardable: false, TargetsDead: false, IsRecovery: false),
                UnitAttackType.TransformSelf => new UnitAttackTypeInfo(UnitAttackTargetType.Any, CanMiss: false, IsWardable: false, TargetsDead: false, IsRecovery: false),
                UnitAttackType.TransformEnemy => new UnitAttackTypeInfo(UnitAttackTargetType.Enemy, CanMiss: true, IsWardable: true, TargetsDead: false, IsRecovery: false),
                UnitAttackType.Blister => new UnitAttackTypeInfo(UnitAttackTargetType.Enemy, CanMiss: true, IsWardable: true, TargetsDead: false, IsRecovery: false),
                UnitAttackType.GiveProtection => new UnitAttackTypeInfo(UnitAttackTargetType.Ally, CanMiss: false, IsWardable: false, TargetsDead: false, IsRecovery: false),
                UnitAttackType.ReduceArmor => new UnitAttackTypeInfo(UnitAttackTargetType.Enemy, CanMiss: false, IsWardable: true, TargetsDead: false, IsRecovery: false)
            };
        }
    }
}
