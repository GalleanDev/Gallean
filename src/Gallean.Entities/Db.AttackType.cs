namespace Gallean.Entities;

public static partial class Db
{
    public enum AttackType
    {
        Damage = 1,
        DrainLife = 2,
        Paralyze = 3,
        Heal = 6,
        Fear = 7,
        IncreaseDamage = 8,
        Petrify = 9,
        ReduceDamage = 10,
        ReduceInitiative = 11,
        Poison = 12,
        Frostbite = 13,
        Revive = 14,
        DrainLifeOverflow = 15,
        Cure = 16,
        Summon = 17,
        ReduceLevel = 18,
        GiveAdditionalAttack = 19,
        Doppelganger = 20,
        TransformSelf = 21,
        TransformEnemy = 22,
        Blister = 23,
        GiveProtection = 24,
        ReduceArmor = 25
    }
}
