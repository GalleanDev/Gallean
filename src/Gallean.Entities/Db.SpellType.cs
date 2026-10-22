namespace Gallean.Entities;

public static partial class Db
{
    public enum SpellType
    {
        Attack = 0,
        Lower = 1,
        Heal = 2,
        Boost = 3,
        Summon = 4,
        Fog = 6,
        Unfog = 7,
        RestoreMovement = 8,
        Invisibility = 9,
        RemoveRod = 10,
        ChangeTerrain = 11,
        GiveWards = 12
    }
}
