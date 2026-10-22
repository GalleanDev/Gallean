namespace Gallean.Entities;

public static partial class Db
{
    public enum ScenarioEventConditionType
    {
        Frequency = 0,
        EnteringZone = 2,
        EnteringCity = 3,
        OwningCity = 4,
        DestroyingStack = 5,
        OwningItem = 6,
        LeaderOwningItem = 7,
        DiplomacyRelations = 8,
        Alliance = 9,
        LootingRuin = 10,
        TransformingLand = 11,
        VisitingSite = 12,
        StackToLocation = 14,
        StackToCity = 15,
        ItemToLocation = 16,
        StackExistence = 17,
        VariableInRange = 18
    }
}
