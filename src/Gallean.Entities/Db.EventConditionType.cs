namespace Gallean.Entities;

public static partial class Db
{
    public enum EventConditionType
    {
        Frequency = 0,
        Location = 2,
        EnterCity = 3,
        OwnCity = 4,
        KillStack = 5,
        OwnItem = 6,
        LeaderOwnItem = 7,
        Diplomacy = 8,
        Alliance = 9,
        LootRuin = 10,
        TransformLand = 11,
        VisitSite = 12,
        LeaderToZone = 14,
        LeaderToCity = 15,
        ItemToLocation = 16,
        StackExists = 17,
        VarInRange = 18
    }
}
