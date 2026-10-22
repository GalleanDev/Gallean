namespace Gallean.Entities;

public static partial class Db
{
    public enum OrderType
    {
        Normal = 1,
        Stand = 2,
        Guard = 3,
        AttackStack = 4,
        DefendStack = 5,
        SecureCity = 6,
        Roam = 7,
        MoveToLocation = 8,
        DefendLocation = 9,
        Berserk = 10,
        Assist = 11,
        Explore = 12,
        Steal = 13,
        DefendCity = 14
    }
}
