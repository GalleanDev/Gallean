namespace Gallean.Entities;

public static partial class Db
{
    public enum ScenarioEventEffectType
    {
        Win = 0,
        CreateStack = 1,
        CastSpell = 2,
        CastSpellMap = 3,
        ChangeOwner = 4,
        MoveBeside = 5,
        Battle = 6,
        EnableEvent = 7,
        GiveSpell = 8,
        GiveItem = 9,
        MoveStack = 10,
        Ally = 11,
        Diplomacy = 12,
        Unfog = 13,
        RemoveMountain = 14,
        RemoveLandmark = 15,
        ChangeObjective = 16,
        Popup = 17,
        ChangeOrder = 18,
        DestroyItem = 19,
        RemoveStack = 20,
        ChangeLandmark = 21,
        ChangeTerrain = 22,
        ModifyVariable = 23
    }
}
